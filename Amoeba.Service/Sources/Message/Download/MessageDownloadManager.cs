using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Core;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Utilities;

namespace Amoeba.Service
{
    class MessageDownloadManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;

        private Settings _settings;

        private HashSet<Signature> _searchSignatures = new HashSet<Signature>();

        private VolatileHashDictionary<Metadata, Link> _links;
        private VolatileHashDictionary<Metadata, Profile> _profiles;
        private VolatileHashDictionary<Metadata, Store> _stores;
        private VolatileHashDictionary<Metadata, Mail> _mails;

        private WatchTimer _watchTimer;

        private Random _random = new Random();

        private ManagerState _state = ManagerState.Stop;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public MessageDownloadManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = coreManager;

            _settings = new Settings(configPath);

            _links = new VolatileHashDictionary<Metadata, Link>(new TimeSpan(0, 30, 0));
            _profiles = new VolatileHashDictionary<Metadata, Profile>(new TimeSpan(0, 30, 0));
            _stores = new VolatileHashDictionary<Metadata, Store>(new TimeSpan(0, 30, 0));
            _mails = new VolatileHashDictionary<Metadata, Mail>(new TimeSpan(0, 30, 0));

            _watchTimer = new WatchTimer(this.WatchTimer, new TimeSpan(0, 0, 30));
        }

        private void WatchTimer()
        {
            _links.Update();
            _profiles.Update();
            _stores.Update();
            _mails.Update();
        }

        public IEnumerable<Signature> SearchSignatures
        {
            get
            {
                lock (_thisLock)
                {
                    return _searchSignatures.ToArray();
                }
            }
        }

        public void SetSearchSignatures(IEnumerable<Signature> signatures)
        {
            lock (_thisLock)
            {
                _searchSignatures.Clear();
                _searchSignatures.UnionWith(signatures);
            }
        }

        public Profile GetProfile(Signature signature)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            lock (_thisLock)
            {
                var broadcastMetadata = _coreManager.GetBroadcastMessage(signature, "Profile");
                if (broadcastMetadata == null) return null;

                Profile result;

                if (!_cache_BroadcastProfiles.TryGetValue(broadcastMetadata, out result))
                {
                    BackgroundDownloadItem item;

                    if (!_downloadItems.TryGetValue(broadcastMetadata.Metadata, out item))
                    {
                        item = new BackgroundDownloadItem();
                        item.Depth = 1;
                        item.State = BackgroundDownloadState.Downloading;
                        item.UpdateTime = DateTime.UtcNow;

                        _cacheManager.Lock(broadcastMetadata.Metadata.Key);

                        _downloadItems.Add(broadcastMetadata.Metadata, item);
                    }
                    else
                    {
                        item.UpdateTime = DateTime.UtcNow;

                        if (item.Stream != null)
                        {
                            item.Stream.Seek(0, SeekOrigin.Begin);
                            result = ContentConverter.FromProfileStream(item.Stream);
                            _cache_BroadcastProfiles[broadcastMetadata] = result;
                        }
                    }
                }

                return result;
            }
        }

        public override ManagerState State
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

                return _state;
            }
        }

        private readonly object _stateLock = new object();

        public override void Start()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;
                }
            }
        }

        public override void Stop()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_thisLock)
            {
                int version = _settings.Load("Version", () => 0);

                this.SetSearchSignatures(_settings.Load("SearchSignatures", () => new Signature[0]));
            }
        }

        public void Save()
        {
            lock (_thisLock)
            {
                _settings.Save("Version", 0);

                _settings.Save("SearchSignatures", this.SearchSignatures);
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

    [Serializable]
    class MessageDownloadManagerException : ManagerException
    {
        public MessageDownloadManagerException() : base() { }
        public MessageDownloadManagerException(string message) : base(message) { }
        public MessageDownloadManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
