using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Amoeba.Core;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Net;
using Omnius.Net.I2p;
using Omnius.Utilities;

namespace Amoeba.Service
{
    class I2pManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;

        private Settings _settings;

        private ConnectionI2pConfig _config;

        private SamManager _samManager;

        private WatchTimer _watchTimer;

        private List<string> _locationUris;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public I2pManager(string configPath, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            _settings = new Settings(configPath);

            _watchTimer = new WatchTimer(this.WatchThread);
        }

        public ConnectionI2pConfig Config
        {
            get
            {
                lock (_lockObject)
                {
                    return _config;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _config = value;
                }

                _watchTimer.Run();
            }
        }

        public IEnumerable<string> LocationUris
        {
            get
            {
                lock (_lockObject)
                {
                    return _locationUris.ToArray();
                }
            }
        }

        public Cap ConnectCap(string uri)
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;
            if (!this.Config.IsEnabled) return null;

            if (!uri.StartsWith("i2p:")) return null;

            try
            {
                var result = UriUtils.Parse(uri);
                if (result == null) return null;

                var scheme = result.GetValue<string>("Scheme");
                if (scheme != "i2p") return null;

                var address = result.GetValue<string>("Address");

                Socket socket = null;

                try
                {
                    socket = _samManager.Connect(address);
                }
                catch (Exception)
                {
                    if (socket != null)
                    {
                        socket.Dispose();
                        socket = null;
                    }

                    throw;
                }

                return new SocketCap(socket);
            }
            catch (Exception)
            {

            }

            return null;
        }

        public Cap AcceptCap()
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;
            if (!this.Config.IsEnabled) return null;

            Socket socket = null;

            try
            {
                string base32Address;

                socket = _samManager.Accept(out base32Address);
            }
            catch (SamException)
            {
                if (socket != null) socket.Dispose();

                return null;
            }

            return new SocketCap(socket);
        }

        private volatile string _watchSamBridgeUri = null;

        private void WatchThread()
        {
            for (;;)
            {
                ConnectionI2pConfig config = null;

                lock (_lockObject)
                {
                    config = this.Config;
                }

                string i2pUri = null;

                if (config.IsEnabled)
                {
                    if ((_samManager == null || !_samManager.IsConnected)
                        || _watchSamBridgeUri != config.SamBridgeUri)
                    {
                        try
                        {
                            var result = UriUtils.Parse(config.SamBridgeUri);
                            if (result == null) throw new Exception();

                            var scheme = result.GetValue<string>("Scheme");
                            if (scheme == "tcp") throw new Exception();

                            var address = result.GetValue<string>("Address");
                            var port = result.GetValueOrDefault<int>("Port", () => 7656);

                            {
                                if (_samManager != null)
                                {
                                    _samManager.Dispose();
                                    _samManager = null;
                                }

                                _samManager = new SamManager(address, port, "Amoeba");
                            }

                            var base32Address = _samManager.Start();

                            if (base32Address != null)
                            {
                                i2pUri = string.Format("i2p:{0}", base32Address);
                            }

                            _watchSamBridgeUri = config.SamBridgeUri;
                        }
                        catch (Exception)
                        {
                            if (_samManager != null)
                            {
                                _samManager.Dispose();
                                _samManager = null;
                            }
                        }
                    }
                }
                else
                {
                    if (_samManager != null)
                    {
                        _samManager.Dispose();
                        _samManager = null;
                    }
                }

                lock (_lockObject)
                {
                    if (this.Config != config) continue;

                    _locationUris.Clear();
                    if (i2pUri != null) _locationUris.Add(i2pUri);
                }
            }
        }

        public override ManagerState State
        {
            get
            {
                return _state;
            }
        }

        private readonly object _stateLock = new object();

        public override void Start()
        {
            lock (_stateLock)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _watchTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 3, 0));
                }
            }
        }

        public override void Stop()
        {
            lock (_stateLock)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                _watchTimer.Stop();

                if (_samManager != null)
                {
                    _samManager.Dispose();
                    _samManager = null;
                }
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);

                _config = _settings.Load<ConnectionI2pConfig>("Config");
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                _settings.Save("Config", _config);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_watchTimer != null)
                {
                    _watchTimer.Dispose();
                    _watchTimer = null;
                }

                if (_samManager != null)
                {
                    _samManager.Dispose();
                    _samManager = null;
                }
            }
        }
    }
}
