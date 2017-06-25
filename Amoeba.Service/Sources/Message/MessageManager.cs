using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Utilities;

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

            _watchTimer = new WatchTimer(this.WatchTimer);
            _watchTimer.Start(new TimeSpan(0, 0, 30));

            _coreManager.GetLockSignaturesEvent = (_) => this.SearchSignatures;
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

        public Task<BroadcastMessage<Profile>> GetProfile(Signature signature)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var broadcastMetadata = _coreManager.GetBroadcastMetadata(signature, "Profile");
            if (broadcastMetadata == null) return Task.FromResult<BroadcastMessage<Profile>>(null);

            // Cache
            {
                if (_cache_Profiles.TryGetValue(broadcastMetadata, out var result))
                {
                    return Task.FromResult(result);
                }
            }

            return Task.Run(() =>
            {
                var stream = _coreManager.VolatileGetStream(broadcastMetadata.Metadata, 1024 * 1024 * 32);
                if (stream == null) return null;

                var result = new BroadcastMessage<Profile>(
                    broadcastMetadata.Certificate.GetSignature(),
                    broadcastMetadata.CreationTime,
                    ContentConverter.FromStream<Profile>(stream));

                if (result.Value == null) return null;

                _cache_Profiles[broadcastMetadata] = result;

                return result;
            });
        }

        public Task<BroadcastMessage<Store>> GetStore(Signature signature)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var broadcastMetadata = _coreManager.GetBroadcastMetadata(signature, "Store");
            if (broadcastMetadata == null) return Task.FromResult<BroadcastMessage<Store>>(null);

            // Cache
            {
                if (_cache_Stores.TryGetValue(broadcastMetadata, out var result))
                {
                    return Task.FromResult(result);
                }
            }

            return Task.Run(() =>
            {
                var stream = _coreManager.VolatileGetStream(broadcastMetadata.Metadata, 1024 * 1024 * 32);
                if (stream == null) return null;

                var result = new BroadcastMessage<Store>(
                    broadcastMetadata.Certificate.GetSignature(),
                    broadcastMetadata.CreationTime,
                    ContentConverter.FromStream<Store>(stream));

                if (result.Value == null) return null;

                _cache_Stores[broadcastMetadata] = result;

                return result;
            });
        }

        public Task<IEnumerable<UnicastMessage<MailMessage>>> GetMailMessages(Signature signature, IExchangeDecrypt exchangePrivateKey)
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
                        if (dic.TryGetValue(unicastMetadata, out var result))
                        {
                            results.Add(result);

                            continue;
                        }
                    }

                    {
                        var stream = _coreManager.VolatileGetStream(unicastMetadata.Metadata, 1024 * 1024 * 1);
                        if (stream == null) continue;

                        var result = new UnicastMessage<MailMessage>(
                            unicastMetadata.Signature,
                            unicastMetadata.Certificate.GetSignature(),
                            unicastMetadata.CreationTime,
                            ContentConverter.FromCryptoStream<MailMessage>(stream, exchangePrivateKey));

                        if (result.Value == null) continue;

                        dic[unicastMetadata] = result;

                        results.Add(result);
                    }
                }

                return (IEnumerable<UnicastMessage<MailMessage>>)results.ToArray();
            });
        }

        public Task<IEnumerable<MulticastMessage<ChatMessage>>> GetChatMessages(Tag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

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
                untrusts.Sort((x, y) =>
                {
                    int c;
                    if (0 != (c = y.Cost.CashAlgorithm.CompareTo(x.Cost.CashAlgorithm))) return c;
                    if (0 != (c = y.Cost.Value.CompareTo(x.Cost.Value))) return c;

                    return y.CreationTime.CompareTo(x.CreationTime);
                });

                foreach (var multicastMetadata in CollectionUtils.Unite(trusts.Take(1024), untrusts.Take(1024)))
                {
                    var dic = _cache_ChatMessages.GetOrAdd(multicastMetadata.Tag, (_) => new LockedHashDictionary<MulticastMetadata, MulticastMessage<ChatMessage>>());

                    // Cache
                    {
                        if (dic.TryGetValue(multicastMetadata, out var result))
                        {
                            results.Add(result);

                            continue;
                        }
                    }

                    {
                        var stream = _coreManager.VolatileGetStream(multicastMetadata.Metadata, 1024 * 1024 * 1);
                        if (stream == null) continue;

                        var result = new MulticastMessage<ChatMessage>(
                            multicastMetadata.Tag,
                            multicastMetadata.Certificate.GetSignature(),
                            multicastMetadata.CreationTime,
                            multicastMetadata.Cost,
                            ContentConverter.FromStream<ChatMessage>(stream));

                        if (result.Value == null) continue;

                        dic[multicastMetadata] = result;

                        results.Add(result);
                    }
                }

                return (IEnumerable<MulticastMessage<ChatMessage>>)results.ToArray();
            });
        }

        public Task Upload(Profile profile, DigitalSignature digitalSignature, CancellationToken token)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (digitalSignature == null) throw new ArgumentNullException(nameof(digitalSignature));

            return _coreManager.VolatileSetStream(ContentConverter.ToStream(profile), TimeSpan.FromDays(180), token)
                .ContinueWith(task =>
                {
                    _coreManager.UploadMetadata(new BroadcastMetadata("Profile", DateTime.UtcNow, task.Result, digitalSignature));
                });
        }

        public Task Upload(Store store, DigitalSignature digitalSignature, CancellationToken token)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (digitalSignature == null) throw new ArgumentNullException(nameof(digitalSignature));

            return _coreManager.VolatileSetStream(ContentConverter.ToStream(store), TimeSpan.FromDays(180), token)
                .ContinueWith(task =>
                {
                    _coreManager.UploadMetadata(new BroadcastMetadata("Store", DateTime.UtcNow, task.Result, digitalSignature));
                });
        }

        public Task Upload(Signature targetSignature, MailMessage mailMessage, IExchangeEncrypt exchangePublicKey, DigitalSignature digitalSignature, CancellationToken token)
        {
            if (targetSignature == null) throw new ArgumentNullException(nameof(targetSignature));
            if (mailMessage == null) throw new ArgumentNullException(nameof(mailMessage));
            if (digitalSignature == null) throw new ArgumentNullException(nameof(digitalSignature));

            return _coreManager.VolatileSetStream(ContentConverter.ToCryptoStream(mailMessage, 1024 * 256, exchangePublicKey), TimeSpan.FromDays(180), token)
                .ContinueWith(task =>
                {
                    _coreManager.UploadMetadata(new UnicastMetadata("MailMessage", targetSignature, DateTime.UtcNow, task.Result, digitalSignature));
                });
        }

        public Task Upload(Tag tag, ChatMessage chatMessage, DigitalSignature digitalSignature, Miner miner, CancellationToken token)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (chatMessage == null) throw new ArgumentNullException(nameof(chatMessage));
            if (digitalSignature == null) throw new ArgumentNullException(nameof(digitalSignature));

            return _coreManager.VolatileSetStream(ContentConverter.ToStream(chatMessage), TimeSpan.FromDays(180), token)
                .ContinueWith(task =>
                {
                    MulticastMetadata multicastMetadata;

                    try
                    {
                        multicastMetadata = new MulticastMetadata("ChatMessage", tag, DateTime.UtcNow, task.Result, digitalSignature, miner, token);
                    }
                    catch (MinerException)
                    {
                        return;
                    }

                    _coreManager.UploadMetadata(multicastMetadata);
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
                if (_watchTimer != null)
                {
                    try
                    {
                        _watchTimer.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    _watchTimer = null;
                }
            }
        }
    }

    class MessageManagerException : ManagerException
    {
        public MessageManagerException() : base() { }
        public MessageManagerException(string message) : base(message) { }
        public MessageManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
