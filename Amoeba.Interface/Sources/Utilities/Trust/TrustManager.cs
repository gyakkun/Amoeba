using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Utilities;
using Amoeba.Service;
using Omnius.Collections;
using Omnius.Security;

namespace Amoeba.Interface
{
    class TrustManager : ManagerBase, ISettings
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private LockedHashDictionary<Signature, BroadcastMessage<Profile>> _cacheProfiles = new LockedHashDictionary<Signature, BroadcastMessage<Profile>>();

        private TaskManager _watchTaskManager;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public TrustManager(string configPath, ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            _settings = new Settings(configPath);

            _watchTaskManager = new TaskManager(this.WatchThread);
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                this.Refresh();

                if (token.WaitHandle.WaitOne(1000 * 60)) return;
            }
        }

        private void Refresh()
        {
            var trustSignatures = new HashSet<Signature>();

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

                trustSignatures.UnionWith(profiles.Select(n => n.AuthorSignature).ToArray());
            }

            Inspector.SetTrustSignatures(trustSignatures);
            _serviceManager.SetSearchSignatures(trustSignatures);
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

                for (;;)
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

                _watchTaskManager.Start();
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                _settings.Save("CacheProfiles", _cacheProfiles.Select(n => n.Value).ToList());
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
