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
using Omnius.Serialization;
using System.Collections.Concurrent;

namespace Amoeba.Service
{
    class MessageDownloadManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;

        private Settings _settings;

        private LockedHashSet<Signature> _searchSignatures = new LockedHashSet<Signature>();

        private VolatileHashDictionary<BroadcastMetadata, BroadcastMessage<Profile>> _cache_Profiles;
        private VolatileHashDictionary<BroadcastMetadata, BroadcastMessage<Store>> _cache_Stores;
        private VolatileHashDictionary<Signature, LockedHashDictionary<UnicastMetadata, UnicastMessage<MailMessage>>> _cache_MailMessages;
        private VolatileHashDictionary<Tag, LockedHashDictionary<MulticastMetadata, MulticastMessage<ChatMessage>>> _cache_ChatMessages;

        private WatchTimer _watchTimer;

        private Random _random = new Random();

        private ManagerState _state = ManagerState.Stop;

        private readonly object _syncObject = new object();
        private volatile bool _disposed;

        public MessageDownloadManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = coreManager;

            _settings = new Settings(configPath);

            _cache_Profiles = new VolatileHashDictionary<BroadcastMetadata, BroadcastMessage<Profile>>(new TimeSpan(0, 30, 0));
            _cache_Stores = new VolatileHashDictionary<BroadcastMetadata, BroadcastMessage<Store>>(new TimeSpan(0, 30, 0));
            _cache_MailMessages = new VolatileHashDictionary<Signature, LockedHashDictionary<UnicastMetadata, UnicastMessage<MailMessage>>>(new TimeSpan(0, 30, 0));
            _cache_ChatMessages = new VolatileHashDictionary<Tag, LockedHashDictionary<MulticastMetadata, MulticastMessage<ChatMessage>>>(new TimeSpan(0, 30, 0));

            _watchTimer = new WatchTimer(this.WatchTimer, new TimeSpan(0, 0, 30));
        }

        private void WatchTimer()
        {
            lock (_syncObject)
            {
                _cache_Profiles.Update();
                _cache_Stores.Update();
                _cache_MailMessages.Update();
                _cache_ChatMessages.Update();
            }
        }

        public IEnumerable<Signature> SearchSignatures
        {
            get
            {
                lock (_syncObject)
                {
                    return _searchSignatures.ToArray();
                }
            }
        }

        public void SetSearchSignatures(IEnumerable<Signature> signatures)
        {
            lock (_syncObject)
            {
                _searchSignatures.Clear();
                _searchSignatures.UnionWith(signatures);
            }
        }

        private Task<BroadcastMessage<Profile>> GetProfile(Signature signature)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var broadcastMetadata = _coreManager.GetBroadcastMetadata(signature, "Profile");
            if (broadcastMetadata == null) return Task.FromResult<BroadcastMessage<Profile>>(null);

            // Cache
            {
                BroadcastMessage<Profile> result;

                if (_cache_Profiles.TryGetValue(broadcastMetadata, out result))
                {
                    return Task.FromResult(result);
                }
            }

            var task = _coreManager.GetStream(broadcastMetadata.Metadata, 1024 * 1024 * 32)
                .ContinueWith((t) =>
                {
                    var stream = t.Result;
                    if (stream == null) return null;

                    var result = new BroadcastMessage<Profile>(
                        broadcastMetadata.Certificate.GetSignature(),
                        broadcastMetadata.CreationTime,
                        ContentConverter.FromStream<Profile>(0, stream));

                    _cache_Profiles[broadcastMetadata] = result;

                    return result;
                });

            return task;
        }

        private Task<BroadcastMessage<Store>> GetStore(Signature signature)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var broadcastMetadata = _coreManager.GetBroadcastMetadata(signature, "Store");
            if (broadcastMetadata == null) return Task.FromResult<BroadcastMessage<Store>>(null);

            // Cache
            {
                BroadcastMessage<Store> result;

                if (_cache_Stores.TryGetValue(broadcastMetadata, out result))
                {
                    return Task.FromResult(result);
                }
            }

            var task = _coreManager.GetStream(broadcastMetadata.Metadata, 1024 * 1024 * 32)
                .ContinueWith((t) =>
                {
                    var stream = t.Result;
                    if (stream == null) return null;

                    var result = new BroadcastMessage<Store>(
                        broadcastMetadata.Certificate.GetSignature(),
                        broadcastMetadata.CreationTime,
                        ContentConverter.FromStream<Store>(0, stream));

                    _cache_Stores[broadcastMetadata] = result;

                    return result;
                });

            return task;
        }

        private Task<IEnumerable<UnicastMessage<MailMessage>>> GetMailMessages(Signature signature, IExchangeDecrypt exchangePrivateKey)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));
            if (exchangePrivateKey == null) throw new ArgumentNullException(nameof(exchangePrivateKey));

            return Task.Run(() =>
            {
                var results = new List<UnicastMessage<MailMessage>>();

                foreach (var unicastMetadata in _coreManager.GetUnicastMetadatas(signature, "MailMessage"))
                {
                    if (!_searchSignatures.Contains(unicastMetadata.Certificate.GetSignature())) continue;

                    var dic = _cache_MailMessages.GetOrAdd(unicastMetadata.Signature, (_) => new LockedHashDictionary<UnicastMetadata, UnicastMessage<MailMessage>>());

                    // Cache
                    {
                        UnicastMessage<MailMessage> result;

                        if (dic.TryGetValue(unicastMetadata, out result))
                        {
                            results.Add(result);

                            continue;
                        }
                    }

                    {
                        var stream = _coreManager.GetStream(unicastMetadata.Metadata, 1024 * 1024 * 1).Result;
                        if (stream == null) continue;

                        var result = new UnicastMessage<MailMessage>(
                            unicastMetadata.Signature,
                            unicastMetadata.Certificate.GetSignature(),
                            unicastMetadata.CreationTime,
                            ContentConverter.FromStream<MailMessage>(0, stream));

                        dic[unicastMetadata] = result;

                        results.Add(result);
                    }
                }

                return (IEnumerable<UnicastMessage<MailMessage>>)results.ToArray();
            });
        }

        private Task<IEnumerable<MulticastMessage<ChatMessage>>> GetChatMessages(Tag tag, IExchangeDecrypt exchangePrivateKey)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (exchangePrivateKey == null) throw new ArgumentNullException(nameof(exchangePrivateKey));

            var now = DateTime.UtcNow;

            return Task.Run(() =>
            {
                var results = new List<MulticastMessage<ChatMessage>>();

                foreach (var multiastMetadata in _coreManager.GetMulticastMetadatas(tag, "ChatMessage"))
                {
                    if (!_searchSignatures.Contains(multiastMetadata.Certificate.GetSignature()))
                    {
                        if ((now - multiastMetadata.CreationTime).TotalDays > 7) continue;
                    }

                    var dic = _cache_ChatMessages.GetOrAdd(multiastMetadata.Tag, (_) => new LockedHashDictionary<MulticastMetadata, MulticastMessage<ChatMessage>>());

                    // Cache
                    {
                        MulticastMessage<ChatMessage> result;

                        if (dic.TryGetValue(multiastMetadata, out result))
                        {
                            results.Add(result);

                            continue;
                        }
                    }

                    {
                        var stream = _coreManager.GetStream(multiastMetadata.Metadata, 1024 * 1024 * 1).Result;
                        if (stream == null) continue;

                        var result = new MulticastMessage<ChatMessage>(
                            multiastMetadata.Tag,
                            multiastMetadata.Certificate.GetSignature(),
                            multiastMetadata.CreationTime,
                            multiastMetadata.Cost,
                            ContentConverter.FromStream<ChatMessage>(0, stream));

                        dic[multiastMetadata] = result;

                        results.Add(result);
                    }
                }

                return (IEnumerable<MulticastMessage<ChatMessage>>)results.ToArray();
            });
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
                lock (_syncObject)
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
                lock (_syncObject)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_syncObject)
            {
                int version = _settings.Load("Version", () => 0);

                this.SetSearchSignatures(_settings.Load("SearchSignatures", () => new Signature[0]));
            }
        }

        public void Save()
        {
            lock (_syncObject)
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
