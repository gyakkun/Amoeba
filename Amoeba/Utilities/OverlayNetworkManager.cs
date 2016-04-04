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
using Library;
using Library.Net;
using Library.Net.Amoeba;
using Library.Net.Connections;
using Library.Net.I2p;

namespace Amoeba
{
    class OverlayNetworkManager : StateManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private AmoebaManager _amoebaManager;
        private BufferManager _bufferManager;

        private Settings _settings;

        private string _oldSamBridgeUri;
        private SamManager _samManager;

        private Regex _regex = new Regex(@"(.*?):(.*):(\d*)");

        private Thread _watchThread;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public OverlayNetworkManager(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _settings = new Settings(this.ThisLock);

            _amoebaManager.CreateCapEvent = this.CreateCap;
            _amoebaManager.AcceptCapEvent = this.AcceptCap;
        }

        private Cap CreateCap(object sender, string uri)
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            if (!uri.StartsWith("i2p:")) return null;

            List<IDisposable> garbages = new List<IDisposable>();

            try
            {
                string scheme = null;
                string host = null;

                {
                    Regex regex = new Regex(@"(.*?):(.*)");
                    var match = regex.Match(uri);

                    if (match.Success)
                    {
                        scheme = match.Groups[1].Value;
                        host = match.Groups[2].Value;
                    }
                }

                if (host == null) return null;

                {
                    string proxyScheme = null;
                    string proxyHost = null;
                    int proxyPort = -1;

                    {
                        Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(this.SamBridgeUri);

                        if (match.Success)
                        {
                            proxyScheme = match.Groups[1].Value;
                            proxyHost = match.Groups[2].Value;
                            proxyPort = int.Parse(match.Groups[3].Value);
                        }
                    }

                    if (proxyHost == null) return null;

                    if (scheme == "i2p")
                    {
                        Socket socket = null;

                        try
                        {
                            socket = _samManager.Connect(host);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            if (socket != null) socket.Dispose();

                            throw;
                        }

                        return new SocketCap(socket);
                    }
                }
            }
            catch (Exception)
            {
                foreach (var item in garbages)
                {
                    item.Dispose();
                }
            }

            return null;
        }

        private Cap AcceptCap(object sender, out string uri)
        {
            uri = null;

            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            Socket socket = null;

            try
            {
                string base32Address;

                socket = _samManager.Accept(out base32Address);
                uri = string.Format("i2p:{0}", base32Address);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if (socket != null) socket.Dispose();

                return null;
            }

            return new SocketCap(socket);
        }

        public string SamBridgeUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _settings.SamBridgeUri;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _settings.SamBridgeUri = value;
                }
            }
        }

        private bool AddUri(string uri)
        {
            lock (this.ThisLock)
            {
                lock (_amoebaManager.ThisLock)
                {
                    var baseNode = _amoebaManager.BaseNode;

                    var uris = new List<string>(baseNode.Uris);
                    if (uris.Contains(uri)) return false;

                    uris.Add(uri);

                    _amoebaManager.SetBaseNode(new Node(baseNode.Id, uris));
                }
            }

            return true;
        }

        private bool RemoveUri(string uri)
        {
            lock (this.ThisLock)
            {
                lock (_amoebaManager.ThisLock)
                {
                    var baseNode = _amoebaManager.BaseNode;

                    var uris = new List<string>(baseNode.Uris);
                    if (!uris.Remove(uri)) return false;

                    _amoebaManager.SetBaseNode(new Node(baseNode.Id, uris));
                }
            }

            return true;
        }

        private void WatchThread()
        {
            Stopwatch checkSamStopwatch = new Stopwatch();
            checkSamStopwatch.Start();

            for (;;)
            {
                Thread.Sleep(1000);
                if (this.State == ManagerState.Stop) return;

                if (!checkSamStopwatch.IsRunning || checkSamStopwatch.Elapsed.TotalSeconds >= 30)
                {
                    checkSamStopwatch.Restart();

                    if ((_samManager == null || !_samManager.IsConnected)
                        || _oldSamBridgeUri != this.SamBridgeUri)
                    {
                        string i2pUri = null;

                        try
                        {
                            var match = _regex.Match(this.SamBridgeUri);
                            if (!match.Success) throw new Exception();

                            if (match.Groups[1].Value == "tcp")
                            {
                                {
                                    if (_samManager != null)
                                    {
                                        _samManager.Dispose();
                                        _samManager = null;
                                    }

                                    var host = match.Groups[2].Value;
                                    var port = int.Parse(match.Groups[3].Value);

                                    if (_samManager != null) _samManager.Dispose();
                                    _samManager = new SamManager(host, port, "Amoeba");
                                }

                                var base32Address = _samManager.Start();

                                if (base32Address != null)
                                {
                                    i2pUri = string.Format("i2p:{0}", base32Address);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);

                            if (_samManager != null) _samManager.Dispose();
                        }

                        lock (this.ThisLock)
                        {
                            if (i2pUri != _settings.I2pUri)
                            {
                                if (this.RemoveUri(_settings.I2pUri))
                                    Log.Information(string.Format("Remove Node uri: {0}", _settings.I2pUri));
                            }

                            _settings.I2pUri = i2pUri;

                            if (_settings.I2pUri != null)
                            {
                                if (this.AddUri(_settings.I2pUri))
                                    Log.Information(string.Format("Add Node uri: {0}", _settings.I2pUri));
                            }

                            _oldSamBridgeUri = this.SamBridgeUri;
                        }
                    }
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
                lock (this.ThisLock)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _watchThread = new Thread(this.WatchThread);
                    _watchThread.Priority = ThreadPriority.Lowest;
                    _watchThread.Name = "OverlayNetworkManager_WatchThread";
                    _watchThread.Start();
                }
            }
        }

        public override void Stop()
        {
            lock (_stateLock)
            {
                lock (this.ThisLock)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                _watchThread.Join();
                _watchThread = null;

                if (_samManager != null) _samManager.Dispose();
                _samManager = null;

                lock (this.ThisLock)
                {
                    if (_settings.I2pUri != null)
                    {
                        if (this.RemoveUri(_settings.I2pUri))
                            Log.Information(string.Format("Remove Node uri: {0}", _settings.I2pUri));
                    }
                    _settings.I2pUri = null;
                }
            }
        }

        #region ISettings

        public void Load(string directoryPath)
        {
            lock (this.ThisLock)
            {
                _settings.Load(directoryPath);
            }
        }

        public void Save(string directoryPath)
        {
            lock (this.ThisLock)
            {
                _settings.Save(directoryPath);
            }
        }

        #endregion

        private class Settings : Library.Configuration.SettingsBase
        {
            private object _thisLock;

            public Settings(object lockObject)
                : base(new List<Library.Configuration.ISettingContent>() {
                    new Library.Configuration.SettingContent<string>() { Name = "SamBridgeUri", Value = "tcp:127.0.0.1:7656" },
                    new Library.Configuration.SettingContent<string>() { Name = "I2pUri", Value = "" },
                })
            {
                _thisLock = lockObject;
            }

            public override void Load(string directoryPath)
            {
                lock (_thisLock)
                {
                    base.Load(directoryPath);
                }
            }

            public override void Save(string directoryPath)
            {
                lock (_thisLock)
                {
                    base.Save(directoryPath);
                }
            }

            public string SamBridgeUri
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (string)this["SamBridgeUri"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["SamBridgeUri"] = value;
                    }
                }
            }

            public string I2pUri
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (string)this["I2pUri"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["I2pUri"] = value;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_samManager != null) _samManager.Dispose();
                _samManager = null;
            }
        }

        #region IThisLock

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion
    }

    [Serializable]
    class OverlayNetworkManagerException : ManagerException
    {
        public OverlayNetworkManagerException() : base() { }
        public OverlayNetworkManagerException(string message) : base(message) { }
        public OverlayNetworkManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
