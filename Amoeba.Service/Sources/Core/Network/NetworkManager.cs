using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Omnius.Utilities;

namespace Amoeba.Service
{
    public delegate IEnumerable<Signature> GetSignaturesEventHandler(object sender);

    public delegate Cap ConnectCapEventHandler(object sender, string uri);
    public delegate Cap AcceptCapEventHandler(object sender, out string uri);

    class NetworkManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CacheManager _cacheManager;

        private Settings _settings;

        private Random _random = new Random();

        private volatile Location _myLocation;

        private RouteTable<SessionInfo> _routeTable;
        private LockedHashSet<Location> _cloudLocations = new LockedHashSet<Location>();

        private volatile int _connectionCountLimit;
        private volatile int _bandwidthLimit;

        private LockedHashDictionary<Connection, SessionInfo> _connections = new LockedHashDictionary<Connection, SessionInfo>();

        private MetadataManager _metadataManager;

        private List<TaskManager> _connectTaskManagers = new List<TaskManager>();
        private List<TaskManager> _acceptTaskManagers = new List<TaskManager>();
        private TaskManager _computeTaskManager;
        private List<TaskManager> _sendTaskManagers = new List<TaskManager>();
        private List<TaskManager> _receiveTaskManagers = new List<TaskManager>();

        private VolatileHashDictionary<Hash, DiffusionPriority> _pushBlocksRequestMap = new VolatileHashDictionary<Hash, DiffusionPriority>(new TimeSpan(0, 3, 0));

        private VolatileHashSet<Signature> _pushBroadcastMetadatasRequestSet = new VolatileHashSet<Signature>(new TimeSpan(0, 3, 0));
        private VolatileHashSet<Signature> _pushUnicastMetadatasRequestSet = new VolatileHashSet<Signature>(new TimeSpan(0, 3, 0));
        private VolatileHashSet<Tag> _pushMulticastMetadatasRequestSet = new VolatileHashSet<Tag>(new TimeSpan(0, 3, 0));

        private SafeInteger _receivedByteCount = new SafeInteger();
        private SafeInteger _sentByteCount = new SafeInteger();

        private ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        private const int _maxLocationCount = 256;
        private const int _maxBlockLinkCount = 8192;
        private const int _maxBlockRequestCount = 8192;
        private const int _maxMetadataRequestCount = 2048;
        private const int _maxMetadataResultCount = 2048;

        private readonly int _threadCount = Math.Max(2, Math.Min(System.Environment.ProcessorCount, 32) / 2);

        public NetworkManager(string configPath, CacheManager cacheManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _cacheManager = cacheManager;

            _settings = new Settings(configPath);

            _routeTable = new RouteTable<SessionInfo>(256, 32);

            _metadataManager = new MetadataManager();
            _metadataManager.GetLockSignaturesEvent += (_) => this.OnGetLockSignatures();

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
        }

        private volatile Info _info = new Info();

        public class Info
        {
            public SafeInteger ConnectCount { get; } = new SafeInteger();
            public SafeInteger AcceptCount { get; } = new SafeInteger();

            public SafeInteger PushLocationCount { get; } = new SafeInteger();
            public SafeInteger PushBlockLinkCount { get; } = new SafeInteger();
            public SafeInteger PushBlockRequestCount { get; } = new SafeInteger();
            public SafeInteger PushBlockResultCount { get; } = new SafeInteger();
            public SafeInteger PushMessageRequestCount { get; } = new SafeInteger();
            public SafeInteger PushMessageResultCount { get; } = new SafeInteger();

            public SafeInteger PullLocationCount { get; } = new SafeInteger();
            public SafeInteger PullBlockLinkCount { get; } = new SafeInteger();
            public SafeInteger PullBlockRequestCount { get; } = new SafeInteger();
            public SafeInteger PullBlockResultCount { get; } = new SafeInteger();
            public SafeInteger PullMessageRequestCount { get; } = new SafeInteger();
            public SafeInteger PullMessageResultCount { get; } = new SafeInteger();
        }

        public NetworkReport Report
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (_lockObject)
                {
                    return new NetworkReport(
                        _myLocation,
                        _info.ConnectCount,
                        _info.AcceptCount,
                        _routeTable.Count,
                        _metadataManager.Count,
                        _receivedByteCount,
                        _sentByteCount,
                        _info.PushLocationCount,
                        _info.PushBlockLinkCount,
                        _info.PushBlockRequestCount,
                        _info.PushBlockResultCount,
                        _info.PushMessageRequestCount,
                        _info.PushMessageResultCount,
                        _info.PullLocationCount,
                        _info.PullBlockLinkCount,
                        _info.PullBlockRequestCount,
                        _info.PullBlockResultCount,
                        _info.PullMessageRequestCount,
                        _info.PullMessageResultCount);
                }
            }
        }

        public IEnumerable<NetworkConnectionReport> GetNetworkConnectionReports()
        {
            lock (_lockObject)
            {
                var list = new List<NetworkConnectionReport>();

                foreach (var sessionInfo in _routeTable.ToArray().Select(n => n.Value))
                {
                    list.Add(new NetworkConnectionReport(
                        sessionInfo.Id,
                        sessionInfo.Type,
                        sessionInfo.Uri,
                        sessionInfo.Location,
                        Math.Round(sessionInfo.PriorityManager.GetPriority() * 100, 2),
                        sessionInfo.Connection.ReceivedByteCount,
                        sessionInfo.Connection.SentByteCount));
                }

                return list;
            }
        }

        public NetworkConfig Config
        {
            get
            {
                lock (_lockObject)
                {
                    return new NetworkConfig(_connectionCountLimit, _bandwidthLimit);
                }
            }
        }

        public void SetConfig(NetworkConfig config)
        {
            lock (_lockObject)
            {
                _connectionCountLimit = config.ConnectionCountLimit;
                _bandwidthLimit = config.BandwidthLimit;
            }
        }

        public Location MyLocation
        {
            get
            {
                lock (_lockObject)
                {
                    return _myLocation;
                }
            }
        }

        public void SetMyLocation(Location myLocation)
        {
            lock (_lockObject)
            {
                if (_myLocation != myLocation)
                {
                    foreach (var connection in _connections.Keys.ToArray())
                    {
                        this.RemoveConnection(connection);
                    }

                    this.UpdateMyId();
                }

                _myLocation = myLocation;
            }
        }

        public IEnumerable<Location> CloudLocations
        {
            get
            {
                lock (_lockObject)
                {
                    return _cloudLocations;
                }
            }
        }

        public void SetCloudLocations(IEnumerable<Location> locations)
        {
            lock (_lockObject)
            {
                _cloudLocations.UnionWith(locations);
            }
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

        private void UpdateMyId()
        {
            lock (_lockObject)
            {
                var baseId = new byte[32];

                using (var random = RandomNumberGenerator.Create())
                {
                    random.GetBytes(baseId);
                }

                _routeTable.BaseId = baseId;
            }
        }

        private VolatileHashSet<Location> _connectingLocations = new VolatileHashSet<Location>(new TimeSpan(0, 1, 0));

        private void ConnectThread(CancellationToken token)
        {
            try
            {
                for (; ; )
                {
                    if (token.WaitHandle.WaitOne(1000)) return;

                    // 接続数を制限する。
                    {
                        int connectionCount = _routeTable.ToArray().Select(n => n.Value).Count(n => n.Type == SessionType.Connect);
                        if (connectionCount >= (_connectionCountLimit / 2)) continue;
                    }

                    Location location = null;

                    lock (_lockObject)
                    {
                        _connectingLocations.Update();

                        switch (_random.Next(0, 2))
                        {
                            case 0:
                                location = _cloudLocations.Randomize()
                                    .Where(n => !_connectingLocations.Contains(n))
                                    .FirstOrDefault();
                                break;
                            case 1:
                                var sessionInfo = _routeTable.ToArray().Randomize().Select(n => n.Value).FirstOrDefault();
                                if (sessionInfo == null) break;

                                location = sessionInfo.ReceiveInfo.PullLocationSet.Randomize()
                                    .Where(n => !_connectingLocations.Contains(n))
                                    .FirstOrDefault();
                                break;
                        }

                        if (location == null || _myLocation.Uris.Any(n => location.Uris.Contains(n))
                            || _routeTable.ToArray().SelectMany(n => n.Value.Location.Uris).Any(m => location.Uris.Contains(m))) continue;

                        _connectingLocations.Add(location);
                    }

                    Cap cap = null;
                    string uri = null;

                    foreach (string tempUri in new HashSet<string>(location.Uris))
                    {
                        var tempCap = this.OnConnectCap(tempUri);
                        if (tempCap == null) continue;

                        cap = tempCap;
                        uri = tempUri;
                    }

                    if (cap == null)
                    {
                        if (_cloudLocations.Count > 1024) _cloudLocations.Remove(location);

                        continue;
                    }

                    _info.ConnectCount.Increment();

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
                        int connectionCount = _routeTable.ToArray().Select(n => n.Value).Count(n => n.Type == SessionType.Accept);
                        if (connectionCount >= (_connectionCountLimit / 2)) continue;
                    }

                    var cap = this.OnAcceptCap(out string uri);
                    if (cap == null) continue;

                    _info.AcceptCount.Increment();

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
            lock (_lockObject)
            {
                var connection = new Connection(1024 * 1024 * 4, _bufferManager);
                connection.Connect(cap);

                var sessionInfo = new SessionInfo();
                sessionInfo.Connection = connection;
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

                for (int i = 0; i < _threadCount; i++)
                {
                    dic.Add(i, _connections.Values.Count(n => n.ThreadId == i));
                }

                var sortedList = dic.Randomize().ToList();
                sortedList.Sort((x, y) => x.Value.CompareTo(y.Value));

                return sortedList.First().Key;
            }
        }

        private void RemoveConnection(Connection connection)
        {
            lock (_lockObject)
            {
                if (_connections.TryGetValue(connection, out var sessionInfo))
                {
                    _connections.Remove(connection);

                    connection.Dispose();

                    if (sessionInfo.Id != null)
                    {
                        _routeTable.Remove(sessionInfo.Id);
                    }

                    if (sessionInfo.Location != null) _cloudLocations.Add(sessionInfo.Location);
                }
            }
        }

        private void ComputeThread(CancellationToken token)
        {
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

                        // 古いPushリクエスト情報を削除する。
                        {
                            _pushBlocksRequestMap.Update();

                            _pushBroadcastMetadatasRequestSet.Update();
                            _pushUnicastMetadatasRequestSet.Update();
                            _pushMulticastMetadatasRequestSet.Update();
                        }

                        // 不要なMetadataを削除する。
                        {
                            _metadataManager.Refresh();
                        }

                        // 古いセッション情報を破棄する。
                        {
                            foreach (var sessionInfo in _routeTable.ToArray().Select(n => n.Value))
                            {
                                sessionInfo.Update();
                            }
                        }

                        // 長い間通信の無い接続を切断する。
                        {
                            foreach (var sessionInfo in _routeTable.ToArray().Select(n => n.Value))
                            {
                                if (sessionInfo.ReceiveInfo.Stopwatch.Elapsed.TotalMinutes > 10)
                                {
                                    this.RemoveConnection(sessionInfo.Connection);
                                }
                            }
                        }
                    }

                    if (reduceStopwatch.Elapsed.TotalMinutes >= 10)
                    {
                        reduceStopwatch.Restart();

                        // 優先度の低い通信を切断する。
                        {
                            var list = _routeTable.ToArray().Select(n => n.Value).ToList();
                            _random.Shuffle(list);
                            list.Sort((x, y) => x.PriorityManager.GetPriority().CompareTo(y.PriorityManager.GetPriority()));

                            foreach (var sessionInfo in list.Take(1))
                            {
                                this.RemoveConnection(sessionInfo.Connection);
                            }
                        }
                    }

                    // アップロード
                    if (_routeTable.Count >= 3
                        && pushBlockUploadStopwatch.Elapsed.TotalSeconds >= 5)
                    {
                        pushBlockUploadStopwatch.Restart();

                        foreach (var node in _routeTable.ToArray())
                        {
                            var tempList = _cacheManager.IntersectFrom(node.Value.ReceiveInfo.PullBlockRequestSet.Randomize()).Take(256).ToList();

                            lock (node.Value.SendInfo.PushBlockResultQueue.LockObject)
                            {
                                node.Value.SendInfo.PushBlockResultQueue.Clear();

                                foreach (var item in tempList)
                                {
                                    node.Value.SendInfo.PushBlockResultQueue.Enqueue(item);
                                }
                            }
                        }
                    }

                    // ダウンロード
                    if (_routeTable.Count >= 3
                        && pushBlockDownloadStopwatch.Elapsed.TotalSeconds >= 30)
                    {
                        pushBlockDownloadStopwatch.Restart();

                        var pushBlockLinkSet = new HashSet<Hash>();
                        var pushBlockRequestSet = new HashSet<Hash>();

                        {
                            // Link
                            {
                                var myPushBlockLinkSet = new HashSet<Hash>();
                                var cloudPushBlockLinkSet = new HashSet<Hash>();

                                {
                                    var list = _cacheManager.ToArray();
                                    _random.Shuffle(list);

                                    myPushBlockLinkSet.UnionWith(list.Take(_maxBlockLinkCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = info.ReceiveInfo.PullBlockLinkSet.ToArray(new TimeSpan(0, 10, 0));
                                    _random.Shuffle(list);

                                    cloudPushBlockLinkSet.UnionWith(list.Take((int)(_maxBlockLinkCount * info.PriorityManager.GetPriority())));
                                }

                                pushBlockLinkSet.UnionWith(myPushBlockLinkSet);
                                pushBlockLinkSet.UnionWith(cloudPushBlockLinkSet.Randomize());
                            }

                            // Request
                            {
                                var myPushBlockRequestSet = new HashSet<Hash>();
                                var cloudPushBlockRequestSet = new HashSet<Hash>();

                                {
                                    List<Hash> list;
                                    {
                                        var sortedList = _pushBlocksRequestMap.ToList();
                                        _random.Shuffle(sortedList);
                                        sortedList.Sort((x, y) => x.Value.CompareTo(y.Value));

                                        list = _cacheManager.ExceptFrom(sortedList.Select(n => n.Key).ToArray()).ToList();
                                    }

                                    myPushBlockRequestSet.UnionWith(list.Take(_maxBlockRequestCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = _cacheManager.ExceptFrom(info.ReceiveInfo.PullBlockRequestSet.ToArray(new TimeSpan(0, 10, 0))).ToArray();
                                    _random.Shuffle(list);

                                    cloudPushBlockRequestSet.UnionWith(list.Take((int)(_maxBlockRequestCount * info.PriorityManager.GetPriority())));
                                }

                                pushBlockRequestSet.UnionWith(myPushBlockRequestSet);
                                pushBlockRequestSet.UnionWith(cloudPushBlockRequestSet.Randomize());
                            }
                        }

                        {
                            var cloudNodes = _routeTable.ToArray();

                            // Link
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Hash>>();

                                foreach (var hash in pushBlockLinkSet.Randomize())
                                {
                                    foreach (var node in GetTargetNodes(_routeTable.BaseId, hash.Value, cloudNodes))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Hash>()).Add(hash);
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    _random.Shuffle(targets);

                                    lock (node.Value.SendInfo.PushBlockLinkQueue.LockObject)
                                    {
                                        node.Value.SendInfo.PushBlockLinkQueue.Clear();
                                        node.Value.SendInfo.PushBlockLinkQueue.AddRange(targets.Take(_maxBlockLinkCount));
                                    }
                                }
                            }

                            // Request
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, HashSet<Hash>>();

                                foreach (var hash in pushBlockRequestSet.Randomize())
                                {
                                    foreach (var node in GetTargetNodes(_routeTable.BaseId, hash.Value, cloudNodes))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new HashSet<Hash>()).Add(hash);
                                    }

                                    foreach (var node in cloudNodes.Where(n => n.Value.ReceiveInfo.PullBlockLinkSet.Contains(hash)))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new HashSet<Hash>()).Add(hash);
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    lock (node.Value.SendInfo.PushBlockRequestQueue.LockObject)
                                    {
                                        node.Value.SendInfo.PushBlockRequestQueue.Clear();
                                        node.Value.SendInfo.PushBlockRequestQueue.AddRange(targets.Take(_maxBlockRequestCount).Randomize());
                                    }
                                }
                            }
                        }
                    }

                    // アップロード
                    if (_routeTable.Count >= 3
                        && pushMetadataUploadStopwatch.Elapsed.TotalSeconds >= 30)
                    {
                        pushMetadataUploadStopwatch.Restart();

                        // BroadcastMetadata
                        foreach (var signature in _metadataManager.GetBroadcastSignatures())
                        {
                            foreach (var node in _routeTable.Search(signature.Id, 2))
                            {
                                node.Value.ReceiveInfo.PullBroadcastMetadataRequestSet.Add(signature);
                            }
                        }

                        // UnicastMetadata
                        foreach (var signature in _metadataManager.GetUnicastSignatures())
                        {
                            foreach (var node in _routeTable.Search(signature.Id, 2))
                            {
                                node.Value.ReceiveInfo.PullUnicastMetadataRequestSet.Add(signature);
                            }
                        }

                        // MulticastMetadata
                        foreach (var tag in _metadataManager.GetMulticastTags())
                        {
                            foreach (var node in _routeTable.Search(tag.Id, 2))
                            {
                                node.Value.ReceiveInfo.PullMulticastMetadataRequestSet.Add(tag);
                            }
                        }
                    }

                    // ダウンロード
                    if (_routeTable.Count >= 3
                        && pushMetadataDownloadStopwatch.Elapsed.TotalSeconds >= 30)
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
                                    _random.Shuffle(list);

                                    pushBroadcastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = info.ReceiveInfo.PullBroadcastMetadataRequestSet.ToArray(new TimeSpan(0, 10, 0));
                                    _random.Shuffle(list);

                                    pushBroadcastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }
                            }

                            // UnicastMetadata
                            {
                                {
                                    var list = _pushUnicastMetadatasRequestSet.ToArray();
                                    _random.Shuffle(list);

                                    pushUnicastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = info.ReceiveInfo.PullUnicastMetadataRequestSet.ToArray(new TimeSpan(0, 10, 0));
                                    _random.Shuffle(list);

                                    pushUnicastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }
                            }

                            // MulticastMetadata
                            {
                                {
                                    var list = _pushMulticastMetadatasRequestSet.ToArray();
                                    _random.Shuffle(list);

                                    pushMulticastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = info.ReceiveInfo.PullMulticastMetadataRequestSet.ToArray(new TimeSpan(0, 10, 0));
                                    _random.Shuffle(list);

                                    pushMulticastMetadatasRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }
                            }
                        }

                        {
                            var cloudNodes = _routeTable.ToArray();

                            // BroadcastMetadata
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Signature>>();

                                foreach (var signature in pushBroadcastMetadatasRequestSet.Randomize())
                                {
                                    foreach (var node in GetTargetNodes(_routeTable.BaseId, signature.Id, cloudNodes))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Signature>()).Add(signature);
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    _random.Shuffle(targets);

                                    lock (node.Value.SendInfo.PushBroadcastMetadataRequestQueue.LockObject)
                                    {
                                        node.Value.SendInfo.PushBroadcastMetadataRequestQueue.Clear();
                                        node.Value.SendInfo.PushBroadcastMetadataRequestQueue.AddRange(targets.Take(_maxMetadataRequestCount));
                                    }
                                }
                            }

                            // UnicastMetadata
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Signature>>();

                                foreach (var signature in pushUnicastMetadatasRequestSet.Randomize())
                                {
                                    foreach (var node in GetTargetNodes(_routeTable.BaseId, signature.Id, cloudNodes))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Signature>()).Add(signature);
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    _random.Shuffle(targets);

                                    lock (node.Value.SendInfo.PushUnicastMetadataRequestQueue.LockObject)
                                    {
                                        node.Value.SendInfo.PushUnicastMetadataRequestQueue.Clear();
                                        node.Value.SendInfo.PushUnicastMetadataRequestQueue.AddRange(targets.Take(_maxMetadataRequestCount));
                                    }
                                }
                            }

                            // MulticastMetadata
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Tag>>();

                                foreach (var tag in pushMulticastMetadatasRequestSet.Randomize())
                                {
                                    foreach (var node in GetTargetNodes(_routeTable.BaseId, tag.Id, cloudNodes))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Tag>()).Add(tag);
                                    }
                                }

                                foreach (var (node, targets) in tempMap)
                                {
                                    _random.Shuffle(targets);

                                    lock (node.Value.SendInfo.PushMulticastMetadataRequestQueue.LockObject)
                                    {
                                        node.Value.SendInfo.PushMulticastMetadataRequestQueue.Clear();
                                        node.Value.SendInfo.PushMulticastMetadataRequestQueue.AddRange(targets.Take(_maxMetadataRequestCount));
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

            IEnumerable<Node<T>> GetTargetNodes<T>(byte[] baseId, byte[] targetId, IEnumerable<Node<T>> cloudNodes)
            {
                var tempList = new List<Node<T>>();
                tempList.AddRange(RouteTable<T>.Search(baseId, targetId, cloudNodes, 6));
                tempList.AddRange(cloudNodes.Randomize().Take(1));
                _random.Shuffle(tempList);

                return tempList.Take(2);
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
                        int remain = (_bandwidthLimit != 0) ? _bandwidthLimit / _threadCount : int.MaxValue;

                        foreach (var connection in _connections.Where(n => n.Value.ThreadId == threadId).Select(n => n.Key).Randomize())
                        {
                            try
                            {
                                int count = connection.Send(Math.Min(remain, 1024 * 1024 * 4));
                                _sentByteCount.Add(count);

                                remain -= count;
                                if (remain == 0) break;
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e);

                                this.RemoveConnection(connection);
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
                        int remain = (_bandwidthLimit != 0) ? _bandwidthLimit / _threadCount : int.MaxValue;

                        foreach (var connection in _connections.Where(n => n.Value.ThreadId == threadId).Select(n => n.Key).Randomize())
                        {
                            try
                            {
                                int count = connection.Receive(Math.Min(remain, 1024 * 1024 * 4));
                                _receivedByteCount.Add(count);

                                remain -= count;
                                if (remain == 0) break;
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e);

                                this.RemoveConnection(connection);
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

        private enum ProtocolVersion : uint
        {
            Version0 = 0,
            Version1 = 1,
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
            // Init
            if (!sessionInfo.SendInfo.IsInitialized)
            {
                sessionInfo.SendInfo.IsInitialized = true;

                Stream versionStream = new BufferStream(_bufferManager);
                VintUtils.SetUInt64(versionStream, (uint)ProtocolVersion.Version1);

                var dataStream = (new ProfilePacket(_routeTable.BaseId, _myLocation)).Export(_bufferManager);

                return new UniteStream(versionStream, dataStream);
            }

            if (!sessionInfo.ReceiveInfo.IsInitialized) return null;

            // Locations
            if (sessionInfo.SendInfo.LocationResultStopwatch.Elapsed.TotalSeconds > 30)
            {
                sessionInfo.SendInfo.LocationResultStopwatch.Restart();

                var tempLocations = new List<Location>();
                tempLocations.Add(this.MyLocation);
                tempLocations.AddRange(_cloudLocations);
                _random.Shuffle(tempLocations);

                var packet = new LocationsPacket(tempLocations.Take(_maxLocationCount));

                Stream typeStream = new BufferStream(_bufferManager);
                VintUtils.SetUInt64(typeStream, (uint)SerializeId.Locations);

                _info.PushLocationCount.Add(packet.Locations.Count());

                return new UniteStream(typeStream, packet.Export(_bufferManager));
            }

            // BlocksLink
            if (sessionInfo.SendInfo.PushBlockLinkQueue.Count > 0)
            {
                BlocksLinkPacket packet;

                lock (sessionInfo.SendInfo.PushBlockLinkQueue.LockObject)
                {
                    packet = new BlocksLinkPacket(sessionInfo.SendInfo.PushBlockLinkQueue);
                    sessionInfo.SendInfo.PushBlockLinkQueue.Clear();
                }

                Stream typeStream = new BufferStream(_bufferManager);
                VintUtils.SetUInt64(typeStream, (uint)SerializeId.BlocksLink);

                _info.PushBlockLinkCount.Add(packet.Hashes.Count());

                sessionInfo.SendInfo.PushBlockLinkSet.AddRange(packet.Hashes);

                return new UniteStream(typeStream, packet.Export(_bufferManager));
            }

            // BlocksRequest
            if (sessionInfo.SendInfo.PushBlockRequestQueue.Count > 0)
            {
                BlocksRequestPacket packet;

                lock (sessionInfo.SendInfo.PushBlockRequestQueue.LockObject)
                {
                    packet = new BlocksRequestPacket(sessionInfo.SendInfo.PushBlockRequestQueue);
                    sessionInfo.SendInfo.PushBlockRequestQueue.Clear();
                }

                Stream typeStream = new BufferStream(_bufferManager);
                VintUtils.SetUInt64(typeStream, (uint)SerializeId.BlocksRequest);

                _info.PushBlockRequestCount.Add(packet.Hashes.Count());

                sessionInfo.SendInfo.PushBlockRequestSet.AddRange(packet.Hashes);

                return new UniteStream(typeStream, packet.Export(_bufferManager));
            }

            // BlockResult
            if (sessionInfo.SendInfo.BlockResultStopwatch.Elapsed.TotalSeconds > 1)
            {
                sessionInfo.SendInfo.BlockResultStopwatch.Restart();

                if (_random.Next(0, 100) < (sessionInfo.PriorityManager.GetPriority() * 100))
                {
                    Hash hash = null;

                    lock (sessionInfo.SendInfo.PushBlockResultQueue.LockObject)
                    {
                        if (sessionInfo.SendInfo.PushBlockResultQueue.Count > 0)
                        {
                            hash = sessionInfo.SendInfo.PushBlockResultQueue.Dequeue();
                        }
                    }

                    Stream dataStream = null;

                    if (hash != null)
                    {
                        var buffer = new ArraySegment<byte>();

                        try
                        {
                            buffer = _cacheManager[hash];

                            dataStream = (new BlockResultPacket(hash, buffer)).Export(_bufferManager);
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
                        Stream typeStream = new BufferStream(_bufferManager);
                        VintUtils.SetUInt64(typeStream, (uint)SerializeId.BlockResult);

                        _info.PushBlockResultCount.Increment();

                        sessionInfo.SendInfo.PushBlockResultSet.Add(hash);
                        sessionInfo.ReceiveInfo.PullBlockRequestSet.Remove(hash);

                        return new UniteStream(typeStream, dataStream);
                    }
                }
            }

            if (ProtocolVersion.Version1 <= sessionInfo.Version)
            {
                // BroadcastMetadatasRequest
                if (sessionInfo.SendInfo.PushBroadcastMetadataRequestQueue.Count > 0)
                {
                    BroadcastMetadatasRequestPacket packet;

                    lock (sessionInfo.SendInfo.PushBroadcastMetadataRequestQueue.LockObject)
                    {
                        packet = new BroadcastMetadatasRequestPacket(sessionInfo.SendInfo.PushBroadcastMetadataRequestQueue);
                        sessionInfo.SendInfo.PushBroadcastMetadataRequestQueue.Clear();
                    }

                    Stream typeStream = new BufferStream(_bufferManager);
                    VintUtils.SetUInt64(typeStream, (uint)SerializeId.BroadcastMetadatasRequest);

                    _info.PushMessageRequestCount.Add(packet.Signatures.Count());

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }

                // BroadcastMetadatasResult
                if (sessionInfo.SendInfo.BroadcastMetadataResultStopwatch.Elapsed.TotalSeconds > 30)
                {
                    sessionInfo.SendInfo.BroadcastMetadataResultStopwatch.Restart();

                    var broadcastMetadatas = new List<BroadcastMetadata>();

                    var signatures = sessionInfo.ReceiveInfo.PullBroadcastMetadataRequestSet.ToList();
                    _random.Shuffle(signatures);

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

                        Stream typeStream = new BufferStream(_bufferManager);
                        VintUtils.SetUInt64(typeStream, (uint)SerializeId.BroadcastMetadatasResult);

                        _info.PushMessageResultCount.Add(packet.BroadcastMetadatas.Count());

                        return new UniteStream(typeStream, packet.Export(_bufferManager));
                    }
                }

                // UnicastMetadatasRequest
                if (sessionInfo.SendInfo.PushUnicastMetadataRequestQueue.Count > 0)
                {
                    UnicastMetadatasRequestPacket packet;

                    lock (sessionInfo.SendInfo.PushUnicastMetadataRequestQueue.LockObject)
                    {
                        packet = new UnicastMetadatasRequestPacket(sessionInfo.SendInfo.PushUnicastMetadataRequestQueue);
                        sessionInfo.SendInfo.PushUnicastMetadataRequestQueue.Clear();
                    }

                    Stream typeStream = new BufferStream(_bufferManager);
                    VintUtils.SetUInt64(typeStream, (uint)SerializeId.UnicastMetadatasRequest);

                    _info.PushMessageRequestCount.Add(packet.Signatures.Count());

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }

                // UnicastMetadatasResult
                if (sessionInfo.SendInfo.UnicastMetadataResultStopwatch.Elapsed.TotalSeconds > 30)
                {
                    sessionInfo.SendInfo.UnicastMetadataResultStopwatch.Restart();

                    var unicastMetadatas = new List<UnicastMetadata>();

                    var signatures = sessionInfo.ReceiveInfo.PullUnicastMetadataRequestSet.ToList();
                    _random.Shuffle(signatures);

                    foreach (var signature in signatures)
                    {
                        foreach (var metadata in _metadataManager.GetUnicastMetadatas(signature).Randomize())
                        {
                            unicastMetadatas.Add(metadata);

                            if (unicastMetadatas.Count >= _maxMetadataResultCount) goto End;
                        }
                    }

                    End:;

                    if (unicastMetadatas.Count > 0)
                    {
                        var packet = new UnicastMetadatasResultPacket(unicastMetadatas);
                        unicastMetadatas.Clear();

                        Stream typeStream = new BufferStream(_bufferManager);
                        VintUtils.SetUInt64(typeStream, (uint)SerializeId.UnicastMetadatasResult);

                        _info.PushMessageResultCount.Add(packet.UnicastMetadatas.Count());

                        return new UniteStream(typeStream, packet.Export(_bufferManager));
                    }
                }

                // MulticastMetadatasRequest
                if (sessionInfo.SendInfo.PushMulticastMetadataRequestQueue.Count > 0)
                {
                    MulticastMetadatasRequestPacket packet;

                    lock (sessionInfo.SendInfo.PushMulticastMetadataRequestQueue.LockObject)
                    {
                        packet = new MulticastMetadatasRequestPacket(sessionInfo.SendInfo.PushMulticastMetadataRequestQueue);
                        sessionInfo.SendInfo.PushMulticastMetadataRequestQueue.Clear();
                    }

                    Stream typeStream = new BufferStream(_bufferManager);
                    VintUtils.SetUInt64(typeStream, (uint)SerializeId.MulticastMetadatasRequest);

                    _info.PushMessageRequestCount.Add(packet.Tags.Count());

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }

                // MulticastMetadatasResult
                if (sessionInfo.SendInfo.MulticastMetadataResultStopwatch.Elapsed.TotalSeconds > 30)
                {
                    sessionInfo.SendInfo.MulticastMetadataResultStopwatch.Restart();

                    var multicastMetadatas = new List<MulticastMetadata>();

                    var tags = sessionInfo.ReceiveInfo.PullMulticastMetadataRequestSet.ToList();
                    _random.Shuffle(tags);

                    foreach (var tag in tags)
                    {
                        foreach (var metadata in _metadataManager.GetMulticastMetadatas(tag).Randomize())
                        {
                            multicastMetadatas.Add(metadata);

                            if (multicastMetadatas.Count >= _maxMetadataResultCount) goto End;
                        }
                    }

                    End:;

                    if (multicastMetadatas.Count > 0)
                    {
                        var packet = new MulticastMetadatasResultPacket(multicastMetadatas);
                        multicastMetadatas.Clear();

                        Stream typeStream = new BufferStream(_bufferManager);
                        VintUtils.SetUInt64(typeStream, (uint)SerializeId.MulticastMetadatasResult);

                        _info.PushMessageResultCount.Add(packet.MulticastMetadatas.Count());

                        return new UniteStream(typeStream, packet.Export(_bufferManager));
                    }
                }
            }

            return null;
        }

        private void Receive(SessionInfo sessionInfo, Stream stream)
        {
            sessionInfo.ReceiveInfo.Stopwatch.Restart();

            if (!sessionInfo.ReceiveInfo.IsInitialized)
            {
                var targetVersion = (ProtocolVersion)VintUtils.GetUInt64(stream);
                sessionInfo.Version = (ProtocolVersion)Math.Min((uint)targetVersion, (uint)ProtocolVersion.Version1);

                using (var dataStream = new RangeStream(stream))
                {
                    var profile = ProfilePacket.Import(dataStream, _bufferManager);
                    if (profile.Id == null || profile.Location == null) throw new ArgumentException("Broken Profile");

                    lock (_lockObject)
                    {
                        if (Unsafe.Equals(profile.Id, _routeTable.BaseId)) throw new ArgumentException("Conflict");

                        if (_connections.ContainsKey(sessionInfo.Connection))
                        {
                            sessionInfo.Id = profile.Id;
                            sessionInfo.Location = profile.Location;

                            if (!_routeTable.Add(profile.Id, sessionInfo)) throw new ArgumentException("RouteTable Overflow");
                        }
                    }
                }

                sessionInfo.ReceiveInfo.IsInitialized = true;
            }
            else
            {
                try
                {
                    int id = (int)VintUtils.GetUInt64(stream);

                    using (var dataStream = new RangeStream(stream))
                    {
                        if (id == (int)SerializeId.Locations)
                        {
                            if (sessionInfo.ReceiveInfo.PullLocationSet.Count > _maxLocationCount * sessionInfo.ReceiveInfo.PullLocationSet.SurvivalTime.TotalMinutes * 2) return;

                            var packet = LocationsPacket.Import(dataStream, _bufferManager);

                            _info.PullLocationCount.Add(packet.Locations.Count());

                            sessionInfo.ReceiveInfo.PullLocationSet.AddRange(packet.Locations);
                        }
                        else if (id == (int)SerializeId.BlocksLink)
                        {
                            if (sessionInfo.ReceiveInfo.PullBlockLinkSet.Count > _maxBlockLinkCount * sessionInfo.ReceiveInfo.PullBlockLinkSet.SurvivalTime.TotalMinutes * 2) return;

                            var packet = BlocksLinkPacket.Import(dataStream, _bufferManager);

                            _info.PullBlockLinkCount.Add(packet.Hashes.Count());

                            sessionInfo.ReceiveInfo.PullBlockLinkSet.AddRange(packet.Hashes);
                        }
                        else if (id == (int)SerializeId.BlocksRequest)
                        {
                            if (sessionInfo.ReceiveInfo.PullBlockRequestSet.Count > _maxBlockRequestCount * sessionInfo.ReceiveInfo.PullBlockRequestSet.SurvivalTime.TotalMinutes * 2) return;

                            var packet = BlocksRequestPacket.Import(dataStream, _bufferManager);

                            _info.PullBlockRequestCount.Add(packet.Hashes.Count());

                            sessionInfo.ReceiveInfo.PullBlockRequestSet.AddRange(packet.Hashes.Where(n => !sessionInfo.SendInfo.PushBlockResultSet.Contains(n)));
                        }
                        else if (id == (int)SerializeId.BlockResult)
                        {
                            var packet = BlockResultPacket.Import(dataStream, _bufferManager);

                            _info.PullBlockResultCount.Increment();

                            try
                            {
                                _cacheManager[packet.Hash] = packet.Value;

                                sessionInfo.SendInfo.PushBlockResultSet.Add(packet.Hash);

                                if (sessionInfo.SendInfo.PushBlockRequestSet.Contains(packet.Hash))
                                {
                                    sessionInfo.PriorityManager.Increment();
                                }
                            }
                            finally
                            {
                                if (packet.Value.Array != null)
                                {
                                    _bufferManager.ReturnBuffer(packet.Value.Array);
                                }
                            }
                        }
                        else if (id == (int)SerializeId.BroadcastMetadatasRequest)
                        {
                            if (sessionInfo.ReceiveInfo.PullBroadcastMetadataRequestSet.Count > _maxMetadataRequestCount * sessionInfo.ReceiveInfo.PullBroadcastMetadataRequestSet.SurvivalTime.TotalMinutes * 2) return;

                            var packet = BroadcastMetadatasRequestPacket.Import(dataStream, _bufferManager);

                            _info.PullMessageRequestCount.Add(packet.Signatures.Count());

                            sessionInfo.ReceiveInfo.PullBroadcastMetadataRequestSet.AddRange(packet.Signatures);
                        }
                        else if (id == (int)SerializeId.BroadcastMetadatasResult)
                        {
                            var packet = BroadcastMetadatasResultPacket.Import(dataStream, _bufferManager);

                            _info.PullMessageResultCount.Add(packet.BroadcastMetadatas.Count());

                            foreach (var metadata in packet.BroadcastMetadatas)
                            {
                                _metadataManager.SetMetadata(metadata);
                            }
                        }
                        else if (id == (int)SerializeId.UnicastMetadatasRequest)
                        {
                            if (sessionInfo.ReceiveInfo.PullUnicastMetadataRequestSet.Count > _maxMetadataRequestCount * sessionInfo.ReceiveInfo.PullUnicastMetadataRequestSet.SurvivalTime.TotalMinutes * 2) return;

                            var packet = UnicastMetadatasRequestPacket.Import(dataStream, _bufferManager);

                            _info.PullMessageRequestCount.Add(packet.Signatures.Count());

                            sessionInfo.ReceiveInfo.PullUnicastMetadataRequestSet.AddRange(packet.Signatures);
                        }
                        else if (id == (int)SerializeId.UnicastMetadatasResult)
                        {
                            var packet = UnicastMetadatasResultPacket.Import(dataStream, _bufferManager);

                            _info.PullMessageResultCount.Add(packet.UnicastMetadatas.Count());

                            foreach (var metadata in packet.UnicastMetadatas)
                            {
                                _metadataManager.SetMetadata(metadata);
                            }
                        }
                        else if (id == (int)SerializeId.MulticastMetadatasRequest)
                        {
                            if (sessionInfo.ReceiveInfo.PullMulticastMetadataRequestSet.Count > _maxMetadataRequestCount * sessionInfo.ReceiveInfo.PullMulticastMetadataRequestSet.SurvivalTime.TotalMinutes * 2) return;

                            var packet = MulticastMetadatasRequestPacket.Import(dataStream, _bufferManager);

                            _info.PullMessageRequestCount.Add(packet.Tags.Count());

                            sessionInfo.ReceiveInfo.PullMulticastMetadataRequestSet.AddRange(packet.Tags);
                        }
                        else if (id == (int)SerializeId.MulticastMetadatasResult)
                        {
                            var packet = MulticastMetadatasResultPacket.Import(dataStream, _bufferManager);

                            _info.PullMessageResultCount.Add(packet.MulticastMetadatas.Count());

                            foreach (var metadata in packet.MulticastMetadatas)
                            {
                                _metadataManager.SetMetadata(metadata);
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        private class SessionInfo
        {

            public Connection Connection { get; set; }
            public SessionType Type { get; set; }
            public string Uri { get; set; }
            public int ThreadId { get; set; }

            public ProtocolVersion Version { get; set; }
            public byte[] Id { get; set; }
            public Location Location { get; set; }

            public PriorityManager PriorityManager { get; private set; } = new PriorityManager(new TimeSpan(0, 10, 0));

            public SendInfo SendInfo { get; private set; } = new SendInfo();
            public ReceiveInfo ReceiveInfo { get; private set; } = new ReceiveInfo();

            public void Update()
            {
                this.PriorityManager.Update();
                this.SendInfo.Update();
                this.ReceiveInfo.Update();
            }
        }

        private class SendInfo
        {
            public bool IsInitialized { get; set; }

            public VolatileHashSet<Hash> PushBlockLinkSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 10, 0));
            public VolatileHashSet<Hash> PushBlockRequestSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 10, 0));
            public VolatileHashSet<Hash> PushBlockResultSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(1, 0, 0));

            public Stopwatch LocationResultStopwatch { get; private set; } = Stopwatch.StartNew();
            public Stopwatch BlockResultStopwatch { get; private set; } = Stopwatch.StartNew();
            public Stopwatch BroadcastMetadataResultStopwatch { get; private set; } = Stopwatch.StartNew();
            public Stopwatch UnicastMetadataResultStopwatch { get; private set; } = Stopwatch.StartNew();
            public Stopwatch MulticastMetadataResultStopwatch { get; private set; } = Stopwatch.StartNew();

            public LockedList<Hash> PushBlockLinkQueue { get; private set; } = new LockedList<Hash>();
            public LockedList<Hash> PushBlockRequestQueue { get; private set; } = new LockedList<Hash>();
            public LockedQueue<Hash> PushBlockResultQueue { get; private set; } = new LockedQueue<Hash>();

            public LockedList<Signature> PushBroadcastMetadataRequestQueue { get; private set; } = new LockedList<Signature>();
            public LockedList<Signature> PushUnicastMetadataRequestQueue { get; private set; } = new LockedList<Signature>();
            public LockedList<Tag> PushMulticastMetadataRequestQueue { get; private set; } = new LockedList<Tag>();

            public void Update()
            {
                this.PushBlockLinkSet.Update();
                this.PushBlockRequestSet.Update();
                this.PushBlockResultSet.Update();
            }
        }

        private class ReceiveInfo
        {
            public bool IsInitialized { get; set; }

            public Stopwatch Stopwatch { get; private set; } = Stopwatch.StartNew();

            public VolatileHashSet<Location> PullLocationSet { get; private set; } = new VolatileHashSet<Location>(new TimeSpan(0, 10, 0));

            public VolatileHashSet<Hash> PullBlockLinkSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 20, 0));
            public VolatileHashSet<Hash> PullBlockRequestSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 20, 0));

            public VolatileHashSet<Signature> PullBroadcastMetadataRequestSet { get; private set; } = new VolatileHashSet<Signature>(new TimeSpan(0, 20, 0));
            public VolatileHashSet<Signature> PullUnicastMetadataRequestSet { get; private set; } = new VolatileHashSet<Signature>(new TimeSpan(0, 20, 0));
            public VolatileHashSet<Tag> PullMulticastMetadataRequestSet { get; private set; } = new VolatileHashSet<Tag>(new TimeSpan(0, 20, 0));

            public void Update()
            {
                this.PullLocationSet.Update();

                this.PullBlockLinkSet.Update();
                this.PullBlockRequestSet.Update();

                this.PullBroadcastMetadataRequestSet.Update();
                this.PullUnicastMetadataRequestSet.Update();
                this.PullMulticastMetadataRequestSet.Update();
            }
        }

        public void Download(Hash hash, DiffusionPriority diffusionPriority)
        {
            lock (_lockObject)
            {
                _pushBlocksRequestMap.Add(hash, diffusionPriority);
            }
        }

        public void Upload(BroadcastMetadata metadata)
        {
            lock (_lockObject)
            {
                _metadataManager.SetMetadata(metadata);
            }
        }

        public void Upload(UnicastMetadata metadata)
        {
            lock (_lockObject)
            {
                _metadataManager.SetMetadata(metadata);
            }
        }

        public void Upload(MulticastMetadata metadata)
        {
            lock (_lockObject)
            {
                _metadataManager.SetMetadata(metadata);
            }
        }

        public BroadcastMetadata GetBroadcastMetadata(Signature signature, string type)
        {
            lock (_lockObject)
            {
                _pushBroadcastMetadatasRequestSet.Add(signature);

                return _metadataManager.GetBroadcastMetadata(signature, type);
            }
        }

        public IEnumerable<UnicastMetadata> GetUnicastMetadatas(Signature signature, string type)
        {
            lock (_lockObject)
            {
                _pushUnicastMetadatasRequestSet.Add(signature);

                return _metadataManager.GetUnicastMetadatas(signature, type);
            }
        }

        public IEnumerable<MulticastMetadata> GetMulticastMetadatas(Tag tag, string type)
        {
            lock (_lockObject)
            {
                _pushMulticastMetadatasRequestSet.Add(tag);

                return _metadataManager.GetMulticastMetadatas(tag, type);
            }
        }

        public override ManagerState State
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

                return _state;
            }
        }

        private readonly object _stateLockObject = new object();

        public override void Start()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

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
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

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
                this.SetMyLocation(_settings.Load<Location>("MyLocation", () => new Location(null)));
                this.SetCloudLocations(_settings.Load<IEnumerable<Location>>("CloudLocations", () => new List<Location>()));

                // MetadataManager
                {
                    foreach (var metadata in _settings.Load("BroadcastMetadatas", () => new BroadcastMetadata[0]))
                    {
                        _metadataManager.SetMetadata(metadata);
                    }

                    foreach (var metadata in _settings.Load("UnicastMetadatas", () => new UnicastMetadata[0]))
                    {
                        _metadataManager.SetMetadata(metadata);
                    }

                    foreach (var metadata in _settings.Load("MulticastMetadatas", () => new MulticastMetadata[0]))
                    {
                        _metadataManager.SetMetadata(metadata);
                    }
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
            }
        }

#endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
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
