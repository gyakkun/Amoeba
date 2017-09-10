using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;

namespace Amoeba.Interface
{
    class MessageManager : ManagerBase, ISettings
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private LockedHashSet<Signature> _trustSignatures = new LockedHashSet<Signature>();
        private LockedHashDictionary<Signature, BroadcastMessage<Profile>> _cacheProfiles = new LockedHashDictionary<Signature, BroadcastMessage<Profile>>();
        private LockedHashDictionary<Signature, BroadcastMessage<Store>> _cacheStores = new LockedHashDictionary<Signature, BroadcastMessage<Store>>();

        private TaskManager _watchTaskManager;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public MessageManager(string configPath, ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

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

                if (token.WaitHandle.WaitOne(1000 * 60)) return;
            }
        }

        private void UpdateProfiles()
        {
            var searchSignatures = new HashSet<Signature>();

            {
                var profiles = new HashSet<BroadcastMessage<Profile>>();

                foreach (var leaderSignature in SettingsManager.Instance.SubscribeSignatures.ToArray())
                {
                    var targetProfiles = new List<BroadcastMessage<Profile>>();

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

            searchSignatures.Add(SettingsManager.Instance.AccountInfo.DigitalSignature.GetSignature());

            lock (_serviceManager.LockObject)
            {
                var oldConfig = _serviceManager.Config;
                _serviceManager.SetConfig(new ServiceConfig(oldConfig.Core, oldConfig.Connection, new MessageConfig(searchSignatures)));
            }
        }

        private IEnumerable<BroadcastMessage<Profile>> GetProfiles(IEnumerable<Signature> trustSignatures)
        {
            var profiles = new List<BroadcastMessage<Profile>>();

            foreach (var trustSignature in trustSignatures)
            {
                var profile = _serviceManager.GetProfile(trustSignature).Result;

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
            var stores = new List<BroadcastMessage<Store>>();

            foreach (var trustSignature in _trustSignatures)
            {
                var store = _serviceManager.GetStore(trustSignature).Result;

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

        public IEnumerable<RelationSignatureInfo> GetRelationSignatureInfos()
        {
            var infos = new List<RelationSignatureInfo>();

            foreach (var signature in SettingsManager.Instance.SubscribeSignatures)
            {
                infos.Add(this.GetRelationSignatureInfo(signature));
            }

            return infos;
        }

        private RelationSignatureInfo GetRelationSignatureInfo(Signature leaderSignature)
        {
            var infos = new List<RelationSignatureInfo>();
            var workInfos = new List<RelationSignatureInfo>();

            var checkedSignatures = new HashSet<Signature>();
            var workCheckedSignatures = new HashSet<Signature>();

            {
                _cacheProfiles.TryGetValue(leaderSignature, out var leaderProfile);

                var info = new RelationSignatureInfo();
                info.Signature = leaderSignature;
                info.Profile = leaderProfile;

                infos.Add(info);
                checkedSignatures.Add(leaderSignature);
            }

            {
                int index = 0;

                for (; ; )
                {
                    for (; index < infos.Count && index < 32 * 1024; index++)
                    {
                        var targetInfo = infos[index];
                        if (targetInfo.Profile == null) continue;

                        var sortedList = targetInfo.Profile.Value.TrustSignatures.ToList();
                        sortedList.Sort((x, y) => x.ToString().CompareTo(y.ToString()));

                        foreach (var trustSignature in sortedList)
                        {
                            if (checkedSignatures.Contains(trustSignature)) continue;

                            _cacheProfiles.TryGetValue(trustSignature, out var trustProfile);

                            var info = new RelationSignatureInfo();
                            info.Signature = trustSignature;
                            info.Profile = trustProfile;

                            infos[index].Children.Add(info);

                            workInfos.Add(info);
                            workCheckedSignatures.Add(trustSignature);
                        }
                    }

                    if (workInfos.Count == 0) break;

                    infos.AddRange(workInfos);
                    workInfos.Clear();

                    checkedSignatures.UnionWith(workCheckedSignatures);
                    workCheckedSignatures.Clear();
                }
            }

            return infos[0];
        }

        public IEnumerable<BroadcastMessage<Profile>> GetProfiles()
        {
            var profiles = new List<BroadcastMessage<Profile>>();

            foreach (var trustSignature in _trustSignatures)
            {
                var profile = this.GetProfile(trustSignature);
                if (profile == null) continue;

                profiles.Add(profile);
            }

            return profiles;
        }

        public BroadcastMessage<Profile> GetProfile(Signature trustSignature)
        {
            _cacheProfiles.TryGetValue(trustSignature, out var cachedProfile);
            return cachedProfile;
        }

        public IEnumerable<BroadcastMessage<Store>> GetStores()
        {
            var stores = new List<BroadcastMessage<Store>>();

            foreach (var trustSignature in _trustSignatures)
            {
                var store = this.GetStore(trustSignature);
                if (store == null) continue;

                stores.Add(store);
            }

            return stores;
        }

        public BroadcastMessage<Store> GetStore(Signature trustSignature)
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

                foreach (var profile in _settings.Load("CacheProfiles", () => new List<BroadcastMessage<Profile>>()))
                {
                    _cacheProfiles.Add(profile.AuthorSignature, profile);
                }

                foreach (var store in _settings.Load("CacheStores", () => new List<BroadcastMessage<Store>>()))
                {
                    _cacheStores.Add(store.AuthorSignature, store);
                }

                _watchTaskManager.Start();
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                _settings.Save("CacheProfiles", _cacheProfiles.Select(n => n.Value).ToList());
                _settings.Save("CacheStores", _cacheStores.Select(n => n.Value).ToList());
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();
            }
        }
    }
}
