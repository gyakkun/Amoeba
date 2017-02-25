using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Io;
using Omnius.Security;
using Omnius.Utilities;

namespace Amoeba.Core
{
    class DownloadManager : StateManagerBase, ISettings
    {
        private NetworkManager _networkManager;
        private CacheManager _cacheManager;
        private BufferManager _bufferManager;

        private Settings _settings;

        private HashSet<Signature> _trustSignatures = new HashSet<Signature>();

        private Thread _downloadThread;
        private List<Thread> _decodeThreads = new List<Thread>();

        private ExistManager _existManager = new ExistManager();

        private VolatileDownloadItemInfoManager _volatileDownloadItemInfoManager;
        private DownloadItemInfoManager _downloadItemInfoManager;

        private WatchTimer _watchTimer;

        private int _threadCount = 2;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public DownloadManager(string configPath, NetworkManager networkManager, CacheManager cacheManager, BufferManager bufferManager)
        {
            _networkManager = networkManager;
            _cacheManager = cacheManager;
            _bufferManager = bufferManager;

            _settings = new Settings(configPath);

            _volatileDownloadItemInfoManager = new VolatileDownloadItemInfoManager();
            _volatileDownloadItemInfoManager.AddEvents += (info) => this.Event_AddInfo(info);
            _volatileDownloadItemInfoManager.RemoveEvents += (info) => this.Event_RemoveInfo(info);

            _downloadItemInfoManager = new DownloadItemInfoManager();
            _downloadItemInfoManager.AddEvents += (info) => this.Event_AddInfo(info);
            _downloadItemInfoManager.RemoveEvents += (info) => this.Event_RemoveInfo(info);

            _watchTimer = new WatchTimer(this.WatchThread, new TimeSpan(0, 1, 0));

            _threadCount = Math.Max(1, Math.Min(System.Environment.ProcessorCount, 32) / 2);

            _cacheManager.BlockAddEvents += (hashes) => this.Update_DownloadBlockStates(true, hashes);
            _cacheManager.BlockRemoveEvents += (hashes) => this.Update_DownloadBlockStates(false, hashes);
        }

        private void Event_AddInfo(DownloadItemInfo info)
        {
            lock (_lockObject)
            {
                _cacheManager.Lock(info.Metadata.Hash);
                this.CheckState(info.Index);
            }
        }

        private void Event_RemoveInfo(DownloadItemInfo info)
        {
            lock (_lockObject)
            {
                _cacheManager.Unlock(info.Metadata.Hash);
                this.UncheckState(info.Index);

                info.State = DownloadState.Error;
            }
        }

        private void WatchThread()
        {
            lock (_lockObject)
            {
                _volatileDownloadItemInfoManager.Update();
            }
        }

        private void Update_DownloadBlockStates(bool state, IEnumerable<Hash> hashes)
        {
            try
            {
                lock (_lockObject)
                {
                    foreach (var hash in hashes)
                    {
                        _existManager.Set(hash, state);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void CheckState(Index index)
        {
            lock (_lockObject)
            {
                if (index == null) return;

                foreach (var group in index.Groups)
                {
                    var hashes = new List<Hash>();

                    foreach (var hash in group.Hashes)
                    {
                        _cacheManager.Lock(hash);

                        if (_cacheManager.Contains(hash)) hashes.Add(hash);
                    }

                    _existManager.Add(group, hashes);
                }
            }
        }

        private void UncheckState(Index index)
        {
            lock (_lockObject)
            {
                if (index == null) return;

                foreach (var group in index.Groups)
                {
                    foreach (var hash in group.Hashes)
                    {
                        _cacheManager.Unlock(hash);
                    }

                    _existManager.Remove(group);
                }
            }
        }

        private void DownloadThread()
        {
            var random = new Random();
            int round = 0;

            for (;;)
            {
                Thread.Sleep(1000 * 1);
                if (this.State == ManagerState.Stop) return;

                DownloadItemInfo item = null;

                lock (_lockObject)
                {
                    var tempList = CollectionUtils.Unite(_volatileDownloadItemInfoManager, _downloadItemInfoManager).ToArray();

                    if (tempList.Length > 0)
                    {
                        {
                            var items = tempList
                               .Where(n => n.State == DownloadState.Downloading)
                               .Where(x =>
                               {
                                   if (x.Depth == 0) return _cacheManager.Contains(x.Metadata.Hash);
                                   else return 0 == (x.Index.Groups.Sum(n => n.Hashes.Count() / 2) - x.Index.Groups.Sum(n => Math.Min(n.Hashes.Count() / 2, _existManager.GetCount(n))));
                               })
                               .ToList();

                            item = items.FirstOrDefault();
                        }

                        if (item == null)
                        {
                            var items = tempList
                                .Where(n => n.State == DownloadState.Downloading)
                                .ToList();

                            if (items.Count > 0)
                            {
                                round = (round >= items.Count) ? 0 : round;
                                item = items[round++];
                            }
                        }
                    }
                }

                if (item == null) continue;

                try
                {
                    if (item.Depth == 0)
                    {
                        if (!_cacheManager.Contains(item.Metadata.Hash))
                        {
                            item.State = DownloadState.Downloading;

                            _networkManager.Download(item.Metadata.Hash);
                        }
                        else
                        {
                            item.State = DownloadState.Decoding;
                        }
                    }
                    else
                    {
                        if (!item.Index.Groups.All(n => _existManager.GetCount(n) >= n.Hashes.Count() / 2))
                        {
                            item.State = DownloadState.Downloading;

                            var limitCount = 1024;

                            foreach (var group in item.Index.Groups.Randomize())
                            {
                                if (_existManager.GetCount(group) >= group.Hashes.Count() / 2) continue;

                                foreach (var hash in _existManager.GetHashes(group, false))
                                {
                                    if (_networkManager.IsDownloadWaiting(hash))
                                    {
                                        if (--limitCount <= 0) goto End;
                                    }
                                }
                            }

                            foreach (var group in item.Index.Groups.Randomize())
                            {
                                if (_existManager.GetCount(group) >= group.Hashes.Count() / 2) continue;

                                var tempHashes = new List<Hash>();

                                foreach (var hash in _existManager.GetHashes(group, false))
                                {
                                    if (!_networkManager.IsDownloadWaiting(hash))
                                    {
                                        _networkManager.Download(hash);

                                        if (--limitCount <= 0) goto End;
                                    }
                                }
                            }

                            End:;
                        }
                        else
                        {
                            item.State = DownloadState.ParityDecoding;
                        }
                    }
                }
                catch (Exception e)
                {
                    item.State = DownloadState.Error;

                    Log.Error(e);
                }
            }
        }

        LockedHashSet<DownloadItemInfo> _workingItems = new LockedHashSet<DownloadItemInfo>();

        private void DecodeThread()
        {
            var random = new Random();

            for (;;)
            {
                Thread.Sleep(1000 * 3);
                if (this.State == ManagerState.Stop) return;

                DownloadItemInfo item = null;

                lock (_lockObject)
                {
                    item = CollectionUtils.Unite(_volatileDownloadItemInfoManager, _downloadItemInfoManager)
                        .Where(n => !_workingItems.Contains(n))
                        .Where(n => n.State == DownloadState.Decoding || n.State == DownloadState.ParityDecoding)
                        .OrderBy(n => (n.Depth == n.Metadata.Depth) ? 0 : 1)
                        .OrderBy(n => (n.State == DownloadState.Decoding) ? 0 : 1)
                        .FirstOrDefault();

                    if (item != null)
                    {
                        _workingItems.Add(item);
                    }
                }

                if (item == null) continue;

                try
                {
                    if ((item.Depth == 0 && !_cacheManager.Contains(item.Metadata.Hash))
                        || (item.Depth > 0 && !item.Index.Groups.All(n => _existManager.GetCount(n) >= n.Hashes.Count() / 2)))
                    {
                        item.State = DownloadState.Downloading;
                    }
                    else
                    {
                        var hashes = new HashCollection();

                        if (item.Depth == 0)
                        {
                            hashes.Add(item.Metadata.Hash);
                        }
                        else
                        {
                            try
                            {
                                foreach (var group in item.Index.Groups)
                                {
                                    using (var tokenSource = new CancellationTokenSource())
                                    {
                                        var task = _cacheManager.ParityDecoding(group, tokenSource.Token);

                                        while (!task.IsCompleted)
                                        {
                                            if (this.State == ManagerState.Stop) tokenSource.Cancel();

                                            lock (_lockObject)
                                            {
                                                if (item.State == DownloadState.Error) tokenSource.Cancel();
                                            }

                                            task.Wait(1000);
                                        }

                                        if (task.Exception != null) throw task.Exception;

                                        hashes.AddRange(task.Result);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                continue;
                            }

                            item.State = DownloadState.Decoding;
                        }

                        if (item.Depth < item.Metadata.Depth)
                        {
                            Index index;
                            {
                                try
                                {
                                    using (var stream = new BufferStream(_bufferManager))
                                    using (var tokenSource = new CancellationTokenSource())
                                    {
                                        // Write
                                        {
                                            var task = _cacheManager.Decoding(stream, hashes, (long)1024 * 1024 * 1024 * 4, tokenSource.Token);

                                            while (!task.IsCompleted)
                                            {
                                                if (this.State == ManagerState.Stop) tokenSource.Cancel();

                                                lock (_lockObject)
                                                {
                                                    if (item.State == DownloadState.Error) tokenSource.Cancel();
                                                }

                                                task.Wait(1000);
                                            }

                                            if (task.Exception != null) throw task.Exception;
                                        }

                                        stream.Seek(0, SeekOrigin.Begin);

                                        // Read
                                        {
                                            index = Index.Import(stream, _bufferManager);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }

                            lock (_lockObject)
                            {
                                this.UncheckState(item.Index);

                                item.Index = index;

                                this.CheckState(item.Index);

                                foreach (var group in item.Index.Groups)
                                {
                                    foreach (var hash in group.Hashes)
                                    {
                                        _cacheManager.Lock(hash);
                                    }
                                }

                                item.Depth++;

                                item.State = DownloadState.Downloading;
                            }
                        }
                        else
                        {
                            lock (_lockObject)
                            {
                                item.ResultHashes.AddRange(hashes);

                                item.State = DownloadState.Completed;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    item.State = DownloadState.Error;

                    Log.Error(e);
                }
                finally
                {
                    _workingItems.Remove(item);
                }
            }
        }

        #region Volatile

        public Task<Stream> GetStream(Metadata metadata, long maxLength)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            DownloadItemInfo info;

            lock (_lockObject)
            {
                info = _volatileDownloadItemInfoManager.GetInfo(metadata);

                if (info == null)
                {
                    info = new DownloadItemInfo(metadata);
                    info.State = DownloadState.Downloading;

                    _volatileDownloadItemInfoManager.Add(info);
                }

                if (info.State != DownloadState.Completed) return Task.FromResult<Stream>(null);
            }

            return Task.Run(() =>
            {
                Stream stream = null;

                try
                {
                    stream = new BufferStream(_bufferManager);

                    using (var wrapperStream = new WrapperStream(stream, true))
                    using (var tokenSource = new CancellationTokenSource())
                    {
                        var task = _cacheManager.Decoding(wrapperStream, info.ResultHashes, maxLength, tokenSource.Token);

                        while (!task.IsCompleted)
                        {
                            if (this.State == ManagerState.Stop) tokenSource.Cancel();

                            task.Wait(1000);
                        }

                        if (task.Exception != null) throw task.Exception;
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                }
                catch
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                        stream = null;
                    }
                }

                return stream;
            });
        }

        #endregion

        public IEnumerable<Information> GetDownloadInformations()
        {
            lock (_lockObject)
            {
                var list = new List<Information>();

                foreach (var info in _downloadItemInfoManager)
                {
                    var contexts = new List<InformationContext>();
                    {
                        contexts.Add(new InformationContext("Metadata", info.Metadata));
                        contexts.Add(new InformationContext("State", info.State));
                        contexts.Add(new InformationContext("Depth", info.Depth));

                        if (info.State != DownloadState.Error)
                        {
                            if (info.State == DownloadState.Downloading || info.State == DownloadState.Decoding || info.State == DownloadState.ParityDecoding)
                            {
                                if (info.Depth == 1) contexts.Add(new InformationContext("DownloadBlockCount", _cacheManager.Contains(info.Metadata.Hash) ? 1 : 0));
                                else contexts.Add(new InformationContext("DownloadBlockCount", info.Index.Groups.Sum(n => Math.Min(n.Hashes.Count() / 2, _existManager.GetCount(n)))));
                            }
                            else if (info.State == DownloadState.Completed)
                            {
                                if (info.Depth == 1) contexts.Add(new InformationContext("DownloadBlockCount", _cacheManager.Contains(info.Metadata.Hash) ? 1 : 0));
                                else contexts.Add(new InformationContext("DownloadBlockCount", info.Index.Groups.Sum(n => _existManager.GetCount(n))));
                            }

                            if (info.Depth == 1) contexts.Add(new InformationContext("ParityBlockCount", 0));
                            else contexts.Add(new InformationContext("ParityBlockCount", info.Index.Groups.Sum(n => n.Hashes.Count() / 2)));

                            if (info.Depth == 1) contexts.Add(new InformationContext("BlockCount", 1));
                            else contexts.Add(new InformationContext("BlockCount", info.Index.Groups.Sum(n => n.Hashes.Count())));
                        }
                    }

                    list.Add(new Information(contexts));
                }

                return list;
            }
        }

        public Task Export(Metadata metadata, Stream outStream, long maxLength, CancellationToken token)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            if (outStream == null) throw new ArgumentNullException(nameof(outStream));

            lock (_lockObject)
            {
                var info = _downloadItemInfoManager.GetInfo(metadata);
                if (info.State != DownloadState.Completed) throw new DownloadManagerException("Is not completed");

                return _cacheManager.Decoding(outStream, info.ResultHashes, maxLength, token);
            }
        }

        public void Add(Metadata metadata)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            lock (_lockObject)
            {
                if (!_downloadItemInfoManager.Contains(metadata)) return;

                var info = new DownloadItemInfo(metadata);
                info.State = DownloadState.Downloading;

                _downloadItemInfoManager.Add(info);
            }
        }

        public void Remove(Metadata metadata)
        {
            lock (_lockObject)
            {
                var info = _downloadItemInfoManager.GetInfo(metadata);

                _downloadItemInfoManager.Remove(metadata);
            }
        }

        public void Reset(Metadata metadata)
        {
            lock (_lockObject)
            {
                this.Remove(metadata);
                this.Add(metadata);
            }
        }

        public override ManagerState State
        {
            get
            {
                return _state;
            }
        }

        private readonly object _stateLock = new object();

        public override void Start()
        {
            lock (_stateLock)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _downloadThread = new Thread(this.DownloadThread);
                    _downloadThread.Priority = ThreadPriority.BelowNormal;
                    _downloadThread.Name = "DownloadManager_DownloadThread";
                    _downloadThread.Start();

                    for (int i = 0; i < _threadCount; i++)
                    {
                        var thread = new Thread(this.DecodeThread);
                        thread.Priority = ThreadPriority.BelowNormal;
                        thread.Name = "DownloadManager_DecodeThread";
                        thread.Start();

                        _decodeThreads.Add(thread);
                    }
                }
            }
        }

        public override void Stop()
        {
            lock (_stateLock)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                _downloadThread.Join();
                _downloadThread = null;

                foreach (var thread in _decodeThreads)
                {
                    thread.Join();
                }
                _decodeThreads.Clear();
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);

                foreach (var info in _settings.Load<DownloadItemInfo[]>("DownloadItemInfos", () => new DownloadItemInfo[0]))
                {
                    _downloadItemInfoManager.Add(info);
                }
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                _settings.Save("DownloadItemInfos", _downloadItemInfoManager.ToArray());
            }
        }

        #endregion

        private class VolatileDownloadItemInfoManager : IEnumerable<DownloadItemInfo>
        {
            private Dictionary<Metadata, Container<DownloadItemInfo>> _downloadItemInfos;

            public VolatileDownloadItemInfoManager()
            {
                _downloadItemInfos = new Dictionary<Metadata, Container<DownloadItemInfo>>();
            }

            public Action<DownloadItemInfo> AddEvents;
            public Action<DownloadItemInfo> RemoveEvents;

            private void OnAdd(DownloadItemInfo info)
            {
                this.AddEvents?.Invoke(info);
            }

            private void OnRemove(DownloadItemInfo info)
            {
                this.RemoveEvents?.Invoke(info);
            }

            public void Add(DownloadItemInfo info)
            {
                var container = new Container<DownloadItemInfo>();
                container.Value = info;
                container.UpdateTime = DateTime.UtcNow;

                _downloadItemInfos.Add(info.Metadata, container);

                this.OnAdd(container.Value);
            }

            public bool Contains(Metadata metadata)
            {
                return _downloadItemInfos.ContainsKey(metadata);
            }

            public DownloadItemInfo GetInfo(Metadata metadata)
            {
                Container<DownloadItemInfo> container;
                if (!_downloadItemInfos.TryGetValue(metadata, out container)) return null;

                container.UpdateTime = DateTime.UtcNow;

                return container.Value;
            }

            public DownloadItemInfo[] ToArray()
            {
                return _downloadItemInfos.Values.Select(n => n.Value).ToArray();
            }

            public void Update()
            {
                var now = DateTime.UtcNow;

                foreach (var container in _downloadItemInfos.Values.ToArray())
                {
                    if ((now - container.UpdateTime).TotalMinutes < 30) continue;

                    _downloadItemInfos.Remove(container.Value.Metadata);
                    this.OnRemove(container.Value);
                }
            }

            #region IEnumerable<DownloadItemInfo>

            public IEnumerator<DownloadItemInfo> GetEnumerator()
            {
                foreach (var info in _downloadItemInfos.Values.Select(n => n.Value))
                {
                    yield return info;
                }
            }

            #endregion

            #region IEnumerable

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            private class Container<T>
            {
                public T Value { get; set; }
                public DateTime UpdateTime { get; set; }
            }
        }

        private class DownloadItemInfoManager : IEnumerable<DownloadItemInfo>
        {
            private Dictionary<Metadata, DownloadItemInfo> _downloadItemInfos;

            public DownloadItemInfoManager()
            {
                _downloadItemInfos = new Dictionary<Metadata, DownloadItemInfo>();
            }

            public Action<DownloadItemInfo> AddEvents;
            public Action<DownloadItemInfo> RemoveEvents;

            private void OnAdd(DownloadItemInfo info)
            {
                this.AddEvents?.Invoke(info);
            }

            private void OnRemove(DownloadItemInfo info)
            {
                this.RemoveEvents?.Invoke(info);
            }

            public void Add(DownloadItemInfo info)
            {
                _downloadItemInfos.Add(info.Metadata, info);

                this.OnAdd(info);
            }

            public void Remove(Metadata metadata)
            {
                DownloadItemInfo info;
                if (!_downloadItemInfos.TryGetValue(metadata, out info)) return;

                _downloadItemInfos.Remove(metadata);

                this.OnRemove(info);
            }

            public bool Contains(Metadata metadata)
            {
                return _downloadItemInfos.ContainsKey(metadata);
            }

            public DownloadItemInfo GetInfo(Metadata metadata)
            {
                DownloadItemInfo info;
                if (!_downloadItemInfos.TryGetValue(metadata, out info)) return null;

                return info;
            }

            public DownloadItemInfo[] ToArray()
            {
                return _downloadItemInfos.Values.ToArray();
            }

            #region IEnumerable<DownloadItemInfo>

            public IEnumerator<DownloadItemInfo> GetEnumerator()
            {
                foreach (var info in _downloadItemInfos.Values)
                {
                    yield return info;
                }
            }

            #endregion

            #region IEnumerable

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion
        }

        [DataContract(Name = "DownloadItemInfo")]
        private class DownloadItemInfo
        {
            private Metadata _metadata;

            private int _depth;
            private Index _index;

            private DownloadState _state;

            private HashCollection _resultHashes;

            public DownloadItemInfo(Metadata metadata)
            {
                this.Metadata = metadata;
            }

            [DataMember(Name = "Metadata")]
            public Metadata Metadata
            {
                get
                {
                    return _metadata;
                }
                private set
                {
                    _metadata = value;
                }
            }

            [DataMember(Name = "Depth")]
            public int Depth
            {
                get
                {
                    return _depth;
                }
                set
                {
                    _depth = value;
                }
            }

            [DataMember(Name = "Index")]
            public Index Index
            {
                get
                {
                    return _index;
                }
                set
                {
                    _index = value;
                }
            }

            [DataMember(Name = "State")]
            public DownloadState State
            {
                get
                {
                    return _state;
                }
                set
                {
                    _state = value;
                }
            }

            [DataMember(Name = "ResultHashes")]
            public HashCollection ResultHashes
            {
                get
                {
                    if (_resultHashes == null)
                        _resultHashes = new HashCollection();

                    return _resultHashes;
                }
            }
        }

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
    class DownloadManagerException : ManagerException
    {
        public DownloadManagerException() : base() { }
        public DownloadManagerException(string message) : base(message) { }
        public DownloadManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
