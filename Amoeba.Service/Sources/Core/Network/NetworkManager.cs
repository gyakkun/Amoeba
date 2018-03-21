using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Io;
using Omnius.Net;
using Omnius.Security;
using Omnius.Serialization;
using Omnius.Utils;

namespace Amoeba.Service
{
    public delegate IEnumerable<Signature> GetSignaturesEventHandler(object sender);

    public delegate Cap ConnectCapEventHandler(object sender, string uri);
    public delegate Cap AcceptCapEventHandler(object sender, out string uri);

    sealed partial class NetworkManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CacheManager _cacheManager;
        private MetadataManager _metadataManager;

        private Settings _settings;

        private volatile Location _myLocation;
        private LockedHashSet<Location> _cloudLocations = new LockedHashSet<Location>();
        private volatile NetworkConfig _config;
        private LockedHashSet<Hash> _uploadBlockHashes = new LockedHashSet<Hash>();
        private LockedHashSet<Hash> _diffusionBlockHashes = new LockedHashSet<Hash>();

        private volatile byte[] _baseId;
        private LockedHashDictionary<Connection, SessionInfo> _connections = new LockedHashDictionary<Connection, SessionInfo>();

        private List<TaskManager> _connectTaskManagers = new List<TaskManager>();
        private List<TaskManager> _acceptTaskManagers = new List<TaskManager>();
        private TaskManager _computeTaskManager;
        private List<TaskManager> _sendTaskManagers = new List<TaskManager>();
        private List<TaskManager> _receiveTaskManagers = new List<TaskManager>();

        private VolatileHashSet<Hash> _pushBlocksRequestSet = new VolatileHashSet<Hash>(new TimeSpan(0, 10, 0));
        private VolatileHashSet<Signature> _pushBroadcastMetadatasRequestSet = new VolatileHashSet<Signature>(new TimeSpan(0, 10, 0));
        private VolatileHashSet<Signature> _pushUnicastMetadatasRequestSet = new VolatileHashSet<Signature>(new TimeSpan(0, 10, 0));
        private VolatileHashSet<Tag> _pushMulticastMetadatasRequestSet = new VolatileHashSet<Tag>(new TimeSpan(0, 10, 0));

        private ManagerState _state = ManagerState.Stop;

        private ReaderWriterLockManager _connectionLockManager = new ReaderWriterLockManager();

        private readonly object _lockObject = new object();
        private volatile bool _isDisposed;

        private const int _maxLocationCount = 256;
        private const int _maxBlockLinkCount = 256;
        private const int _maxBlockRequestCount = 256;
        private const int _maxMetadataRequestCount = 256;
        private const int _maxMetadataResultCount = 256;

        private readonly int _threadCount = 4;

        public NetworkManager(string configPath, CacheManager cacheManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _cacheManager = cacheManager;
            _metadataManager = new MetadataManager();
            _metadataManager.GetLockSignaturesEvent += (_) => this.OnGetLockSignatures();

            _settings = new Settings(configPath);

            for (int i = 0; i < 3; i++)
            {
                _connectTaskManagers.Add(new TaskManager(this.ConnectThread));
                _acceptTaskManagers.Add(new TaskManager(this.AcceptThread));
            }

            _computeTaskManager = new TaskManager(this.ComputeThread);

            foreach (int i in Enumerable.Range(0, _threadCount))
            {
                _sendTaskManagers.Add(new TaskManager((token) => this.SendThread(i, token)));
                _receiveTaskManagers.Add(new TaskManager((token) => this.ReceiveThread(i, token)));
            }

            this.UpdateBaseId();
        }

        private volatile NetworkStatus _status = new NetworkStatus();

        public class NetworkStatus
        {
            public AtomicInteger ConnectCount { get; } = new AtomicInteger();
            public AtomicInteger AcceptCount { get; } = new AtomicInteger();

            public AtomicInteger ReceivedByteCount { get; } = new AtomicInteger();
            public AtomicInteger SentByteCount { get; } = new AtomicInteger();

            public AtomicInteger PushLocationCount { get; } = new AtomicInteger();
            public AtomicInteger PushBlockLinkCount { get; } = new AtomicInteger();
            public AtomicInteger PushBlockRequestCount { get; } = new AtomicInteger();
            public AtomicInteger PushBlockResultCount { get; } = new AtomicInteger();
            public AtomicInteger PushMessageRequestCount { get; } = new AtomicInteger();
            public AtomicInteger PushMessageResultCount { get; } = new AtomicInteger();

            public AtomicInteger PullLocationCount { get; } = new AtomicInteger();
            public AtomicInteger PullBlockLinkCount { get; } = new AtomicInteger();
            public AtomicInteger PullBlockRequestCount { get; } = new AtomicInteger();
            public AtomicInteger PullBlockResultCount { get; } = new AtomicInteger();
            public AtomicInteger PullMessageRequestCount { get; } = new AtomicInteger();
            public AtomicInteger PullMessageResultCount { get; } = new AtomicInteger();
        }

        public NetworkReport Report
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                return new NetworkReport(
                    _myLocation,
                    _status.ConnectCount,
                    _status.AcceptCount,
                    _connections.Count,
                    _metadataManager.Count,
                    _uploadBlockHashes.Count,
                    _diffusionBlockHashes.Count,
                    _status.ReceivedByteCount,
                    _status.SentByteCount,
                    _status.PushLocationCount,
                    _status.PushBlockLinkCount,
                    _status.PushBlockRequestCount,
                    _status.PushBlockResultCount,
                    _status.PushMessageRequestCount,
                    _status.PushMessageResultCount,
                    _status.PullLocationCount,
                    _status.PullBlockLinkCount,
                    _status.PullBlockRequestCount,
                    _status.PullBlockResultCount,
                    _status.PullMessageRequestCount,
                    _status.PullMessageResultCount);
            }
        }

        public IEnumerable<NetworkConnectionReport> GetNetworkConnectionReports()
        {
            var list = new List<NetworkConnectionReport>();

            foreach (var (connection, sessionInfo) in _connections.ToArray())
            {
                if (sessionInfo.Id == null) continue;

                list.Add(new NetworkConnectionReport(
                    sessionInfo.Id,
                    sessionInfo.Type,
                    sessionInfo.Uri,
                    sessionInfo.Location,
                    sessionInfo.Priority.GetValue(),
                    connection.ReceivedByteCount,
                    connection.SentByteCount));
            }

            return list;
        }

        public NetworkConfig Config
        {
            get
            {
                return _config;
            }
        }

        public void SetConfig(NetworkConfig config)
        {
            _config = config;
        }

        public Location MyLocation
        {
            get
            {
                return _myLocation;
            }
        }

        public void SetMyLocation(Location myLocation)
        {
            _myLocation = myLocation;
        }

        public IEnumerable<Location> CloudLocations
        {
            get
            {
                return _cloudLocations.ToArray();
            }
        }

        public void SetCloudLocations(IEnumerable<Location> locations)
        {
            _cloudLocations.UnionWith(locations);
        }

        public ConnectCapEventHandler ConnectCapEvent { get; set; }
        public AcceptCapEventHandler AcceptCapEvent { get; set; }

        private Cap OnConnectCap(string uri)
        {
            return this.ConnectCapEvent?.Invoke(this, uri);
        }

        private Cap OnAcceptCap(out string uri)
        {
            uri = null;
            return this.AcceptCapEvent?.Invoke(this, out uri);
        }

        public GetSignaturesEventHandler GetLockSignaturesEvent { get; set; }

        private IEnumerable<Signature> OnGetLockSignatures()
        {
            return this.GetLockSignaturesEvent?.Invoke(this) ?? new Signature[0];
        }

        private void UpdateBaseId()
        {
            var baseId = new byte[32];

            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(baseId);
            }

            _baseId = baseId;
        }

        private VolatileHashSet<string> _connectedUris = new VolatileHashSet<string>(new TimeSpan(0, 3, 0));

        private void ConnectThread(CancellationToken token)
        {
            try
            {
                var random = new Random();

                for (; ; )
                {
                    if (token.WaitHandle.WaitOne(1000)) return;

                    // 接続数を制限する。
                    {
                        int connectionCount = _connections.ToArray().Count(n => n.Value.Type == SessionType.Connect);
                        if (connectionCount >= (_config.ConnectionCountLimit / 2)) continue;
                    }

                    string uri = null;

                    lock (_connectedUris.LockObject)
                    {
                        _connectedUris.Update();

                        switch (random.Next(0, 2))
                        {
                            case 0:
                                uri = _cloudLocations.Randomize()
                                    .SelectMany(n => n.Uris)
                                    .Where(n => !_connectedUris.Contains(n))
                                    .FirstOrDefault();
                                break;
                            case 1:
                                var sessionInfo = _connections.Randomize().Select(n => n.Value).FirstOrDefault();
                                if (sessionInfo == null) break;

                                uri = sessionInfo.Receive.PulledLocationSet.Randomize()
                                    .SelectMany(n => n.Uris)
                                    .Where(n => !_connectedUris.Contains(n))
                                    .FirstOrDefault();
                                break;
                        }

                        if (uri == null || _myLocation.Uris.Contains(uri) || _connections.Any(n => n.Value.Location?.Uris?.Contains(uri) ?? false)) continue;

                        _connectedUris.Add(uri);
                    }

                    Cap cap = this.OnConnectCap(uri);

                    if (cap == null)
                    {
                        lock (_cloudLocations.LockObject)
                        {
                            if (_cloudLocations.Count > 1024)
                            {
                                _cloudLocations.ExceptWith(_cloudLocations.Where(n => n.Uris.Contains(uri)).ToArray());
                            }
                        }

                        continue;
                    }

                    _status.ConnectCount.Increment();

                    this.CreateConnection(cap, SessionType.Connect, uri);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void AcceptThread(CancellationToken token)
        {
            try
            {
                for (; ; )
                {
                    if (token.WaitHandle.WaitOne(1000)) return;

                    // 接続数を制限する。
                    {
                        int connectionCount = _connections.ToArray().Count(n => n.Value.Type == SessionType.Accept);
                        if (connectionCount >= (_config.ConnectionCountLimit / 2)) continue;
                    }

                    var cap = this.OnAcceptCap(out string uri);
                    if (cap == null) continue;

                    _status.AcceptCount.Increment();

                    this.CreateConnection(cap, SessionType.Accept, uri);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void CreateConnection(Cap cap, SessionType type, string uri)
        {
            lock (_connections.LockObject)
            {
                if (_connections.Count >= _config.ConnectionCountLimit) return;

                var connection = new Connection(1024 * 1024 * 4, _bufferManager);
                connection.Connect(cap);

                var sessionInfo = new SessionInfo();
                sessionInfo.Type = type;
                sessionInfo.Uri = uri;
                sessionInfo.ThreadId = GetThreadId();

                connection.SendEvent = (_) => this.Send(sessionInfo);
                connection.ReceiveEvent = (_, stream) => this.Receive(sessionInfo, stream);

                _connections.Add(connection, sessionInfo);
            }

            int GetThreadId()
            {
                var dic = new Dictionary<int, int>();

                lock (_connections.LockObject)
                {
                    for (int i = 0; i < _threadCount; i++)
                    {
                        dic.Add(i, _connections.Values.Count(n => n.ThreadId == i));
                    }
                }

                var sortedList = dic.Randomize().ToList();
                sortedList.Sort((x, y) => x.Value.CompareTo(y.Value));

                return sortedList.First().Key;
            }
        }

        private void RemoveConnection(Connection connection)
        {
            lock (_connections.LockObject)
            {
                if (_connections.TryGetValue(connection, out var sessionInfo))
                {
                    _connections.Remove(connection);

                    connection.Dispose();

                    var location = sessionInfo.Location;
                    if (location != null) _cloudLocations.Add(location);
                }
            }
        }

        private void ComputeThread(CancellationToken token)
        {
            var random = new Random();

            var refreshStopwatch = Stopwatch.StartNew();
            var reduceStopwatch = Stopwatch.StartNew();

            var pushBlockUploadStopwatch = Stopwatch.StartNew();
            var pushBlockDownloadStopwatch = Stopwatch.StartNew();

            var pushMetadataUploadStopwatch = Stopwatch.StartNew();
            var pushMetadataDownloadStopwatch = Stopwatch.StartNew();

            try
            {
                for (; ; )
                {
                    if (token.WaitHandle.WaitOne(1000)) return;

                    if (refreshStopwatch.Elapsed.TotalSeconds >= 30)
                    {
                        refreshStopwatch.Restart();

                        // 不要なMetadataを削除する。
                        {
                            _metadataManager.Refresh();
                        }

                        // 古いPushリクエスト情報を削除する。
                        {
                            _pushBlocksRequestSet.Update();
                            _pushBroadcastMetadatasRequestSet.Update();
                            _pushUnicastMetadatasRequestSet.Update();
                            _pushMulticastMetadatasRequestSet.Update();
                        }

                        // 古いセッション情報を破棄する。
                        foreach (var sessionInfo in _connections.Values)
                        {
                            sessionInfo.Update();
                        }

                        // 長い間通信の無い接続を切断する。
                        foreach (var (connection, sessionInfo) in _connections.ToArray())
                        {
                            if (sessionInfo.Receive.Stopwatch.Elapsed.TotalMinutes < 3) continue;

                            this.RemoveConnection(connection);
                        }
                    }

                    if (reduceStopwatch.Elapsed.TotalMinutes >= 3)
                    {
                        reduceStopwatch.Restart();

                        // 優先度の低い通信を切断する。
                        {
                            var now = DateTime.UtcNow;

                            var tempList = _connections.ToArray().Where(n => (now - n.Value.CreationTime).TotalMinutes > 30).ToList();
                            random.Shuffle(tempList);
                            tempList.Sort((x, y) => x.Value.Priority.GetValue().CompareTo(y.Value.Priority.GetValue()));

                            foreach (var (connection, sessionInfo) in tempList.Take(3))
                            {
                                this.RemoveConnection(connection);
                            }
                        }

                        // 拡散アップロードするブロック数が多すぎる場合、maxCount以下まで削除する。
                        {
                            const int maxCount = 1024 * 256;

                            if (_diffusionBlockHashes.Count > maxCount)
                            {
                                var targetHashes = _diffusionBlockHashes.Randomize().Take(_diffusionBlockHashes.Count - maxCount).ToArray();
                                _diffusionBlockHashes.ExceptWith(targetHashes);
                            }
                        }

                        // キャッシュに存在しないブロックのアップロード情報を削除する。
                        {
                            {
                                var targetHashes = _cacheManager.ExceptFrom(_diffusionBlockHashes.ToArray()).ToArray();
                                _diffusionBlockHashes.ExceptWith(targetHashes);
                            }

                            {
                                var targetHashes = _cacheManager.ExceptFrom(_uploadBlockHashes.ToArray()).ToArray();
                                _uploadBlockHashes.ExceptWith(targetHashes);
                            }
                        }
                    }

                    var cloudNodes = new List<Node<SessionInfo>>();
                    {
                        foreach (var sessionInfo in _connections.Values.ToArray())
                        {
                            if (sessionInfo.Id == null) continue;

                            cloudNodes.Add(new Node<SessionInfo>(sessionInfo.Id, sessionInfo));
                        }

                        if (cloudNodes.Count < 3) continue;
                    }

                    // アップロード
                    if (pushBlockUploadStopwatch.Elapsed.TotalSeconds >= 5)
                    {
                        pushBlockUploadStopwatch.Restart();

                        var diffusionMap = new Dictionary<Node<SessionInfo>, List<Hash>>();
                        var uploadMap = new Dictionary<Node<SessionInfo>, List<Hash>>();

                        foreach (var hash in CollectionUtils.Unite(_diffusionBlockHashes.ToArray(), _uploadBlockHashes.ToArray()).Randomize())
                        {
                            foreach (var node in RouteTableMethods.Search(_baseId, hash.Value, cloudNodes, 2))
                            {
                                var tempList = diffusionMap.GetOrAdd(node, (_) => new List<Hash>());
                                if (tempList.Count > 128) continue;

                                tempList.Add(hash);
                            }
                        }

                        foreach (var node in cloudNodes)
                        {
                            uploadMap.GetOrAdd(node, (_) => new List<Hash>())
                                .AddRange(_cacheManager.IntersectFrom(node.Value.Receive.PulledBlockRequestSet.Randomize()).Take(256));
                        }

                        foreach (var node in cloudNodes)
                        {
                            var tempList = new List<Hash>();
                            {
                                if (diffusionMap.TryGetValue(node, out var diffusionList))
                                {
                                    tempList.AddRange(diffusionList);
                                }

                                if (uploadMap.TryGetValue(node, out var uploadList))
                                {
                                    tempList.AddRange(uploadList);
                                }

                                random.Shuffle(tempList);
                            }

                            lock (node.Value.Send.PushBlockResultQueue.LockObject)
                            {
                                node.Value.Send.PushBlockResultQueue.Clear();

                                foreach (var item in tempList)
                                {
                                    node.Value.Send.PushBlockResultQueue.Enqueue(item);
                                }
                            }
                        }
                    }

                    // ダウンロード
                    if (pushBlockDownloadStopwatch.Elapsed.TotalSeconds >= 30)
                    {
                        pushBlockDownloadStopwatch.Restart();

                        var pushBlockLinkSet = new HashSet<Hash>();
                        var pushBlockRequestSet = new HashSet<Hash>();

                        {
                            // Link
                            {
                                {
                                    var tempList = _cacheManager.ToArray();
                                    random.Shuffle(tempList);

                                    pushBlockLinkSet.UnionWith(tempList.Take(_maxBlockLinkCount));
                                }

                                {
                                    var tempSet = new HashSet<Hash>();

                                    foreach (var node in cloudNodes)
                                    {
                                        var tempList = node.Value.Receive.PulledBlockLinkSet.ToArray(new TimeSpan(0, 20, 0));
                                        random.Shuffle(tempList);

                                        tempSet.UnionWith(tempList.Take(_maxBlockLinkCount * node.Value.Priority.GetValue()));
                                    }

                                    pushBlockLinkSet.UnionWith(tempSet.Take(_maxBlockLinkCount * 8));
                                }
                            }

                            // Request
                            {
                                {
                                    var tempSet = new HashSet<Hash>(_cacheManager.ExceptFrom(_pushBlocksRequestSet.ToArray()).ToArray());

                                    foreach (var node in cloudNodes)
                                    {
                                        tempSet.ExceptWith(node.Value.Send.PushedBlockRequestSet);
                                    }

                                    pushBlockRequestSet.UnionWith(tempSet.Randomize().Take(_maxBlockRequestCount));
                                }

                                {
                                    var tempSet = new HashSet<Hash>();

                                    foreach (var node in cloudNodes)
                                    {
                                        var tempList = _cacheManager.ExceptFrom(node.Value.Receive.PulledBlockRequestSet.ToArray(new TimeSpan(0, 20, 0))).ToArray();
                                        random.Shuffle(tempList);

                                        tempSet.UnionWith(tempList.Take(_maxBlockRequestCount));
                                    }

                                    pushBlockRequestSet.UnionWith(tempSet.Take(_maxBlockRequestCount * 8));
                                }
                            }
                        }

                        {
                            // Link
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Hash>>();

                                foreach (var hash in pushBlockLinkSet.Randomize())
                                {
                                    foreach (var node in RouteTableMethods.Search(_baseId, hash.Value, cloudNodes, 16))
                                    {
                                        if (node.Value.Send.PushedBlockLinkFilter.Contains(hash)) continue;

                                        tempMap.GetOrAdd(node, (_) => new List<Hash>()).Add(hash);

                                        break;
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    random.Shuffle(targets);

                                    lock (node.Value.Send.PushBlockLinkQueue.LockObject)
                                    {
                                        node.Value.Send.PushBlockLinkQueue.Clear();

                                        foreach (var hash in targets.Take(_maxBlockLinkCount))
                                        {
                                            node.Value.Send.PushBlockLinkQueue.Enqueue(hash);
                                        }
                                    }
                                }
                            }

                            // Request
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Hash>>();

                                foreach (var hash in pushBlockRequestSet.Randomize())
                                {
                                    foreach (var node in RouteTableMethods.Search(_baseId, hash.Value, cloudNodes, 16))
                                    {
                                        if (node.Value.Send.PushedBlockRequestSet.Contains(hash)) continue;

                                        tempMap.GetOrAdd(node, (_) => new List<Hash>()).Add(hash);

                                        break;
                                    }

                                    foreach (var node in cloudNodes)
                                    {
                                        //if (node.Value.Send.PushBlockRequestFilter.Contains(hash)
                                        //    || !node.Value.Receive.PulledBlockLinkSet.Contains(hash)) continue;
                                        if (!node.Value.Receive.PulledBlockLinkSet.Contains(hash)) continue;

                                        tempMap.GetOrAdd(node, (_) => new List<Hash>()).Add(hash);

                                        break;
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    random.Shuffle(targets);

                                    lock (node.Value.Send.PushBlockRequestQueue.LockObject)
                                    {
                                        node.Value.Send.PushBlockRequestQueue.Clear();

                                        foreach (var hash in targets.Take(_maxBlockRequestCount))
                                        {
                                            node.Value.Send.PushBlockRequestQueue.Enqueue(hash);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // アップロード
                    if (pushMetadataUploadStopwatch.Elapsed.TotalSeconds >= 30)
                    {
                        pushMetadataUploadStopwatch.Restart();

                        // BroadcastMetadata
                        foreach (var signature in _metadataManager.GetBroadcastSignatures())
                        {
                            foreach (var node in RouteTableMethods.Search(_baseId, signature.Id, cloudNodes, 1))
                            {
                                node.Value.Receive.PulledBroadcastMetadataRequestSet.Add(signature);
                            }
                        }

                        // UnicastMetadata
                        foreach (var signature in _metadataManager.GetUnicastSignatures())
                        {
                            foreach (var node in RouteTableMethods.Search(_baseId, signature.Id, cloudNodes, 1))
                            {
                                node.Value.Receive.PulledUnicastMetadataRequestSet.Add(signature);
                            }
                        }

                        // MulticastMetadata
                        foreach (var tag in _metadataManager.GetMulticastTags())
                        {
                            foreach (var node in RouteTableMethods.Search(_baseId, tag.Id, cloudNodes, 1))
                            {
                                node.Value.Receive.PulledMulticastMetadataRequestSet.Add(tag);
                            }
                        }
                    }

                    // ダウンロード
                    if (pushMetadataDownloadStopwatch.Elapsed.TotalSeconds >= 30)
                    {
                        pushMetadataDownloadStopwatch.Restart();

                        var pushBroadcastMetadatasRequestSet = new HashSet<Signature>();
                        var pushUnicastMetadatasRequestSet = new HashSet<Signature>();
                        var pushMulticastMetadatasRequestSet = new HashSet<Tag>();

                        {
                            // BroadcastMetadata
                            {
                                {
                                    var list = _pushBroadcastMetadatasRequestSet.ToArray();
                                    random.Shuffle(list);

                                    pushBroadcastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }

                                foreach (var node in cloudNodes)
                                {
                                    var list = node.Value.Receive.PulledBroadcastMetadataRequestSet.ToArray();
                                    random.Shuffle(list);

                                    pushBroadcastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }
                            }

                            // UnicastMetadata
                            {
                                {
                                    var list = _pushUnicastMetadatasRequestSet.ToArray();
                                    random.Shuffle(list);

                                    pushUnicastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }

                                foreach (var node in cloudNodes)
                                {
                                    var list = node.Value.Receive.PulledUnicastMetadataRequestSet.ToArray();
                                    random.Shuffle(list);

                                    pushUnicastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }
                            }

                            // MulticastMetadata
                            {
                                {
                                    var list = _pushMulticastMetadatasRequestSet.ToArray();
                                    random.Shuffle(list);

                                    pushMulticastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }

                                foreach (var node in cloudNodes)
                                {
                                    var list = node.Value.Receive.PulledMulticastMetadataRequestSet.ToArray();
                                    random.Shuffle(list);

                                    pushMulticastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }
                            }
                        }

                        {
                            // BroadcastMetadata
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Signature>>();

                                foreach (var signature in pushBroadcastMetadatasRequestSet.Randomize())
                                {
                                    foreach (var node in RouteTableMethods.Search(_baseId, signature.Id, cloudNodes, 3))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Signature>()).Add(signature);
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    random.Shuffle(targets);

                                    lock (node.Value.Send.PushBroadcastMetadataRequestQueue.LockObject)
                                    {
                                        node.Value.Send.PushBroadcastMetadataRequestQueue.Clear();

                                        foreach (var signature in targets.Take(_maxMetadataRequestCount))
                                        {
                                            node.Value.Send.PushBroadcastMetadataRequestQueue.Enqueue(signature);
                                        }
                                    }
                                }
                            }

                            // UnicastMetadata
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Signature>>();

                                foreach (var signature in pushUnicastMetadatasRequestSet.Randomize())
                                {
                                    foreach (var node in RouteTableMethods.Search(_baseId, signature.Id, cloudNodes, 3))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Signature>()).Add(signature);
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    random.Shuffle(targets);

                                    lock (node.Value.Send.PushUnicastMetadataRequestQueue.LockObject)
                                    {
                                        node.Value.Send.PushUnicastMetadataRequestQueue.Clear();

                                        foreach (var signature in targets.Take(_maxMetadataRequestCount))
                                        {
                                            node.Value.Send.PushUnicastMetadataRequestQueue.Enqueue(signature);
                                        }
                                    }
                                }
                            }

                            // MulticastMetadata
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Tag>>();

                                foreach (var tag in pushMulticastMetadatasRequestSet.Randomize())
                                {
                                    foreach (var node in RouteTableMethods.Search(_baseId, tag.Id, cloudNodes, 3))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Tag>()).Add(tag);
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    random.Shuffle(targets);

                                    lock (node.Value.Send.PushMulticastMetadataRequestQueue.LockObject)
                                    {
                                        node.Value.Send.PushMulticastMetadataRequestQueue.Clear();

                                        foreach (var tag in targets.Take(_maxMetadataRequestCount))
                                        {
                                            node.Value.Send.PushMulticastMetadataRequestQueue.Enqueue(tag);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void SendThread(int threadId, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                for (; ; )
                {
                    if (sw.ElapsedMilliseconds > 1000) Debug.WriteLine("SendConnectionsThread: " + sw.ElapsedMilliseconds);

                    // Timer
                    {
                        if (token.WaitHandle.WaitOne((int)(Math.Max(0, 1000 - sw.ElapsedMilliseconds)))) return;
                        sw.Restart();
                    }

                    // Send
                    {
                        int remain = (_config.BandwidthLimit != 0) ? _config.BandwidthLimit / _threadCount : int.MaxValue;

                        foreach (var (connection, sessionInfo) in _connections.Where(n => n.Value.ThreadId == threadId).Randomize())
                        {
                            try
                            {
                                using (_connectionLockManager.ReadLock())
                                {
                                    int count = connection.Send(Math.Min(remain, 1024 * 1024 * 4));
                                    _status.SentByteCount.Add(count);

                                    remain -= count;
                                    if (remain <= 0) break;
                                }
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e);

                                using (_connectionLockManager.WriteLock())
                                {
                                    this.RemoveConnection(connection);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void ReceiveThread(int threadId, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                for (; ; )
                {
                    if (sw.ElapsedMilliseconds > 1000) Debug.WriteLine("ReceiveConnectionsThread: " + sw.ElapsedMilliseconds);

                    // Timer
                    {
                        if (token.WaitHandle.WaitOne((int)(Math.Max(0, 1000 - sw.ElapsedMilliseconds)))) return;
                        sw.Restart();
                    }

                    if (this.State == ManagerState.Stop) return;

                    // Receive
                    {
                        int remain = (_config.BandwidthLimit != 0) ? _config.BandwidthLimit / _threadCount : int.MaxValue;

                        foreach (var (connection, sessionInfo) in _connections.Where(n => n.Value.ThreadId == threadId).Randomize())
                        {
                            try
                            {
                                using (_connectionLockManager.ReadLock())
                                {
                                    int count = connection.Receive(Math.Min(remain, 1024 * 1024 * 4));
                                    _status.ReceivedByteCount.Add(count);

                                    remain -= count;
                                    if (remain <= 0) break;
                                }
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e);

                                using (_connectionLockManager.WriteLock())
                                {
                                    this.RemoveConnection(connection);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private enum SerializeId
        {
            Locations = 0,

            BlocksLink = 1,
            BlocksRequest = 2,
            BlockResult = 3,

            BroadcastMetadatasRequest = 4,
            BroadcastMetadatasResult = 5,

            UnicastMetadatasRequest = 6,
            UnicastMetadatasResult = 7,

            MulticastMetadatasRequest = 8,
            MulticastMetadatasResult = 9,
        }

        private Stream Send(SessionInfo sessionInfo)
        {
            if (!sessionInfo.Send.IsInitialized)
            {
                sessionInfo.Send.IsInitialized = true;

                Stream versionStream = new RecyclableMemoryStream(_bufferManager);
                Varint.SetUInt64(versionStream, (uint)ProtocolVersion.Version1);

                var packet = new ProfilePacket(_baseId, _myLocation);

                var dataStream = packet.Export(_bufferManager);

                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send Profile");

                return new UniteStream(versionStream, dataStream);
            }
            else
            {
                if (sessionInfo.Send.LocationResultStopwatch.Elapsed.TotalMinutes > 3)
                {
                    sessionInfo.Send.LocationResultStopwatch.Restart();

                    var random = RandomProvider.GetThreadRandom();

                    var tempLocations = new List<Location>();
                    tempLocations.Add(_myLocation);
                    tempLocations.AddRange(_cloudLocations);

                    random.Shuffle(tempLocations);

                    var packet = new LocationsPacket(tempLocations.Take(_maxLocationCount).ToArray());

                    Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                    Varint.SetUInt64(typeStream, (uint)SerializeId.Locations);

                    _status.PushLocationCount.Add(packet.Locations.Count());

                    Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send LocationResult");

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }
                else if (sessionInfo.Send.PushBlockLinkQueue.Count > 0)
                {
                    BlocksLinkPacket packet;

                    lock (sessionInfo.Send.PushBlockLinkQueue.LockObject)
                    {
                        sessionInfo.Send.PushedBlockLinkFilter.AddRange(sessionInfo.Send.PushBlockLinkQueue);

                        packet = new BlocksLinkPacket(sessionInfo.Send.PushBlockLinkQueue.ToArray());
                        sessionInfo.Send.PushBlockLinkQueue.Clear();
                    }

                    Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                    Varint.SetUInt64(typeStream, (uint)SerializeId.BlocksLink);

                    _status.PushBlockLinkCount.Add(packet.Hashes.Count());

                    Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send BlockLink");

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }
                else if (sessionInfo.Send.PushBlockRequestQueue.Count > 0)
                {
                    BlocksRequestPacket packet;

                    lock (sessionInfo.Send.PushBlockRequestQueue.LockObject)
                    {
                        sessionInfo.Send.PushedBlockRequestSet.UnionWith(sessionInfo.Send.PushBlockRequestQueue);

                        packet = new BlocksRequestPacket(sessionInfo.Send.PushBlockRequestQueue.ToArray());
                        sessionInfo.Send.PushBlockRequestQueue.Clear();
                    }

                    Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                    Varint.SetUInt64(typeStream, (uint)SerializeId.BlocksRequest);

                    _status.PushBlockRequestCount.Add(packet.Hashes.Count());

                    Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send BlockRequest");

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }
                else if (sessionInfo.Send.BlockResultStopwatch.Elapsed.TotalSeconds > 0.5 && sessionInfo.Send.PushBlockResultQueue.Count > 0)
                {
                    sessionInfo.Send.BlockResultStopwatch.Restart();

                    Hash? hash = null;

                    lock (sessionInfo.Send.PushBlockResultQueue.LockObject)
                    {
                        if (sessionInfo.Send.PushBlockResultQueue.Count > 0)
                        {
                            hash = sessionInfo.Send.PushBlockResultQueue.Dequeue();
                            sessionInfo.Receive.PulledBlockRequestSet.Remove(hash.Value);
                        }
                    }

                    if (hash != null)
                    {
                        Stream dataStream = null;
                        {
                            var buffer = new ArraySegment<byte>();

                            try
                            {
                                buffer = _cacheManager.GetBlock(hash.Value);

                                dataStream = (new BlockResultPacket(hash.Value, buffer)).Export(_bufferManager);
                            }
                            catch (Exception)
                            {

                            }
                            finally
                            {
                                if (buffer.Array != null)
                                {
                                    _bufferManager.ReturnBuffer(buffer.Array);
                                }
                            }
                        }

                        if (dataStream != null)
                        {
                            Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                            Varint.SetUInt64(typeStream, (uint)SerializeId.BlockResult);

                            _status.PushBlockResultCount.Increment();

                            _diffusionBlockHashes.Remove(hash.Value);
                            _uploadBlockHashes.Remove(hash.Value);

                            Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send BlockResult " + NetworkConverter.ToBase64UrlString(hash.Value.Value));

                            return new UniteStream(typeStream, dataStream);
                        }
                    }
                }
                else if (sessionInfo.Send.PushBroadcastMetadataRequestQueue.Count > 0)
                {
                    BroadcastMetadatasRequestPacket packet;

                    lock (sessionInfo.Send.PushBroadcastMetadataRequestQueue.LockObject)
                    {
                        packet = new BroadcastMetadatasRequestPacket(sessionInfo.Send.PushBroadcastMetadataRequestQueue.ToArray());
                        sessionInfo.Send.PushBroadcastMetadataRequestQueue.Clear();
                    }

                    Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                    Varint.SetUInt64(typeStream, (uint)SerializeId.BroadcastMetadatasRequest);

                    _status.PushMessageRequestCount.Add(packet.Signatures.Count());

                    Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send BroadcastMetadataRequest");

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }
                else if (sessionInfo.Send.BroadcastMetadataResultStopwatch.Elapsed.TotalSeconds > 30)
                {
                    sessionInfo.Send.BroadcastMetadataResultStopwatch.Restart();

                    var random = RandomProvider.GetThreadRandom();

                    var broadcastMetadatas = new List<BroadcastMetadata>();

                    var signatures = new List<Signature>();

                    lock (sessionInfo.Receive.PulledBroadcastMetadataRequestSet.LockObject)
                    {
                        signatures.AddRange(sessionInfo.Receive.PulledBroadcastMetadataRequestSet);
                    }

                    random.Shuffle(signatures);

                    foreach (var signature in signatures)
                    {
                        foreach (var metadata in _metadataManager.GetBroadcastMetadatas(signature).Randomize())
                        {
                            broadcastMetadatas.Add(metadata);

                            if (broadcastMetadatas.Count >= _maxMetadataResultCount) goto End;
                        }
                    }

                    End:;

                    if (broadcastMetadatas.Count > 0)
                    {
                        var packet = new BroadcastMetadatasResultPacket(broadcastMetadatas);
                        broadcastMetadatas.Clear();

                        Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                        Varint.SetUInt64(typeStream, (uint)SerializeId.BroadcastMetadatasResult);

                        _status.PushMessageResultCount.Add(packet.BroadcastMetadatas.Count());

                        Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send MetadataResult");

                        return new UniteStream(typeStream, packet.Export(_bufferManager));
                    }
                }
                else if (sessionInfo.Send.PushUnicastMetadataRequestQueue.Count > 0)
                {
                    UnicastMetadatasRequestPacket packet;

                    lock (sessionInfo.Send.PushUnicastMetadataRequestQueue.LockObject)
                    {
                        packet = new UnicastMetadatasRequestPacket(sessionInfo.Send.PushUnicastMetadataRequestQueue.ToArray());
                        sessionInfo.Send.PushUnicastMetadataRequestQueue.Clear();
                    }

                    Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                    Varint.SetUInt64(typeStream, (uint)SerializeId.UnicastMetadatasRequest);

                    _status.PushMessageRequestCount.Add(packet.Signatures.Count());

                    Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send UnicastMetadataRequest");

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }
                else if (sessionInfo.Send.UnicastMetadataResultStopwatch.Elapsed.TotalSeconds > 30)
                {
                    sessionInfo.Send.UnicastMetadataResultStopwatch.Restart();

                    var random = RandomProvider.GetThreadRandom();

                    var UnicastMetadatas = new List<UnicastMetadata>();

                    var signatures = new List<Signature>();

                    lock (sessionInfo.Receive.PulledUnicastMetadataRequestSet.LockObject)
                    {
                        signatures.AddRange(sessionInfo.Receive.PulledUnicastMetadataRequestSet);
                    }

                    random.Shuffle(signatures);

                    foreach (var signature in signatures)
                    {
                        foreach (var metadata in _metadataManager.GetUnicastMetadatas(signature).Randomize())
                        {
                            UnicastMetadatas.Add(metadata);

                            if (UnicastMetadatas.Count >= _maxMetadataResultCount) goto End;
                        }
                    }

                    End:;

                    if (UnicastMetadatas.Count > 0)
                    {
                        var packet = new UnicastMetadatasResultPacket(UnicastMetadatas);
                        UnicastMetadatas.Clear();

                        Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                        Varint.SetUInt64(typeStream, (uint)SerializeId.UnicastMetadatasResult);

                        _status.PushMessageResultCount.Add(packet.UnicastMetadatas.Count());

                        Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send MetadataResult");

                        return new UniteStream(typeStream, packet.Export(_bufferManager));
                    }
                }
                else if (sessionInfo.Send.PushMulticastMetadataRequestQueue.Count > 0)
                {
                    MulticastMetadatasRequestPacket packet;

                    lock (sessionInfo.Send.PushMulticastMetadataRequestQueue.LockObject)
                    {
                        packet = new MulticastMetadatasRequestPacket(sessionInfo.Send.PushMulticastMetadataRequestQueue.ToArray());
                        sessionInfo.Send.PushMulticastMetadataRequestQueue.Clear();
                    }

                    Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                    Varint.SetUInt64(typeStream, (uint)SerializeId.MulticastMetadatasRequest);

                    _status.PushMessageRequestCount.Add(packet.Tags.Count());

                    Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send MulticastMetadataRequest");

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }
                else if (sessionInfo.Send.MulticastMetadataResultStopwatch.Elapsed.TotalSeconds > 30)
                {
                    sessionInfo.Send.MulticastMetadataResultStopwatch.Restart();

                    var random = RandomProvider.GetThreadRandom();

                    var MulticastMetadatas = new List<MulticastMetadata>();

                    var tags = new List<Tag>();

                    lock (sessionInfo.Receive.PulledMulticastMetadataRequestSet.LockObject)
                    {
                        tags.AddRange(sessionInfo.Receive.PulledMulticastMetadataRequestSet);
                    }

                    random.Shuffle(tags);

                    foreach (var tag in tags)
                    {
                        foreach (var metadata in _metadataManager.GetMulticastMetadatas(tag).Randomize())
                        {
                            MulticastMetadatas.Add(metadata);

                            if (MulticastMetadatas.Count >= _maxMetadataResultCount) goto End;
                        }
                    }

                    End:;

                    if (MulticastMetadatas.Count > 0)
                    {
                        var packet = new MulticastMetadatasResultPacket(MulticastMetadatas);
                        MulticastMetadatas.Clear();

                        Stream typeStream = new RecyclableMemoryStream(_bufferManager);
                        Varint.SetUInt64(typeStream, (uint)SerializeId.MulticastMetadatasResult);

                        _status.PushMessageResultCount.Add(packet.MulticastMetadatas.Count());

                        Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Send MetadataResult");

                        return new UniteStream(typeStream, packet.Export(_bufferManager));
                    }
                }
            }

            return null;
        }

        private void Receive(SessionInfo sessionInfo, Stream stream)
        {
            try
            {
                sessionInfo.Receive.Stopwatch.Restart();

                if (!sessionInfo.Receive.IsInitialized)
                {
                    var targetVersion = (ProtocolVersion)Varint.GetUInt64(stream);

                    sessionInfo.Version = (ProtocolVersion)Math.Min((uint)targetVersion, (uint)ProtocolVersion.Version1);

                    using (var dataStream = new RangeStream(stream))
                    {
                        var profile = ProfilePacket.Import(dataStream, _bufferManager);
                        if (profile.Id == null || profile.Location == null) throw new ArgumentException("NetworkManager: Broken Profile");

                        if (Unsafe.Equals(_baseId, profile.Id)) throw new ArgumentException("NetworkManager: Circular Connect");

                        lock (_connections.LockObject)
                        {
                            var connectionIds = _connections.Select(n => n.Value.Id).Where(n => n != null).ToArray();
                            if (connectionIds.Any(n => Unsafe.Equals(n, profile.Id))) throw new ArgumentException("NetworkManager: Conflict");

                            var distance = RouteTableMethods.Distance(_baseId, profile.Id);
                            var count = connectionIds.Select(id => RouteTableMethods.Distance(_baseId, id)).Count(n => n == distance);

                            if (count > 32) throw new ArgumentException("NetworkManager: RouteTable Overflow");
                        }

                        sessionInfo.Id = profile.Id;
                        sessionInfo.Location = profile.Location;

                        sessionInfo.Receive.IsInitialized = true;
                    }

                    Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive Profile");
                }
                else
                {
                    try
                    {
                        int id = (int)Varint.GetUInt64(stream);

                        using (var dataStream = new RangeStream(stream))
                        {
                            if (id == (int)SerializeId.Locations)
                            {
                                var packet = LocationsPacket.Import(dataStream, _bufferManager);

                                if (sessionInfo.Receive.PulledLocationSet.Count + packet.Locations.Count()
                                    > _maxLocationCount * sessionInfo.Receive.PulledLocationSet.SurvivalTime.TotalMinutes / 3) return;

                                sessionInfo.Receive.PulledLocationSet.UnionWith(packet.Locations);

                                _status.PullLocationCount.Add(packet.Locations.Count());

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive LocationResult");
                            }
                            else if (id == (int)SerializeId.BlocksLink)
                            {
                                var packet = BlocksLinkPacket.Import(dataStream, _bufferManager);

                                if (sessionInfo.Receive.PulledBlockLinkSet.Count + packet.Hashes.Count()
                                    > _maxBlockLinkCount * sessionInfo.Receive.PulledBlockLinkSet.SurvivalTime.TotalMinutes * 2) return;

                                sessionInfo.Receive.PulledBlockLinkSet.UnionWith(packet.Hashes);

                                _status.PullBlockLinkCount.Add(packet.Hashes.Count());

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive BlocksLink");
                            }
                            else if (id == (int)SerializeId.BlocksRequest)
                            {
                                var packet = BlocksRequestPacket.Import(dataStream, _bufferManager);

                                if (sessionInfo.Receive.PulledBlockRequestSet.Count + packet.Hashes.Count()
                                    > _maxBlockRequestCount * sessionInfo.Receive.PulledBlockRequestSet.SurvivalTime.TotalMinutes * 2) return;

                                sessionInfo.Receive.PulledBlockRequestSet.UnionWith(packet.Hashes);

                                _status.PullBlockRequestCount.Add(packet.Hashes.Count());

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive BlocksRequest");
                            }
                            else if (id == (int)SerializeId.BlockResult)
                            {
                                var packet = BlockResultPacket.Import(dataStream, _bufferManager);

                                _status.PullBlockResultCount.Increment();

                                try
                                {
                                    _cacheManager.Set(packet.Hash, packet.Value);

                                    if (sessionInfo.Send.PushedBlockRequestSet.Contains(packet.Hash))
                                    {
                                        var priority = (int)(sessionInfo.Send.PushedBlockRequestSet.SurvivalTime.TotalMinutes - sessionInfo.Send.PushedBlockRequestSet.GetElapsedTime(packet.Hash).TotalMinutes);
                                        sessionInfo.Priority.Add(priority);
                                    }
                                    else
                                    {
                                        _diffusionBlockHashes.Add(packet.Hash);
                                    }
                                }
                                finally
                                {
                                    if (packet.Value.Array != null)
                                    {
                                        _bufferManager.ReturnBuffer(packet.Value.Array);
                                    }
                                }

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive BlockResult " + NetworkConverter.ToBase64UrlString(packet.Hash.Value));
                            }
                            else if (id == (int)SerializeId.BroadcastMetadatasRequest)
                            {
                                var packet = BroadcastMetadatasRequestPacket.Import(dataStream, _bufferManager);

                                if (sessionInfo.Receive.PulledBroadcastMetadataRequestSet.Count + packet.Signatures.Count()
                                    > _maxMetadataRequestCount * sessionInfo.Receive.PulledBroadcastMetadataRequestSet.SurvivalTime.TotalMinutes * 2) return;

                                sessionInfo.Receive.PulledBroadcastMetadataRequestSet.UnionWith(packet.Signatures);

                                _status.PullMessageRequestCount.Add(packet.Signatures.Count());

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive BroadcastMetadatasRequest");
                            }
                            else if (id == (int)SerializeId.BroadcastMetadatasResult)
                            {
                                var packet = BroadcastMetadatasResultPacket.Import(dataStream, _bufferManager);

                                if (packet.BroadcastMetadatas.Count() > _maxMetadataResultCount) return;

                                _status.PullMessageResultCount.Add(packet.BroadcastMetadatas.Count());

                                foreach (var metadata in packet.BroadcastMetadatas)
                                {
                                    _metadataManager.SetMetadata(metadata);
                                }

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive BroadcastMetadatasResult");
                            }
                            else if (id == (int)SerializeId.UnicastMetadatasRequest)
                            {
                                var packet = UnicastMetadatasRequestPacket.Import(dataStream, _bufferManager);

                                if (sessionInfo.Receive.PulledUnicastMetadataRequestSet.Count + packet.Signatures.Count()
                                    > _maxMetadataRequestCount * sessionInfo.Receive.PulledUnicastMetadataRequestSet.SurvivalTime.TotalMinutes * 2) return;

                                sessionInfo.Receive.PulledUnicastMetadataRequestSet.UnionWith(packet.Signatures);

                                _status.PullMessageRequestCount.Add(packet.Signatures.Count());

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive UnicastMetadatasRequest");
                            }
                            else if (id == (int)SerializeId.UnicastMetadatasResult)
                            {
                                var packet = UnicastMetadatasResultPacket.Import(dataStream, _bufferManager);

                                if (packet.UnicastMetadatas.Count() > _maxMetadataResultCount) return;

                                _status.PullMessageResultCount.Add(packet.UnicastMetadatas.Count());

                                foreach (var metadata in packet.UnicastMetadatas)
                                {
                                    _metadataManager.SetMetadata(metadata);
                                }

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive UnicastMetadatasResult");
                            }
                            else if (id == (int)SerializeId.MulticastMetadatasRequest)
                            {
                                var packet = MulticastMetadatasRequestPacket.Import(dataStream, _bufferManager);

                                if (sessionInfo.Receive.PulledMulticastMetadataRequestSet.Count + packet.Tags.Count()
                                    > _maxMetadataRequestCount * sessionInfo.Receive.PulledMulticastMetadataRequestSet.SurvivalTime.TotalMinutes * 2) return;

                                sessionInfo.Receive.PulledMulticastMetadataRequestSet.UnionWith(packet.Tags);

                                _status.PullMessageRequestCount.Add(packet.Tags.Count());

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive MulticastMetadatasRequest");
                            }
                            else if (id == (int)SerializeId.MulticastMetadatasResult)
                            {
                                var packet = MulticastMetadatasResultPacket.Import(dataStream, _bufferManager);

                                if (packet.MulticastMetadatas.Count() > _maxMetadataResultCount) return;

                                _status.PullMessageResultCount.Add(packet.MulticastMetadatas.Count());

                                foreach (var metadata in packet.MulticastMetadatas)
                                {
                                    _metadataManager.SetMetadata(metadata);
                                }

                                Debug.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " NetworkManager: Receive MulticastMetadatasResult");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Debug(e);
                    }
                }
            }
            finally
            {
                stream.Dispose();
                stream = null;
            }
        }

        public void Download(Hash hash)
        {
            _pushBlocksRequestSet.Add(hash);
        }

        public void Upload(BroadcastMetadata metadata)
        {
            _metadataManager.SetMetadata(metadata);
        }

        public void Upload(UnicastMetadata metadata)
        {
            _metadataManager.SetMetadata(metadata);
        }

        public void Upload(MulticastMetadata metadata)
        {
            _metadataManager.SetMetadata(metadata);
        }

        public BroadcastMetadata GetBroadcastMetadata(Signature signature, string type)
        {
            _pushBroadcastMetadatasRequestSet.Add(signature);

            return _metadataManager.GetBroadcastMetadata(signature, type);
        }

        public IEnumerable<UnicastMetadata> GetUnicastMetadatas(Signature signature, string type)
        {
            _pushUnicastMetadatasRequestSet.Add(signature);

            return _metadataManager.GetUnicastMetadatas(signature, type);
        }

        public IEnumerable<MulticastMetadata> GetMulticastMetadatas(Tag tag, string type)
        {
            _pushMulticastMetadatasRequestSet.Add(tag);

            return _metadataManager.GetMulticastMetadatas(tag, type);
        }

        public void Diffuse(string path)
        {
            var hashes = _cacheManager.GetContentHashes(path);
            _uploadBlockHashes.UnionWith(hashes);
        }

        public override ManagerState State
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                return _state;
            }
        }

        private readonly object _stateLockObject = new object();

        public override void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_stateLockObject)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    foreach (var taskManager in _connectTaskManagers)
                    {
                        taskManager.Start();
                    }

                    foreach (var taskManager in _acceptTaskManagers)
                    {
                        taskManager.Start();
                    }

                    _computeTaskManager.Start();

                    foreach (var taskManager in _sendTaskManagers)
                    {
                        taskManager.Start();
                    }

                    foreach (var taskManager in _receiveTaskManagers)
                    {
                        taskManager.Start();
                    }
                }
            }
        }

        public override void Stop()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_stateLockObject)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                foreach (var taskManager in _connectTaskManagers)
                {
                    taskManager.Stop();
                }

                foreach (var taskManager in _acceptTaskManagers)
                {
                    taskManager.Stop();
                }

                _computeTaskManager.Stop();

                foreach (var taskManager in _sendTaskManagers)
                {
                    taskManager.Stop();
                }

                foreach (var taskManager in _receiveTaskManagers)
                {
                    taskManager.Stop();
                }
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);

                {
                    int connectionCountLimit = _settings.Load<int>("ConnectionCountLimit", () => 128);
                    int bandwidthLimit = _settings.Load<int>("BandwidthLimit", () => 1024 * 1024 * 32);

                    this.SetConfig(new NetworkConfig(connectionCountLimit, bandwidthLimit));
                }
                this.SetMyLocation(_settings.Load<Location>("MyLocation", () => new Location(Array.Empty<string>())));
                this.SetCloudLocations(_settings.Load<IEnumerable<Location>>("CloudLocations", () => Array.Empty<Location>()));

                // MetadataManager
                {
                    foreach (var metadata in _settings.Load("BroadcastMetadatas", () => Array.Empty<BroadcastMetadata>()))
                    {
                        _metadataManager.SetMetadata(metadata);
                    }

                    foreach (var metadata in _settings.Load("UnicastMetadatas", () => Array.Empty<UnicastMetadata>()))
                    {
                        _metadataManager.SetMetadata(metadata);
                    }

                    foreach (var metadata in _settings.Load("MulticastMetadatas", () => Array.Empty<MulticastMetadata>()))
                    {
                        _metadataManager.SetMetadata(metadata);
                    }
                }

                _uploadBlockHashes.UnionWith(_settings.Load<IEnumerable<Hash>>("UploadBlockHashes", () => Array.Empty<Hash>()));
                _diffusionBlockHashes.UnionWith(_settings.Load<IEnumerable<Hash>>("DiffusionBlockHashes", () => Array.Empty<Hash>()));
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                {
                    var config = this.Config;

                    _settings.Save("ConnectionCountLimit", config.ConnectionCountLimit);
                    _settings.Save("BandwidthLimit", config.BandwidthLimit);
                }

                _settings.Save("MyLocation", _myLocation);
                _settings.Save("CloudLocations", _cloudLocations);

                // MetadataManager
                {
                    _settings.Save("BroadcastMetadatas", _metadataManager.GetBroadcastMetadatas());
                    _settings.Save("UnicastMetadatas", _metadataManager.GetUnicastMetadatas());
                    _settings.Save("MulticastMetadatas", _metadataManager.GetMulticastMetadatas());
                }

                _settings.Save("UploadBlockHashes", _uploadBlockHashes);
                _settings.Save("DiffusionBlockHashes", _diffusionBlockHashes);
            }
        }

        #endregion

        private enum ProtocolVersion : uint
        {
            Version0 = 0,
            Version1 = 1,
        }

        sealed class SessionInfo
        {
            public SessionType Type { get; set; }
            public string Uri { get; set; }
            public int ThreadId { get; set; }

            public ProtocolVersion Version { get; set; }
            public byte[] Id { get; set; }
            public Location Location { get; set; }

            public PriorityManager Priority { get; private set; } = new PriorityManager(new TimeSpan(0, 10, 0));

            public SendInfo Send { get; private set; } = new SendInfo();
            public ReceiveInfo Receive { get; private set; } = new ReceiveInfo();

            public DateTime CreationTime { get; private set; } = DateTime.UtcNow;

            public void Update()
            {
                this.Priority.Update();
                this.Send.Update();
                this.Receive.Update();
            }

            public sealed class SendInfo
            {
                public bool IsInitialized { get; set; }

                public VolatileHashSet<Hash> PushedBlockRequestSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 30, 0));
                public VolatileBloomFilter<Hash> PushedBlockLinkFilter { get; private set; } = new VolatileBloomFilter<Hash>(_maxBlockLinkCount * 2, 0.001, (n) => n.GetHashCode(), new TimeSpan(0, 1, 0), new TimeSpan(3, 0, 0));

                public Stopwatch LocationResultStopwatch { get; private set; } = Stopwatch.StartNew();
                public Stopwatch BlockResultStopwatch { get; private set; } = Stopwatch.StartNew();
                public Stopwatch BroadcastMetadataResultStopwatch { get; private set; } = Stopwatch.StartNew();
                public Stopwatch UnicastMetadataResultStopwatch { get; private set; } = Stopwatch.StartNew();
                public Stopwatch MulticastMetadataResultStopwatch { get; private set; } = Stopwatch.StartNew();

                public LockedQueue<Hash> PushBlockResultQueue { get; private set; } = new LockedQueue<Hash>();
                public LockedQueue<Hash> PushBlockLinkQueue { get; private set; } = new LockedQueue<Hash>();
                public LockedQueue<Hash> PushBlockRequestQueue { get; private set; } = new LockedQueue<Hash>();
                public LockedQueue<Signature> PushBroadcastMetadataRequestQueue { get; private set; } = new LockedQueue<Signature>();
                public LockedQueue<Signature> PushUnicastMetadataRequestQueue { get; private set; } = new LockedQueue<Signature>();
                public LockedQueue<Tag> PushMulticastMetadataRequestQueue { get; private set; } = new LockedQueue<Tag>();

                public void Update()
                {
                    this.PushedBlockRequestSet.Update();

                    this.PushedBlockLinkFilter.Update();
                }
            }

            public sealed class ReceiveInfo
            {
                public bool IsInitialized { get; set; }

                public Stopwatch Stopwatch { get; private set; } = new Stopwatch();

                public VolatileHashSet<Location> PulledLocationSet { get; private set; } = new VolatileHashSet<Location>(new TimeSpan(0, 10, 0));
                public VolatileHashSet<Hash> PulledBlockLinkSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 30, 0));
                public VolatileHashSet<Hash> PulledBlockRequestSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 30, 0));
                public VolatileHashSet<Signature> PulledBroadcastMetadataRequestSet { get; private set; } = new VolatileHashSet<Signature>(new TimeSpan(0, 3, 0));
                public VolatileHashSet<Signature> PulledUnicastMetadataRequestSet { get; private set; } = new VolatileHashSet<Signature>(new TimeSpan(0, 3, 0));
                public VolatileHashSet<Tag> PulledMulticastMetadataRequestSet { get; private set; } = new VolatileHashSet<Tag>(new TimeSpan(0, 3, 0));

                public void Update()
                {
                    this.PulledLocationSet.Update();
                    this.PulledBlockLinkSet.Update();
                    this.PulledBlockRequestSet.Update();
                    this.PulledBroadcastMetadataRequestSet.Update();
                    this.PulledUnicastMetadataRequestSet.Update();
                    this.PulledMulticastMetadataRequestSet.Update();
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                foreach (var taskManager in _connectTaskManagers)
                {
                    taskManager.Dispose();
                }
                _connectTaskManagers.Clear();

                foreach (var taskManager in _acceptTaskManagers)
                {
                    taskManager.Dispose();
                }
                _acceptTaskManagers.Clear();

                _computeTaskManager.Dispose();
                _computeTaskManager = null;

                foreach (var taskManager in _sendTaskManagers)
                {
                    taskManager.Dispose();
                }
                _sendTaskManagers.Clear();

                foreach (var taskManager in _receiveTaskManagers)
                {
                    taskManager.Dispose();
                }
                _receiveTaskManagers.Clear();

                if (_connectionLockManager != null)
                {
                    _connectionLockManager.Dispose();
                    _connectionLockManager = null;
                }
            }
        }
    }

    class NetworkManagerException : ManagerException
    {
        public NetworkManagerException() : base() { }
        public NetworkManagerException(string message) : base(message) { }
        public NetworkManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
