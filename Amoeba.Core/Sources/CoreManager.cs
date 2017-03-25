using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Net;
using System.Threading;

namespace Amoeba.Core
{
    public sealed class CoreManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CacheManager _cacheManager;
        private NetworkManager _networkManager;
        private DownloadManager _downloadManager;

        private volatile ManagerState _state = ManagerState.Stop;

        private bool _isLoaded = false;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public CoreManager(string configPath, string blocksPath, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _cacheManager = new CacheManager(Path.Combine(configPath, "Cache"), blocksPath, _bufferManager);
            _networkManager = new NetworkManager(Path.Combine(configPath, "Network"), _cacheManager, _bufferManager);
            _downloadManager = new DownloadManager(Path.Combine(configPath, "Download"), _networkManager, _cacheManager, _bufferManager);

            _networkManager.ConnectCapEvent = (_, uri) => this.OnConnectCap(uri);
            _networkManager.AcceptCapEvent = (_) => this.OnAcceptCap();
            _networkManager.GetLockSignaturesEvent = (_) => this.OnGetLockSignatures();
        }

        private void Check()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);
            if (!_isLoaded) throw new CoreManagerException("CoreManager is not loaded.");
        }

        public ConnectCapEventHandler ConnectCapEvent { get; set; }
        public AcceptCapEventHandler AcceptCapEvent { get; set; }

        public GetSignaturesEventHandler GetLockSignaturesEvent { get; set; }

        private Cap OnConnectCap(string uri)
        {
            return this.ConnectCapEvent?.Invoke(this, uri);
        }

        private Cap OnAcceptCap()
        {
            return this.AcceptCapEvent?.Invoke(this);
        }

        private IEnumerable<Signature> OnGetLockSignatures()
        {
            return this.GetLockSignaturesEvent?.Invoke(this) ?? new Signature[0];
        }

        public Information Information
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    var contexts = new List<InformationContext>();
                    contexts.AddRange(_cacheManager.Information);
                    contexts.AddRange(_networkManager.Information);

                    return new Information(contexts);
                }
            }
        }

        public Location MyLocation
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _networkManager.MyLocation;
                }
            }
        }

        public IEnumerable<Location> CrowdLocations
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _networkManager.CrowdLocations;
                }
            }
        }

        public int ConnectionCountLimit
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _networkManager.ConnectionCountLimit;
                }
            }
            set
            {
                this.Check();

                lock (_lockObject)
                {
                    _networkManager.ConnectionCountLimit = value;
                }
            }
        }

        public int BandwidthLimit
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _networkManager.BandwidthLimit;
                }
            }
            set
            {
                this.Check();

                lock (_lockObject)
                {
                    _networkManager.BandwidthLimit = value;
                }
            }
        }

        public long Size
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _cacheManager.Size;
                }
            }
        }

        public string BasePath
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _downloadManager.BasePath;
                }
            }
        }

        public IEnumerable<Information> GetConnectionInformations()
        {
            this.Check();

            lock (_lockObject)
            {
                return _networkManager.GetConnectionInformations();
            }
        }

        public void SetMyLocation(Location location)
        {
            this.Check();

            lock (_lockObject)
            {
                _networkManager.SetMyLocation(location);
            }
        }

        public void SetCrowdLocations(IEnumerable<Location> locations)
        {
            this.Check();

            lock (_lockObject)
            {
                _networkManager.SetCrowdLocations(locations);
            }
        }

        public void Resize(long size)
        {
            this.Check();

            lock (_lockObject)
            {
                if (this.State == ManagerState.Start)
                {
                    _downloadManager.Stop();
                }

                _cacheManager.Resize(size);

                if (this.State == ManagerState.Start)
                {
                    _downloadManager.Start();
                }
            }
        }

        public Task CheckBlocks(IProgress<CheckBlocksProgressInfo> progress, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _cacheManager.CheckBlocks(progress, token);
            }
        }

        public Task<Metadata> VolatileSetStream(Stream stream, TimeSpan lifeSpan, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _cacheManager.Import(stream, lifeSpan, token);
            }
        }

        public Stream VolatileGetStream(Metadata metadata, long maxLength)
        {
            this.Check();

            lock (_lockObject)
            {
                return _downloadManager.GetStream(metadata, maxLength);
            }
        }

        public Task<Metadata> Import(string path, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _cacheManager.Import(path, token);
            }
        }

        public IEnumerable<Information> GetContentInformations()
        {
            this.Check();

            lock (_lockObject)
            {
                return _cacheManager.GetContentInformations();
            }
        }

        public void RemoveContent(string path)
        {
            this.Check();

            lock (_lockObject)
            {
                _cacheManager.RemoveContent(path);
            }
        }

        public IEnumerable<Information> GetDownloadInformations()
        {
            this.Check();

            lock (_lockObject)
            {
                return _downloadManager.GetDownloadInformations();
            }
        }

        public void AddDownload(Metadata metadata, string path, long maxLength)
        {
            this.Check();

            lock (_lockObject)
            {
                _downloadManager.Add(metadata, path, maxLength);
            }
        }

        public void RemoveDownload(Metadata metadata, string path)
        {
            this.Check();

            lock (_lockObject)
            {
                _downloadManager.Remove(metadata, path);
            }
        }

        public void ResetDownload(Metadata metadata, string path)
        {
            this.Check();

            lock (_lockObject)
            {
                _downloadManager.Reset(metadata, path);
            }
        }

        public void UploadMetadata(BroadcastMetadata metadata)
        {
            this.Check();

            lock (_lockObject)
            {
                _networkManager.Upload(metadata);
            }
        }

        public void UploadMetadata(UnicastMetadata metadata)
        {
            this.Check();

            lock (_lockObject)
            {
                _networkManager.Upload(metadata);
            }
        }

        public void UploadMetadata(MulticastMetadata metadata)
        {
            this.Check();

            lock (_lockObject)
            {
                _networkManager.Upload(metadata);
            }
        }

        public BroadcastMetadata GetBroadcastMetadata(Signature signature, string type)
        {
            this.Check();

            lock (_lockObject)
            {
                return _networkManager.GetBroadcastMetadata(signature, type);
            }
        }

        public IEnumerable<UnicastMetadata> GetUnicastMetadatas(Signature signature, string type)
        {
            this.Check();

            lock (_lockObject)
            {
                return _networkManager.GetUnicastMetadatas(signature, type);
            }
        }

        public IEnumerable<MulticastMetadata> GetMulticastMetadatas(Tag tag, string type)
        {
            this.Check();

            lock (_lockObject)
            {
                return _networkManager.GetMulticastMetadatas(tag, type);
            }
        }

        public override ManagerState State
        {
            get
            {
                this.Check();

                return _state;
            }
        }

        public override void Start()
        {
            this.Check();

            lock (_lockObject)
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                _networkManager.Start();
                _downloadManager.Start();
            }
        }

        public override void Stop()
        {
            this.Check();

            lock (_lockObject)
            {
                if (this.State == ManagerState.Stop) return;
                _state = ManagerState.Stop;

                _downloadManager.Stop();
                _networkManager.Stop();
            }
        }

        #region ISettings

        public void Load()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_lockObject)
            {
                if (_isLoaded) throw new CoreManagerException("CoreManager was already loaded.");
                _isLoaded = true;

#if DEBUG
                var stopwatch = new Stopwatch();
                stopwatch.Start();
#endif

                {
                    var tasks = new List<Task>();

                    tasks.Add(Task.Run(() => _cacheManager.Load()));
                    tasks.Add(Task.Run(() => _networkManager.Load()));

                    Task.WaitAll(tasks.ToArray());
                }

                {
                    var tasks = new List<Task>();

                    tasks.Add(Task.Run(() => _downloadManager.Load()));

                    Task.WaitAll(tasks.ToArray());
                }

#if DEBUG
                stopwatch.Stop();
                Debug.WriteLine("CoreManager Load: {0}", stopwatch.ElapsedMilliseconds);
#endif
            }
        }

        public void Save()
        {
            this.Check();

            lock (_lockObject)
            {
#if DEBUG
                var stopwatch = new Stopwatch();
                stopwatch.Start();
#endif

                {
                    var tasks = new List<Task>();

                    tasks.Add(Task.Run(() => _downloadManager.Save()));

                    Task.WaitAll(tasks.ToArray());
                }

                {
                    var tasks = new List<Task>();

                    tasks.Add(Task.Run(() => _networkManager.Save()));
                    tasks.Add(Task.Run(() => _cacheManager.Save()));

                    Task.WaitAll(tasks.ToArray());
                }

#if DEBUG
                stopwatch.Stop();
                Debug.WriteLine("CoreManager Save: {0}", stopwatch.ElapsedMilliseconds);
#endif
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _downloadManager.Dispose();
                _networkManager.Dispose();
                _cacheManager.Dispose();
            }
        }
    }

    [Serializable]
    class CoreManagerException : StateManagerException
    {
        public CoreManagerException() : base() { }
        public CoreManagerException(string message) : base(message) { }
        public CoreManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
