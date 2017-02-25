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
    class MessageManager : ManagerBase, ISettings
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

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public MessageManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
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
            lock (_lockObject)
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
                lock (_lockObject)
                {
                    return _searchSignatures.ToArray();
                }
            }
        }

        public void SetSearchSignatures(IEnumerable<Signature> signatures)
        {
            lock (_lockObject)
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

                var trusts = new List<UnicastMetadata>();

                foreach (var unicastMetadata in _coreManager.GetUnicastMetadatas(signature, "MailMessage"))
                {
                    if (_searchSignatures.Contains(unicastMetadata.Certificate.GetSignature()))
                    {
                        trusts.Add(unicastMetadata);
                    }
                }

                trusts.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));

                foreach (var unicastMetadata in trusts.Take(1024))
                {
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

                var trusts = new List<MulticastMetadata>();
                var untrusts = new List<MulticastMetadata>();

                foreach (var multicastMetadata in _coreManager.GetMulticastMetadatas(tag, "ChatMessage"))
                {
                    if (_searchSignatures.Contains(multicastMetadata.Certificate.GetSignature()))
                    {
                        trusts.Add(multicastMetadata);
                    }
                    else
                    {
                        if ((now - multicastMetadata.CreationTime).TotalDays > 7) continue;

                        untrusts.Add(multicastMetadata);
                    }
                }

                trusts.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));
                untrusts.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));

                foreach (var multicastMetadata in CollectionUtils.Unite(trusts.Take(1024), untrusts.Take(32)))
                {
                    var dic = _cache_ChatMessages.GetOrAdd(multicastMetadata.Tag, (_) => new LockedHashDictionary<MulticastMetadata, MulticastMessage<ChatMessage>>());

                    // Cache
                    {
                        MulticastMessage<ChatMessage> result;

                        if (dic.TryGetValue(multicastMetadata, out result))
                        {
                            results.Add(result);

                            continue;
                        }
                    }

                    {
                        var stream = _coreManager.GetStream(multicastMetadata.Metadata, 1024 * 1024 * 1).Result;
                        if (stream == null) continue;

                        var result = new MulticastMessage<ChatMessage>(
                            multicastMetadata.Tag,
                            multicastMetadata.Certificate.GetSignature(),
                            multicastMetadata.CreationTime,
                            multicastMetadata.Cost,
                            ContentConverter.FromStream<ChatMessage>(0, stream));

                        dic[multicastMetadata] = result;

                        results.Add(result);
                    }
                }

                return (IEnumerable<MulticastMessage<ChatMessage>>)results.ToArray();
            });
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);

                this.SetSearchSignatures(_settings.Load("SearchSignatures", () => new Signature[0]));
            }
        }

        public void Save()
        {
            lock (_lockObject)
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
