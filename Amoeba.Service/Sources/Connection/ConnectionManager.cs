using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Amoeba.Core;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Net;
using Omnius.Net.I2p;

namespace Amoeba.Service
{
    [Flags]
    [DataContract(Name = "ConnectionTypes")]
    enum ConnectionTypes
    {
        [EnumMember(Value = "Tcp")]
        Tcp = 0x01,

        [EnumMember(Value = "Tor")]
        Tor = 0x02,

        [EnumMember(Value = "I2p")]
        I2p = 0x04,
    }

    class ConnectionManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;
        private I2pManager _i2pManager;

        private Settings _settings;

        private ConnectionTypes _connectionTypes;

        private Thread _watchThread;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public ConnectionManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = coreManager;
            _i2pManager = new I2pManager(Path.Combine(configPath, "I2pManager"), coreManager, _bufferManager);

            _settings = new Settings(configPath);

            _coreManager.ConnectCapEvent = (_, uri) => this.ConnectCap(uri);
            _coreManager.AcceptCapEvent = (_) => this.AcceptCap();
        }

        public ConnectionTypes ConnectionTypes
        {
            get
            {
                lock (_lockObject)
                {
                    return _connectionTypes;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _connectionTypes = value;
                }
            }
        }

        public Cap ConnectCap(string uri)
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            Cap cap;
            if (this.ConnectionTypes.HasFlag(ConnectionTypes.I2p) && (cap = _i2pManager.ConnectCap(uri)) != null) return cap;

            return null;
        }

        public Cap AcceptCap()
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            Cap cap;
            if (this.ConnectionTypes.HasFlag(ConnectionTypes.I2p) && (cap = _i2pManager.AcceptCap()) != null) return cap;

            return null;
        }

        private void WatchThread()
        {
            var checkStopwatch = new Stopwatch();
            checkStopwatch.Start();

            var nowUris = new List<string>();

            for (;;)
            {
                Thread.Sleep(1000);
                if (this.State == ManagerState.Stop) return;

                if (!checkStopwatch.IsRunning || checkStopwatch.Elapsed.TotalSeconds >= 5)
                {
                    checkStopwatch.Restart();

                    var targetUris = new List<string>();

                    if (this.ConnectionTypes.HasFlag(ConnectionTypes.I2p))
                    {
                        targetUris.AddRange(_i2pManager.LocationUris);

                        if (_i2pManager.State == ManagerState.Stop)
                        {
                            _i2pManager.Start();
                        }
                    }
                    else
                    {
                        if (_i2pManager.State == ManagerState.Start)
                        {
                            _i2pManager.Stop();
                        }
                    }

                    if (!CollectionUtils.Equals(nowUris, targetUris))
                    {
                        _coreManager.SetMyLocation(new Location(targetUris));

                        nowUris.Clear();
                        nowUris.AddRange(targetUris);
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
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _watchThread = new Thread(this.WatchThread);
                    _watchThread.Priority = ThreadPriority.Lowest;
                    _watchThread.Name = "ConnectionManager_WatchThread";
                    _watchThread.Start();
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

                _watchThread.Join();
                _watchThread = null;

                _i2pManager.Stop();
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);

                _connectionTypes = _settings.Load<ConnectionTypes>("ConnectionTypes");
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                _settings.Save("ConnectionTypes", _connectionTypes);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {

            }
        }
    }
}
