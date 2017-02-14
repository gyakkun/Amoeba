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

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public CoreManager(string configPath, string tempPath, string blocksPath, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _cacheManager = new CacheManager(Path.Combine(configPath, "CacheManager"), blocksPath, _bufferManager);
            _networkManager = new NetworkManager(Path.Combine(configPath, "NetworkManager"), _cacheManager, _bufferManager);
            _downloadManager = new DownloadManager(Path.Combine(configPath, "DownloadManager"), tempPath, _networkManager, _cacheManager, _bufferManager);

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

                lock (this.ThisLock)
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

                lock (this.ThisLock)
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

                lock (this.ThisLock)
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

                lock (this.ThisLock)
                {
                    return _networkManager.ConnectionCountLimit;
                }
            }
            set
            {
                this.Check();

                lock (this.ThisLock)
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

                lock (this.ThisLock)
                {
                    return _networkManager.BandwidthLimit;
                }
            }
            set
            {
                this.Check();

                lock (this.ThisLock)
                {
                    _networkManager.BandwidthLimit = value;
                }
            }
        }

        public long ReceivedByteCount
        {
            get
            {
                this.Check();

                lock (this.ThisLock)
                {
                    return _networkManager.ReceivedByteCount;
                }
            }
        }

        public long SentByteCount
        {
            get
            {
                this.Check();

                lock (this.ThisLock)
                {
                    return _networkManager.SentByteCount;
                }
            }
        }

        public long Size
        {
            get
            {
                this.Check();

                lock (this.ThisLock)
                {
                    return _cacheManager.Size;
                }
            }
        }

        public IEnumerable<Information> GetConnectionInformations()
        {
            this.Check();

            lock (this.ThisLock)
            {
                return _networkManager.GetConnectionInformations();
            }
        }

        public void SetMyLocation(Location location)
        {
            this.Check();

            lock (this.ThisLock)
            {
                _networkManager.SetMyLocation(location);
            }
        }

        public void SetCrowdLocations(IEnumerable<Location> locations)
        {
            this.Check();

            lock (this.ThisLock)
            {
                _networkManager.SetCrowdLocations(locations);
            }
        }

        public void Resize(long size)
        {
            this.Check();

            lock (this.ThisLock)
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

        public void CheckBlocks(CheckBlocksProgressEventHandler getProgressEvent)
        {
            this.Check();

            _cacheManager.CheckBlocks((object sender, int badBlockCount, int checkedBlockCount, int blockCount, out bool isStop) =>
            {
                isStop = false;
                getProgressEvent?.Invoke(this, badBlockCount, checkedBlockCount, blockCount, out isStop);
            });
        }

        public IEnumerable<Information> GetCacheInformations()
        {
            this.Check();

            lock (this.ThisLock)
            {
                return _cacheManager.GetCacheInformations();
            }
        }

        public Task<Metadata> Import(Stream stream, CancellationToken token)
        {
            this.Check();

            lock (this.ThisLock)
            {
                return _cacheManager.Import(stream, token);
            }
        }

        public void RemoveCache(Metadata metadata)
        {
            this.Check();

            lock (_thisLock)
            {
                _cacheManager.RemoveCache(metadata);
            }
        }

        public bool ContainsCache(Metadata metadata)
        {
            this.Check();

            lock (_thisLock)
            {
                return _cacheManager.ContainsCache(metadata);
            }
        }

        public IEnumerable<Information> GetShareInformations()
        {
            this.Check();

            lock (this.ThisLock)
            {
                return _cacheManager.GetShareInformations();
            }
        }

        public Task<Metadata> Import(string path, CancellationToken token)
        {
            this.Check();

            lock (this.ThisLock)
            {
                return _cacheManager.Import(path, token);
            }
        }


        public void RemoveShare(string path)
        {
            this.Check();

            lock (_thisLock)
            {
                _cacheManager.RemoveShare(path);
            }
        }

        public bool ContainsShare(string path)
        {
            this.Check();

            lock (_thisLock)
            {
                return _cacheManager.ContainsShare(path);
            }
        }

        public BroadcastMessage GetBroadcastMessage(Signature signature, string type)
        {
            this.Check();

            lock (_thisLock)
            {
                return _networkManager.GetBroadcastMessage(signature, type);
            }
        }

        public IEnumerable<UnicastMessage> GetUnicastMessages(Signature signature, string type)
        {
            this.Check();

            lock (_thisLock)
            {
                return _networkManager.GetUnicastMessages(signature, type);
            }
        }

        public void Upload(BroadcastMessage message)
        {
            this.Check();

            lock (_thisLock)
            {
                _networkManager.Upload(message);
            }
        }

        public void Upload(UnicastMessage message)
        {
            this.Check();

            lock (_thisLock)
            {
                _networkManager.Upload(message);
            }
        }

        public Stream GetStream(Metadata metadata, long maxLength)
        {
            this.Check();

            lock (this.ThisLock)
            {
                return _downloadManager.GetStream(metadata, maxLength);
            }
        }

        public IEnumerable<Information> GetDownloadInformations()
        {
            this.Check();

            lock (this.ThisLock)
            {
                return _downloadManager.GetDownloadInformations();
            }
        }

        public Task Decoding(Metadata metadata, Stream outStream, long maxLength, CancellationToken token)
        {
            this.Check();

            lock (this.ThisLock)
            {
                return _downloadManager.Decoding(metadata, outStream, maxLength, token);
            }
        }

        public void AddDownload(Metadata metadata)
        {
            this.Check();

            lock (this.ThisLock)
            {
                _downloadManager.Add(metadata);
            }
        }

        public void RemoveDownload(Metadata metadata)
        {
            this.Check();

            lock (this.ThisLock)
            {
                _downloadManager.Remove(metadata);
            }
        }

        public void ResetDownload(Metadata metadata)
        {
            this.Check();

            lock (this.ThisLock)
            {
                _downloadManager.Reset(metadata);
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

            lock (this.ThisLock)
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

            lock (this.ThisLock)
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

            lock (this.ThisLock)
            {
                if (_isLoaded) throw new CoreManagerException("CoreManager was already loaded.");
                _isLoaded = true;

#if DEBUG
                var stopwatch = new Stopwatch();
                stopwatch.Start();
# endif

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

            lock (this.ThisLock)
            {
#if DEBUG
                var stopwatch = new Stopwatch();
                stopwatch.Start();
# endif

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

        #region IThisLock

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion
    }

    [Serializable]
    class CoreManagerException : StateManagerException
    {
        public CoreManagerException() : base() { }
        public CoreManagerException(string message) : base(message) { }
        public CoreManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
