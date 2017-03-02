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
using Omnius.Utilities;

namespace Amoeba.Service
{
    class ConnectionManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;
        private I2pManager _i2pManager;

        private Settings _settings;

        private WatchTimer _watchTimer;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public ConnectionManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = coreManager;
            _i2pManager = new I2pManager(Path.Combine(configPath, "I2pManager"), _bufferManager);

            _settings = new Settings(configPath);

            _coreManager.ConnectCapEvent = (_, uri) => this.ConnectCap(uri);
            _coreManager.AcceptCapEvent = (_) => this.AcceptCap();

            _watchTimer = new WatchTimer(this.WatchThread);
        }

        public Cap ConnectCap(string uri)
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            Cap cap;
            if ((cap = _i2pManager.ConnectCap(uri)) != null) return cap;

            return null;
        }

        public Cap AcceptCap()
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            Cap cap;
            if ((cap = _i2pManager.AcceptCap()) != null) return cap;

            return null;
        }

        private void WatchThread()
        {
            var targetUris = new List<string>();

            targetUris.AddRange(_i2pManager.LocationUris);

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

        private readonly object _stateLock = new object();

        public override void Start()
        {
            lock (_stateLock)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _i2pManager.Start();

                    _watchTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 30));
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
                _coreManager.SetMyLocation(new Location(null));

                _i2pManager.Stop();
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);
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
