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
using System.Runtime.Serialization;
using System.Collections;

namespace Amoeba.Service
{
    class ContentManager : ManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;

        private Settings _settings;

        private WatchTimer _watchTimer;

        private List<DownloadItemInfo> _downloadItemInfos = new List<DownloadItemInfo>();

        private Random _random = new Random();

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public ContentManager(string configPath, CoreManager coreManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = coreManager;

            _settings = new Settings(configPath);

            _watchTimer = new WatchTimer(this.WatchTimer);
            _watchTimer.Start(new TimeSpan(0, 0, 30));
        }

        private void WatchTimer()
        {
            lock (_lockObject)
            {

            }
        }

        public IEnumerable<Information> GetDownloadInformations()
        {
            lock (_lockObject)
            {
                var dic = new Dictionary<Metadata, Information>();

                foreach (var info in _coreManager.GetDownloadInformations())
                {
                    dic[info.GetValue<Metadata>("Metadata")] = info;
                }

                var list = new List<Information>();

                foreach (var item in _downloadItemInfos)
                {
                    if (!dic.TryGetValue(item.Seed.Metadata, out var info)) continue;

                    var contexts = new List<InformationContext>();
                    {
                        contexts.Add(new InformationContext("Seed", item.Seed));
                        contexts.Add(new InformationContext("Path", item.Path));

                        contexts.AddRange(info.Where(n => n.Key != "Metadata"));
                    }

                    list.Add(new Information(contexts));
                }

                return list;
            }
        }

        public void Add(Seed seed, string path)
        {
            lock (_lockObject)
            {
                if (_downloadItemInfos.Any(n => n.Seed == seed && n.Path == path)) return;

                _downloadItemInfos.Add(new DownloadItemInfo(seed, path));

                _coreManager.AddDownload(seed.Metadata, seed.Length);
            }
        }

        public void Remove(Seed seed, string path)
        {
            lock (_lockObject)
            {
                _downloadItemInfos.RemoveAll(n => n.Seed == seed && n.Path == path);

                if (!_downloadItemInfos.Any(n => n.Seed.Metadata == seed.Metadata))
                {
                    _coreManager.RemoveDownload(seed.Metadata);
                }
            }
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

        [DataContract(Name = nameof(DownloadItemInfo))]
        private class DownloadItemInfo
        {
            private Seed _seed;
            private string _path;

            public DownloadItemInfo(Seed seed, string path)
            {
                this.Seed = seed;
                this.Path = path;
            }

            [DataMember(Name = nameof(Seed))]
            public Seed Seed
            {
                get
                {
                    return _seed;
                }
                private set
                {
                    _seed = value;
                }
            }

            [DataMember(Name = nameof(Path))]
            public string Path
            {
                get
                {
                    return _path;
                }
                set
                {
                    _path = value;
                }
            }
        }

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

    [Serializable]
    class ContentManagerException : ManagerException
    {
        public ContentManagerException() : base() { }
        public ContentManagerException(string message) : base(message) { }
        public ContentManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
