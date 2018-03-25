using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Utils;

namespace Amoeba.Service
{
    sealed partial class MessageManager : ManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;

        private Settings _settings;

        private LockedHashSet<Signature> _searchSignatures = new LockedHashSet<Signature>();

        private VolatileHashDictionary<BroadcastMetadata, BroadcastProfileMessage> _cache_Profiles;
        private VolatileHashDictionary<BroadcastMetadata, BroadcastStoreMessage> _cache_Stores;
        private VolatileHashDictionary<Signature, LockedHashDictionary<UnicastMetadata, UnicastCommentMessage>> _cache_MailMessages;
        private VolatileHashDictionary<Tag, LockedHashDictionary<MulticastMetadata, MulticastCommentMessage>> _cache_ChatMessages;

        private WatchTimer _watchTimer;

        private Random _random = new Random();

        private readonly object _lockObject = new object();
        private volatile bool _isDisposed;

        public MessageManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = coreManager;

            _settings = new Settings(configPath);

            _cache_Profiles = new VolatileHashDictionary<BroadcastMetadata, BroadcastProfileMessage>(new TimeSpan(0, 30, 0));
            _cache_Stores = new VolatileHashDictionary<BroadcastMetadata, BroadcastStoreMessage>(new TimeSpan(0, 30, 0));
            _cache_MailMessages = new VolatileHashDictionary<Signature, LockedHashDictionary<UnicastMetadata, UnicastCommentMessage>>(new TimeSpan(0, 30, 0));
            _cache_ChatMessages = new VolatileHashDictionary<Tag, LockedHashDictionary<MulticastMetadata, MulticastCommentMessage>>(new TimeSpan(0, 30, 0));

            _watchTimer = new WatchTimer(this.WatchTimer);
            _watchTimer.Start(new TimeSpan(0, 0, 30));

            _coreManager.GetLockSignaturesEvent = (_) => this.Config.SearchSignatures;
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

        public MessageConfig Config
        {
            get
            {
                lock (_lockObject)
                {
                    return new MessageConfig(_searchSignatures);
                }
            }
        }

        public void SetConfig(MessageConfig config)
        {
            lock (_lockObject)
            {
                _searchSignatures.Clear();
                _searchSignatures.UnionWith(config.SearchSignatures);
            }
        }

        public Task<BroadcastProfileMessage> GetProfile(Signature signature)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var broadcastMetadata = _coreManager.GetBroadcastMetadata(signature, "Profile");
            if (broadcastMetadata == null) return Task.FromResult<BroadcastProfileMessage>(null);

            // Cache
            {
                if (_cache_Profiles.TryGetValue(broadcastMetadata, out var result))
                {
                    return Task.FromResult(result);
                }
            }

            return Task.Run(() =>
            {
                try
                {
                    var stream = _coreManager.VolatileGetStream(broadcastMetadata.Metadata, 1024 * 1024 * 32);
                    if (stream == null) return null;

                    var content = ContentConverter.FromStream<ProfileContent>(stream, 0);
                    if (content == null) return null;

                    var result = new BroadcastProfileMessage(
                        broadcastMetadata.Certificate.GetSignature(),
                        broadcastMetadata.CreationTime,
                        content);

                    _cache_Profiles[broadcastMetadata] = result;

                    return result;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return null;
            });
        }

        public Task<BroadcastStoreMessage> GetStore(Signature signature)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var broadcastMetadata = _coreManager.GetBroadcastMetadata(signature, "Store");
            if (broadcastMetadata == null) return Task.FromResult<BroadcastStoreMessage>(null);

            // Cache
            {
                if (_cache_Stores.TryGetValue(broadcastMetadata, out var result))
                {
                    return Task.FromResult(result);
                }
            }

            return Task.Run(() =>
            {
                try
                {
                    var stream = _coreManager.VolatileGetStream(broadcastMetadata.Metadata, 1024 * 1024 * 32);
                    if (stream == null) return null;

                    var content = ContentConverter.FromStream<StoreContent>(stream, 0);
                    if (content == null) return null;

                    var result = new BroadcastStoreMessage(
                        broadcastMetadata.Certificate.GetSignature(),
                        broadcastMetadata.CreationTime,
                        content);

                    _cache_Stores[broadcastMetadata] = result;

                    return result;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return null;
            });
        }

        public Task<IEnumerable<UnicastCommentMessage>> GetUnicastCommentMessages(Signature signature, AgreementPrivateKey agreementPrivateKey)
        {
            if (signature == null) throw new ArgumentNullException(nameof(signature));
            if (agreementPrivateKey == null) throw new ArgumentNullException(nameof(agreementPrivateKey));

            return Task.Run(() =>
            {
                try
                {
                    var results = new List<UnicastCommentMessage>();

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
                        var dic = _cache_MailMessages.GetOrAdd(unicastMetadata.Signature, (_) => new LockedHashDictionary<UnicastMetadata, UnicastCommentMessage>());

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

                            var result = new UnicastCommentMessage(
                                unicastMetadata.Signature,
                                unicastMetadata.Certificate.GetSignature(),
                                unicastMetadata.CreationTime,
                                ContentConverter.FromCryptoStream<CommentContent>(stream, agreementPrivateKey, 0));

                            if (result.Value == null) continue;

                            dic[unicastMetadata] = result;

                            results.Add(result);
                        }
                    }

                    return (IEnumerable<UnicastCommentMessage>)results.ToArray();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return Array.Empty<UnicastCommentMessage>();
            });
        }

        public Task<IEnumerable<MulticastCommentMessage>> GetMulticastCommentMessages(Tag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            var now = DateTime.UtcNow;

            return Task.Run(() =>
            {
                try
                {
                    var results = new List<MulticastCommentMessage>();

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
                        var dic = _cache_ChatMessages.GetOrAdd(multicastMetadata.Tag, (_) => new LockedHashDictionary<MulticastMetadata, MulticastCommentMessage>());

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

                            var content = ContentConverter.FromStream<CommentContent>(stream, 0);
                            if (content == null) continue;

                            var result = new MulticastCommentMessage(
                                multicastMetadata.Tag,
                                multicastMetadata.Certificate.GetSignature(),
                                multicastMetadata.CreationTime,
                                multicastMetadata.Cost,
                                content);

                            dic[multicastMetadata] = result;

                            results.Add(result);
                        }
                    }

                    return (IEnumerable<MulticastCommentMessage>)results.ToArray();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

                return Array.Empty<MulticastCommentMessage>();
            });
        }

        public Task Upload(ProfileContent profile, DigitalSignature digitalSignature, CancellationToken token)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (digitalSignature == null) throw new ArgumentNullException(nameof(digitalSignature));

            return _coreManager.VolatileSetStream(ContentConverter.ToStream(profile, 0), TimeSpan.FromDays(360), token)
                .ContinueWith(task =>
                {
                    _coreManager.UploadMetadata(new BroadcastMetadata("Profile", DateTime.UtcNow, task.Result, digitalSignature));
                });
        }

        public Task Upload(StoreContent store, DigitalSignature digitalSignature, CancellationToken token)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (digitalSignature == null) throw new ArgumentNullException(nameof(digitalSignature));

            return _coreManager.VolatileSetStream(ContentConverter.ToStream(store, 0), TimeSpan.FromDays(360), token)
                .ContinueWith(task =>
                {
                    _coreManager.UploadMetadata(new BroadcastMetadata("Store", DateTime.UtcNow, task.Result, digitalSignature));
                });
        }

        public Task Upload(Signature targetSignature, CommentContent comment, AgreementPublicKey agreementPublicKey, DigitalSignature digitalSignature, CancellationToken token)
        {
            if (targetSignature == null) throw new ArgumentNullException(nameof(targetSignature));
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            if (digitalSignature == null) throw new ArgumentNullException(nameof(digitalSignature));

            return _coreManager.VolatileSetStream(ContentConverter.ToCryptoStream(comment, 1024 * 256, agreementPublicKey, 0), TimeSpan.FromDays(360), token)
                .ContinueWith(task =>
                {
                    _coreManager.UploadMetadata(new UnicastMetadata("MailMessage", targetSignature, DateTime.UtcNow, task.Result, digitalSignature));
                });
        }

        public Task Upload(Tag tag, CommentContent comment, DigitalSignature digitalSignature, TimeSpan miningTime, CancellationToken token)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            if (digitalSignature == null) throw new ArgumentNullException(nameof(digitalSignature));

            return _coreManager.VolatileSetStream(ContentConverter.ToStream(comment, 0), TimeSpan.FromDays(360), token)
                .ContinueWith(task =>
                {
                    MulticastMetadata multicastMetadata;

                    try
                    {
                        var miner = new Miner(CashAlgorithm.Version1, -1, miningTime);
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

                {
                    var searchSignatures = _settings.Load("SearchSignatures", () => Array.Empty<Signature>());

                    this.SetConfig(new MessageConfig(searchSignatures));
                }
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                {
                    var config = this.Config;

                    _settings.Save("SearchSignatures", config.SearchSignatures);
                }
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

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
