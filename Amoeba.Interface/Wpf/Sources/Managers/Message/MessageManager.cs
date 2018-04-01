using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amoeba.Messages;
using Amoeba.Rpc;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;

namespace Amoeba.Interface
{
    class MessageManager : ManagerBase, ISettings
    {
        private AmoebaInterfaceManager _amoebaInterfaceManager;

        private Settings _settings;

        private LockedHashSet<Signature> _trustSignatures = new LockedHashSet<Signature>();
        private LockedHashDictionary<Signature, BroadcastProfileMessage> _cacheProfiles = new LockedHashDictionary<Signature, BroadcastProfileMessage>();
        private LockedHashDictionary<Signature, BroadcastStoreMessage> _cacheStores = new LockedHashDictionary<Signature, BroadcastStoreMessage>();

        private TaskManager _watchTaskManager;

        private readonly object _lockObject = new object();
        private volatile bool _isDisposed;

        public MessageManager(string configPath, AmoebaInterfaceManager serviceManager)
        {
            _amoebaInterfaceManager = serviceManager;

            _settings = new Settings(configPath);

            _watchTaskManager = new TaskManager(this.WatchThread);
        }

        public IEnumerable<Signature> TrustSignatures
        {
            get
            {
                return _trustSignatures.ToArray();
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (; ; )
            {
                this.UpdateProfiles();
                this.UpdateStores();

                if (token.WaitHandle.WaitOne(1000 * 60 * 3)) return;
            }
        }

        private void UpdateProfiles()
        {
            var searchSignatures = new HashSet<Signature>();

            {
                var profiles = new HashSet<BroadcastProfileMessage>();

                foreach (var leaderSignature in SettingsManager.Instance.SubscribeSignatures.ToArray())
                {
                    var targetProfiles = new List<BroadcastProfileMessage>();

                    var targetSignatures = new HashSet<Signature>();
                    var checkedSignatures = new HashSet<Signature>();

                    targetSignatures.Add(leaderSignature);

                    for (int i = 0; i < 32; i++)
                    {
                        searchSignatures.UnionWith(targetSignatures);

                        var tempProfiles = this.GetProfiles(targetSignatures).ToList();
                        if (tempProfiles.Count == 0) break;

                        checkedSignatures.UnionWith(targetSignatures);
                        checkedSignatures.UnionWith(tempProfiles.SelectMany(n => n.Value.DeleteSignatures));

                        targetSignatures.Clear();
                        targetSignatures.UnionWith(tempProfiles.SelectMany(n => n.Value.TrustSignatures).Where(n => !checkedSignatures.Contains(n)));

                        targetProfiles.AddRange(tempProfiles);

                        if (targetProfiles.Count > 1024 * 32) goto End;
                    }

                    End:;

                    profiles.UnionWith(targetProfiles.Take(1024 * 32));
                }

                lock (_cacheProfiles.LockObject)
                {
                    _cacheProfiles.Clear();

                    foreach (var profile in profiles)
                    {
                        _cacheProfiles.Add(profile.AuthorSignature, profile);
                    }
                }
            }

            lock (_trustSignatures.LockObject)
            {
                _trustSignatures.Clear();
                _trustSignatures.UnionWith(searchSignatures);
            }

            searchSignatures.Add(SettingsManager.Instance.AccountSetting.DigitalSignature.GetSignature());

            lock (_amoebaInterfaceManager.LockObject)
            {
                var oldConfig = _amoebaInterfaceManager.Config;
                _amoebaInterfaceManager.SetConfig(new ServiceConfig(oldConfig.Core, oldConfig.Connection, new MessageConfig(searchSignatures)));
            }
        }

        private IEnumerable<BroadcastProfileMessage> GetProfiles(IEnumerable<Signature> trustSignatures)
        {
            var profiles = new List<BroadcastProfileMessage>();

            foreach (var trustSignature in trustSignatures)
            {
                var profile = _amoebaInterfaceManager.GetProfile(trustSignature, CancellationToken.None).Result;

                if (profile == null)
                {
                    if (_cacheProfiles.TryGetValue(trustSignature, out var cachedProfile))
                    {
                        profiles.Add(cachedProfile);
                    }
                }
                else
                {
                    if (!_cacheProfiles.TryGetValue(trustSignature, out var cachedProfile)
                        || profile.CreationTime > cachedProfile.CreationTime)
                    {
                        _cacheProfiles[trustSignature] = profile;
                        profiles.Add(profile);
                    }
                    else
                    {
                        profiles.Add(cachedProfile);
                    }
                }
            }

            return profiles;
        }

        private void UpdateStores()
        {
            var stores = new List<BroadcastStoreMessage>();

            foreach (var trustSignature in _trustSignatures)
            {
                var store = _amoebaInterfaceManager.GetStore(trustSignature, CancellationToken.None).Result;

                if (store == null)
                {
                    if (_cacheStores.TryGetValue(trustSignature, out var cachedStore))
                    {
                        stores.Add(cachedStore);
                    }
                }
                else
                {
                    if (!_cacheStores.TryGetValue(trustSignature, out var cachedStore)
                        || store.CreationTime > cachedStore.CreationTime)
                    {
                        _cacheStores[trustSignature] = store;
                        stores.Add(store);
                    }
                    else
                    {
                        stores.Add(cachedStore);
                    }
                }
            }

            lock (_cacheStores.LockObject)
            {
                _cacheStores.Clear();

                foreach (var store in stores)
                {
                    _cacheStores.Add(store.AuthorSignature, store);
                }
            }
        }

        public IEnumerable<BroadcastProfileMessage> GetProfiles()
        {
            var profiles = new List<BroadcastProfileMessage>();

            foreach (var trustSignature in _trustSignatures)
            {
                var profile = this.GetProfile(trustSignature);
                if (profile == null) continue;

                profiles.Add(profile);
            }

            return profiles;
        }

        public BroadcastProfileMessage GetProfile(Signature trustSignature)
        {
            _cacheProfiles.TryGetValue(trustSignature, out var cachedProfile);
            return cachedProfile;
        }

        public IEnumerable<BroadcastStoreMessage> GetStores()
        {
            var stores = new List<BroadcastStoreMessage>();

            foreach (var trustSignature in _trustSignatures)
            {
                var store = this.GetStore(trustSignature);
                if (store == null) continue;

                stores.Add(store);
            }

            return stores;
        }

        public BroadcastStoreMessage GetStore(Signature trustSignature)
        {
            _cacheStores.TryGetValue(trustSignature, out var cachedStore);
            return cachedStore;
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);

                foreach (var profile in _settings.Load("CacheProfiles", () => Enumerable.Empty<BroadcastProfileMessage>()))
                {
                    _cacheProfiles.Add(profile.AuthorSignature, profile);
                }

                foreach (var store in _settings.Load("CacheStores", () => Enumerable.Empty<BroadcastStoreMessage>()))
                {
                    _cacheStores.Add(store.AuthorSignature, store);
                }

                _trustSignatures.UnionWith(_settings.Load("TrustSignatures", () => Enumerable.Empty<Signature>()));

                _watchTaskManager.Start();
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                _settings.Save("CacheProfiles", _cacheProfiles.Select(n => n.Value).ToArray());
                _settings.Save("CacheStores", _cacheStores.Select(n => n.Value).ToArray());
                _settings.Save("TrustSignatures", _trustSignatures);
            }
        }

        #endregion

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();
            }
        }
    }
}