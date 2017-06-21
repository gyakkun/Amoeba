using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Net;
using Omnius.Net.I2p;
using Omnius.Utilities;

namespace Amoeba.Service
{
    class ConnectionManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;
        private CatharsisManager _catharsisManager;
        private TcpConnectionManager _tcpConnectionManager;
        private I2pConnectionManager _i2pConnectionManager;

        private WatchTimer _watchTimer;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public ConnectionManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = coreManager;
            _catharsisManager = new CatharsisManager(Path.Combine(configPath, "Catharsis"), _bufferManager);
            _tcpConnectionManager = new TcpConnectionManager(Path.Combine(configPath, "TcpConnection"), _catharsisManager, _bufferManager);
            _i2pConnectionManager = new I2pConnectionManager(Path.Combine(configPath, "I2pConnection"), _bufferManager);

            _coreManager.ConnectCapEvent = (_, uri) => this.ConnectCap(uri);
            _coreManager.AcceptCapEvent = (object _, out string uri) => this.AcceptCap(out uri);

            _watchTimer = new WatchTimer(this.WatchThread);
        }

        public Information Information
        {
            get
            {
                lock (_lockObject)
                {
                    var contexts = new List<InformationContext>();
                    contexts.AddRange(_tcpConnectionManager.Information);

                    return new Information(contexts);
                }
            }
        }

        public CatharsisConfig CatharsisConfig
        {
            get
            {
                return _catharsisManager.Config;
            }
        }

        public TcpConnectionConfig TcpConnectionConfig
        {
            get
            {
                return _tcpConnectionManager.Config;
            }
        }

        public I2pConnectionConfig I2pConnectionConfig
        {
            get
            {
                return _i2pConnectionManager.Config;
            }
        }

        public void SetCatharsisConfig(CatharsisConfig config)
        {
            _catharsisManager.SetConfig(config);
        }

        public void SetTcpConnectionConfig(TcpConnectionConfig config)
        {
            _tcpConnectionManager.SetConfig(config);
        }

        public void SetI2pConnectionConfig(I2pConnectionConfig config)
        {
            _i2pConnectionManager.SetConfig(config);
        }

        public Cap ConnectCap(string uri)
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            Cap cap;
            if ((cap = _tcpConnectionManager.ConnectCap(uri)) != null) return cap;
            if ((cap = _i2pConnectionManager.ConnectCap(uri)) != null) return cap;

            return null;
        }

        public Cap AcceptCap(out string uri)
        {
            uri = null;

            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            Cap cap;
            if ((cap = _tcpConnectionManager.AcceptCap(out uri)) != null) return cap;
            if ((cap = _i2pConnectionManager.AcceptCap(out uri)) != null) return cap;

            return null;
        }

        private void WatchThread()
        {
            var targetUris = new List<string>();

            targetUris.AddRange(_tcpConnectionManager.LocationUris);
            targetUris.AddRange(_i2pConnectionManager.LocationUris);

            if (!CollectionUtils.Equals(_coreManager.MyLocation.Uris, targetUris))
            {
                _coreManager.SetMyLocation(new Location(targetUris));
            }
        }

        public override ManagerState State
        {
            get
            {
                return _state;
            }
        }

        private readonly object _stateLockObject = new object();

        public override void Start()
        {
            lock (_stateLockObject)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _catharsisManager.Start();
                    _tcpConnectionManager.Start();
                    _i2pConnectionManager.Start();

                    _watchTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 30));
                }
            }
        }

        public override void Stop()
        {
            lock (_stateLockObject)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                _watchTimer.Stop();
                _coreManager.SetMyLocation(new Location(null));

                _catharsisManager.Stop();
                _tcpConnectionManager.Stop();
                _i2pConnectionManager.Stop();
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                _catharsisManager.Load();
                _tcpConnectionManager.Load();
                _i2pConnectionManager.Load();
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _catharsisManager.Save();
                _tcpConnectionManager.Save();
                _i2pConnectionManager.Save();
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _catharsisManager.Dispose();
                _tcpConnectionManager.Dispose();
                _i2pConnectionManager.Dispose();
            }
        }
    }
}
