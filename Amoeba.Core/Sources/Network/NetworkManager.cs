using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Io;
using Omnius.Messaging;
using Amoeba.Core.Cache;
using Omnius.Security;
using Omnius.Serialization;
using Omnius.Utilities;
using Omnius.Net;

namespace Amoeba.Core.Network
{
    public delegate IEnumerable<Signature> GetSignaturesEventHandler(object sender);

    public delegate Cap ConnectCapEventHandler(object sender, string uri);
    public delegate Cap AcceptCapEventHandler(object sender);

    class NetworkManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CacheManager _cacheManager;

        private Settings _settings;

        private Random _random = new Random();

        private volatile byte[] _myId;

        private volatile Location _myLocation;

        private RouteTable<SessionInfo> _routeTable;
        private LockedList<Location> _crowdLocations = new LockedList<Location>();

        private int _connectionCountLimit;
        private int _bandwidthLimit;

        private LockedHashDictionary<Connection, SessionInfo> _connections = new LockedHashDictionary<Connection, SessionInfo>();

        private MessageManager _messageManager;

        private Thread _computeThread;
        private Thread _sendConnectionsThread;
        private Thread _receiveConnectionsThread;
        private List<Thread> _connectThreads = new List<Thread>();
        private List<Thread> _acceptThreads = new List<Thread>();

        private VolatileHashSet<Hash> _pushBlocksRequestSet;

        private VolatileHashSet<Signature> _pushBroadcastMessagesRequestSet;
        private VolatileHashSet<Signature> _pushUnicastMessagesRequestSet;

        private SafeInteger _receivedByteCount = new SafeInteger();
        private SafeInteger _sentByteCount = new SafeInteger();

        private ManagerState _state = ManagerState.Stop;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        private const int _maxLocationCount = 256;
        private const int _maxBlockLinkCount = 8192;
        private const int _maxBlockRequestCount = 8192;
        private const int _maxMetadataRequestCount = 1024;
        private const int _maxMetadataResultCount = 1024;

        public NetworkManager(string configPath, CacheManager cacheManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _cacheManager = cacheManager;

            _settings = new Settings(configPath);

            _routeTable = new RouteTable<SessionInfo>(256, 20);

            _messageManager = new MessageManager();
            _messageManager.GetLockSignaturesEvent += (_) => this.OnGetLockSignaturesEvent();
        }

        public Location MyLocation
        {
            get
            {
                lock (_thisLock)
                {
                    return _myLocation;
                }
            }
        }

        public IEnumerable<Location> CrowdLocations
        {
            get
            {
                lock (_thisLock)
                {
                    return CollectionUtils.Unite(_crowdLocations, _routeTable.ToArray().Select(n => n.Value.Location)).ToArray();
                }
            }
        }

        public int ConnectionCountLimit
        {
            get
            {
                lock (_thisLock)
                {
                    return _connectionCountLimit;
                }
            }
            set
            {
                lock (_thisLock)
                {
                    _connectionCountLimit = value;
                }
            }
        }

        public int BandwidthLimit
        {
            get
            {
                lock (_thisLock)
                {
                    return _bandwidthLimit;
                }
            }
            set
            {
                lock (_thisLock)
                {
                    _bandwidthLimit = value;
                }
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

        public Information Information
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

                lock (_thisLock)
                {
                    var contexts = new List<InformationContext>();

                    // Info
                    {
                        Type type = typeof(Info);

                        foreach (var property in type.GetProperties())
                        {
                            string name = property.Name;
                            object value = property.GetValue(_info);

                            if (value is SafeInteger) value = (long)value;

                            contexts.Add(new InformationContext(name, value));
                        }
                    }

                    contexts.Add(new InformationContext("CrowdNodeCount", _routeTable.Count));
                    contexts.Add(new InformationContext("MessageCount", _messageManager.Count));

                    return new Information(contexts);
                }
            }
        }

        public IEnumerable<Information> ConnectionInformation
        {
            get
            {
                lock (_thisLock)
                {
                    var list = new List<Information>();

                    foreach (var sessionInfo in _routeTable.ToArray().Select(n => n.Value))
                    {
                        var contexts = new List<InformationContext>();

                        contexts.Add(new InformationContext("Version", sessionInfo.Version));
                        contexts.Add(new InformationContext("Id", sessionInfo.Id));
                        contexts.Add(new InformationContext("Location", sessionInfo.Location));
                        contexts.Add(new InformationContext("ReceivedByteCount", sessionInfo.Connection.ReceivedByteCount));
                        contexts.Add(new InformationContext("SentByteCount", sessionInfo.Connection.SentByteCount));

                        list.Add(new Information(contexts));
                    }

                    return list;
                }
            }
        }

        public long ReceivedByteCount
        {
            get
            {
                lock (_thisLock)
                {
                    return _receivedByteCount;
                }
            }
        }

        public long SentByteCount
        {
            get
            {
                lock (_thisLock)
                {
                    return _sentByteCount;
                }
            }
        }

        public ConnectCapEventHandler ConnectCapEvent { get; set; }
        public AcceptCapEventHandler AcceptCapEvent { get; set; }

        private Cap OnConnectCap(string uri)
        {
            return this.ConnectCapEvent?.Invoke(this, uri);
        }

        private Cap OnAcceptCap()
        {
            return this.AcceptCapEvent?.Invoke(this);
        }

        public GetSignaturesEventHandler GetLockSignaturesEvent { get; set; }

        private IEnumerable<Signature> OnGetLockSignaturesEvent()
        {
            return this.GetLockSignaturesEvent?.Invoke(this) ?? new Signature[0];
        }

        private void UpdateMyId()
        {
            lock (_thisLock)
            {
                _myId = new byte[32];

                using (var random = RandomNumberGenerator.Create())
                {
                    random.GetBytes(_myId);
                }
            }
        }

        private void ComputeThread()
        {
            var refreshStopwatch = Stopwatch.StartNew();
            var reduceStopwatch = Stopwatch.StartNew();

            var pushBlockUploadStopwatch = Stopwatch.StartNew();
            var pushBlockDownloadStopwatch = Stopwatch.StartNew();

            var pushMetadataUploadStopwatch = Stopwatch.StartNew();
            var pushMetadataDownloadStopwatch = Stopwatch.StartNew();

            try
            {
                for (;;)
                {
                    Thread.Sleep(1000);
                    if (this.State == ManagerState.Stop) return;

                    if (refreshStopwatch.Elapsed.TotalSeconds >= 30)
                    {
                        refreshStopwatch.Restart();

                        // トラストにより必要なMetadataを選択し、不要なMetadataを削除する。
                        {
                            _messageManager.Refresh();
                        }

                        // 古いセッション情報を破棄する。
                        {
                            foreach (var sessionInfo in _routeTable.ToArray().Select(n => n.Value))
                            {
                                sessionInfo.Update();
                            }
                        }

                        // 長い間接続の無い通信を切断する。
                        {
                            foreach (var sessionInfo in _routeTable.ToArray().Select(n => n.Value))
                            {
                                if (sessionInfo.ReceiveInfo.Stopwatch.Elapsed.TotalMinutes > 2)
                                {
                                    this.RemoveConnection(sessionInfo.Connection);
                                }
                            }
                        }
                    }

                    if (reduceStopwatch.Elapsed.TotalMinutes >= 3)
                    {
                        reduceStopwatch.Restart();

                        // 優先度の低い通信を切断する。
                        {
                            var list = _routeTable.ToArray().Select(n => n.Value).ToList();
                            list.Sort((x, y) => x.PriorityManager.GetPriority().CompareTo(y.PriorityManager.GetPriority()));

                            foreach (var sessionInfo in list.Where(n => (n.PriorityManager.GetPriority() * 100) < 25).Take(2))
                            {
                                this.RemoveConnection(sessionInfo.Connection);
                            }
                        }
                    }

                    // アップロード
                    if (_routeTable.Count >= 3
                        && pushBlockUploadStopwatch.Elapsed.TotalSeconds >= 60)
                    {
                        pushBlockUploadStopwatch.Restart();

                        foreach (var node in _routeTable.ToArray())
                        {
                            var tempList = _cacheManager.IntersectFrom(node.Value.ReceiveInfo.PullBlockRequestSet.ToArray().Randomize()).Take(1024).ToList();

                            lock (node.Value.SendInfo.PushBlockResultQueue.ThisLock)
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
                        && pushBlockDownloadStopwatch.Elapsed.TotalSeconds >= 60)
                    {
                        pushBlockDownloadStopwatch.Restart();

                        var pushBlockLinkSet = new HashSet<Hash>();
                        var pushBlockRequestSet = new HashSet<Hash>();

                        {
                            // Link
                            {
                                {
                                    var list = _cacheManager.ToArray();
                                    _random.Shuffle(list);

                                    pushBlockLinkSet.UnionWith(list.Take(_maxBlockLinkCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = info.ReceiveInfo.PullBlockLinkSet.ToArray(new TimeSpan(0, 10, 0));
                                    _random.Shuffle(list);

                                    pushBlockLinkSet.UnionWith(list.Take(_maxBlockLinkCount));
                                }
                            }

                            // Request
                            {
                                {
                                    var list = _cacheManager.ExceptFrom(_pushBlocksRequestSet.ToArray()).ToArray();
                                    _random.Shuffle(list);

                                    pushBlockRequestSet.UnionWith(list.Take(_maxBlockRequestCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = _cacheManager.ExceptFrom(info.ReceiveInfo.PullBlockRequestSet.ToArray(new TimeSpan(0, 10, 0))).ToArray();
                                    _random.Shuffle(list);

                                    pushBlockRequestSet.UnionWith(list.Take((int)(_maxBlockRequestCount * info.PriorityManager.GetPriority())));
                                }
                            }
                        }

                        {
                            var crowdNodes = _routeTable.ToArray();

                            // Link
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Hash>>();

                                foreach (var hash in pushBlockLinkSet.Randomize())
                                {
                                    foreach (var node in RouteTable<SessionInfo>.Search(hash.Value, _myId, crowdNodes, 2))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Hash>()).Add(hash);
                                    }
                                }

                                foreach (var pair in tempMap)
                                {
                                    var node = pair.Key;
                                    var targets = pair.Value;

                                    _random.Shuffle(targets);

                                    lock (node.Value.SendInfo.PushBlockLinkQueue.ThisLock)
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
                                    foreach (var node in RouteTable<SessionInfo>.Search(hash.Value, _myId, crowdNodes, 2))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new HashSet<Hash>()).Add(hash);
                                    }

                                    foreach (var node in crowdNodes.Where(n => n.Value.ReceiveInfo.PullBlockLinkSet.Contains(hash)))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new HashSet<Hash>()).Add(hash);
                                    }
                                }

                                foreach (var pair in tempMap)
                                {
                                    var node = pair.Key;
                                    var targets = pair.Value;

                                    lock (node.Value.SendInfo.PushBlockRequestQueue.ThisLock)
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
                        && pushMetadataUploadStopwatch.Elapsed.TotalMinutes >= 3)
                    {
                        pushMetadataUploadStopwatch.Restart();

                        // BroadcastMessage
                        foreach (var signature in _messageManager.GetBroadcastSignatures())
                        {
                            foreach (var node in _routeTable.Search(signature.Id, 2))
                            {
                                node.Value.ReceiveInfo.PullBroadcastMessageRequestSet.Add(signature);
                            }
                        }

                        // UnicastMessage
                        foreach (var signature in _messageManager.GetUnicastSignatures())
                        {
                            foreach (var node in _routeTable.Search(signature.Id, 2))
                            {
                                node.Value.ReceiveInfo.PullUnicastMessageRequestSet.Add(signature);
                            }
                        }
                    }

                    // ダウンロード
                    if (_routeTable.Count >= 3
                        && pushMetadataDownloadStopwatch.Elapsed.TotalSeconds >= 60)
                    {
                        pushMetadataDownloadStopwatch.Restart();

                        var pushBroadcastMessagesRequestSet = new HashSet<Signature>();
                        var pushUnicastMessagesRequestSet = new HashSet<Signature>();

                        {
                            // BroadcastMessage
                            {
                                {
                                    var list = _pushBroadcastMessagesRequestSet.ToArray();
                                    _random.Shuffle(list);

                                    pushBroadcastMessagesRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = info.ReceiveInfo.PullBroadcastMessageRequestSet.ToArray(new TimeSpan(0, 10, 0));
                                    _random.Shuffle(list);

                                    pushBroadcastMessagesRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }
                            }

                            // UnicastMessage
                            {
                                {
                                    var list = _pushUnicastMessagesRequestSet.ToArray();
                                    _random.Shuffle(list);

                                    pushUnicastMessagesRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }

                                foreach (var info in _routeTable.ToArray().Select(n => n.Value))
                                {
                                    var list = info.ReceiveInfo.PullUnicastMessageRequestSet.ToArray(new TimeSpan(0, 10, 0));
                                    _random.Shuffle(list);

                                    pushUnicastMessagesRequestSet.UnionWith(list.Take(_maxMetadataRequestCount));
                                }
                            }
                        }

                        {
                            var crowdNodes = _routeTable.ToArray();

                            // BroadcastMessage
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Signature>>();

                                foreach (var signature in pushBroadcastMessagesRequestSet.Randomize())
                                {
                                    foreach (var node in RouteTable<SessionInfo>.Search(signature.Id, _myId, crowdNodes, 2))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Signature>()).Add(signature);
                                    }
                                }

                                foreach (var pair in tempMap)
                                {
                                    var node = pair.Key;
                                    var targets = pair.Value;

                                    _random.Shuffle(targets);

                                    lock (node.Value.SendInfo.PushBroadcastMessageRequestQueue.ThisLock)
                                    {
                                        node.Value.SendInfo.PushBroadcastMessageRequestQueue.Clear();
                                        node.Value.SendInfo.PushBroadcastMessageRequestQueue.AddRange(targets.Take(_maxMetadataRequestCount));
                                    }
                                }
                            }

                            // UnicastMessage
                            {
                                var tempMap = new Dictionary<Node<SessionInfo>, List<Signature>>();

                                foreach (var signature in pushUnicastMessagesRequestSet.Randomize())
                                {
                                    foreach (var node in RouteTable<SessionInfo>.Search(signature.Id, _myId, crowdNodes, 2))
                                    {
                                        tempMap.GetOrAdd(node, (_) => new List<Signature>()).Add(signature);
                                    }
                                }

                                foreach (var pair in tempMap)
                                {
                                    var node = pair.Key;
                                    var targets = pair.Value;

                                    _random.Shuffle(targets);

                                    lock (node.Value.SendInfo.PushUnicastMessageRequestQueue.ThisLock)
                                    {
                                        node.Value.SendInfo.PushUnicastMessageRequestQueue.Clear();
                                        node.Value.SendInfo.PushUnicastMessageRequestQueue.AddRange(targets.Take(_maxMetadataRequestCount));
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

        private void ConnectThread()
        {
            try
            {
                for (;;)
                {
                    Thread.Sleep(1000);
                    if (this.State == ManagerState.Stop) return;

                    // 接続数を制限する。
                    {
                        int connectionCount = _routeTable.ToArray().Select(n => n.Value).Count(n => n.Type == SessionType.Connect);
                        if (connectionCount >= (this.ConnectionCountLimit / 2)) continue;
                    }

                    Location location = null;

                    for (;;)
                    {
                        switch (_random.Next(0, 2))
                        {
                            case 0:
                                location = _crowdLocations.Randomize().FirstOrDefault();
                                break;
                            case 1:
                                var sessionInfo = _routeTable.ToArray().Randomize().Select(n => n.Value).FirstOrDefault();
                                if (sessionInfo == null) break;

                                location = sessionInfo.ReceiveInfo.PullLocationSet.Randomize().FirstOrDefault();
                                break;
                        }

                        if (location != null) break;
                    }

                    Cap cap = null;

                    foreach (var uri in new HashSet<string>(location.Uris))
                    {
                        cap = this.OnConnectCap(uri);
                        if (cap != null) break;
                    }

                    if (cap == null)
                    {
                        if (_crowdLocations.Count > 1024) _crowdLocations.Remove(location);

                        continue;
                    }

                    _info.ConnectCount.Increment();

                    this.CreateConnection(cap, SessionType.Connect);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void AcceptThread()
        {
            try
            {
                for (;;)
                {
                    Thread.Sleep(1000);
                    if (this.State == ManagerState.Stop) return;

                    // 接続数を制限する。
                    {
                        int connectionCount = _routeTable.ToArray().Select(n => n.Value).Count(n => n.Type == SessionType.Accept);
                        if (connectionCount >= (this.ConnectionCountLimit / 2)) continue;
                    }

                    var cap = this.OnAcceptCap();
                    if (cap == null) continue;

                    _info.AcceptCount.Increment();

                    this.CreateConnection(cap, SessionType.Accept);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void CreateConnection(Cap cap, SessionType type)
        {
            lock (_thisLock)
            {
                var connection = new Connection(1024 * 1024 * 8, _bufferManager);
                connection.Connect(cap);

                var sessionInfo = new SessionInfo();
                sessionInfo.Connection = connection;
                sessionInfo.Type = type;

                connection.SendEvent = (_) => this.Send(sessionInfo);
                connection.ReceiveEvent = (_, stream) => this.Receive(sessionInfo, stream);

                _connections.Add(connection, sessionInfo);
            }
        }

        private void RemoveConnection(Connection connection)
        {
            lock (_thisLock)
            {
                SessionInfo sessionInfo;

                if (_connections.TryGetValue(connection, out sessionInfo))
                {
                    _connections.Remove(connection);

                    connection.Dispose();

                    if (sessionInfo.Id != null)
                    {
                        _routeTable.Remove(sessionInfo.Id);
                    }

                    _crowdLocations.Add(sessionInfo.Location);

                    return;
                }

                throw new KeyNotFoundException();
            }
        }

        private void SendConnectionsThread()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                for (;;)
                {
                    // Timer
                    {
                        Thread.Sleep((int)(1000 - sw.ElapsedMilliseconds));
                        sw.Restart();
                    }

                    if (this.State == ManagerState.Stop) return;

                    // Send
                    {
                        var remain = _bandwidthLimit;

                        foreach (var connection in _connections.Keys.ToArray().Randomize())
                        {
                            try
                            {
                                int count = connection.Send(Math.Min(remain, 1024 * 1024));
                                _sentByteCount.Add(count);

                                remain -= count;
                                if (remain == 0) break;
                            }
                            catch (Exception)
                            {
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

        private void ReceiveConnectionsThread()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                for (;;)
                {
                    // Timer
                    {
                        Thread.Sleep((int)(1000 - sw.ElapsedMilliseconds));
                        sw.Restart();
                    }

                    if (this.State == ManagerState.Stop) return;

                    // Receive
                    {
                        var remain = _bandwidthLimit;

                        foreach (var connection in _connections.Keys.ToArray().Randomize())
                        {
                            try
                            {
                                int count = connection.Receive(Math.Min(remain, 1024 * 1024));
                                _receivedByteCount.Add(count);

                                remain -= count;
                                if (remain == 0) break;
                            }
                            catch (Exception)
                            {
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

        private enum SerializeId
        {
            Locations = 0,

            BlocksLink = 1,
            BlocksRequest = 2,
            BlockResult = 3,

            BroadcastMessagesRequest = 4,
            BroadcastMessagesResult = 5,

            UnicastMessagesRequest = 6,
            UnicastMessagesResult = 7,
        }

        private Stream Send(SessionInfo sessionInfo)
        {
            // Init
            if (!sessionInfo.SendInfo.IsInitialized)
            {
                sessionInfo.SendInfo.IsInitialized = true;

                Stream versionStream = new BufferStream(_bufferManager);
                VintUtils.Write(versionStream, 0);

                var dataStream = (new ProfilePacket(_myId, _myLocation)).Export(_bufferManager);

                return new UniteStream(versionStream, dataStream);
            }

            // Locations
            if (sessionInfo.SendInfo.LocationStopwatch.Elapsed.TotalMinutes > 3)
            {
                sessionInfo.SendInfo.LocationStopwatch.Restart();

                var locations = new List<Location>();
                locations.AddRange(_routeTable.ToArray().Select(n => n.Value.Location));
                locations.AddRange(_crowdLocations);
                _random.Shuffle(locations);

                var packet = new LocationsPacket(locations);

                Stream typeStream = new BufferStream(_bufferManager);
                VintUtils.Write(typeStream, (int)SerializeId.Locations);

                _info.PushLocationCount.Add(packet.Locations.Count());

                return new UniteStream(typeStream, packet.Export(_bufferManager));
            }

            // BlocksLink
            if (sessionInfo.SendInfo.PushBlockLinkQueue.Count > 0)
            {
                var packet = new BlocksLinkPacket(sessionInfo.SendInfo.PushBlockLinkQueue);
                sessionInfo.SendInfo.PushBlockLinkQueue.Clear();

                Stream typeStream = new BufferStream(_bufferManager);
                VintUtils.Write(typeStream, (int)SerializeId.BlocksLink);

                _info.PushBlockLinkCount.Add(packet.Hashes.Count());

                return new UniteStream(typeStream, packet.Export(_bufferManager));
            }

            // BlocksRequest
            if (sessionInfo.SendInfo.PushBlockRequestQueue.Count > 0)
            {
                var packet = new BlocksRequestPacket(sessionInfo.SendInfo.PushBlockRequestQueue);
                sessionInfo.SendInfo.PushBlockRequestQueue.Clear();

                Stream typeStream = new BufferStream(_bufferManager);
                VintUtils.Write(typeStream, (int)SerializeId.BlocksRequest);

                _info.PushBlockRequestCount.Add(packet.Hashes.Count());

                return new UniteStream(typeStream, packet.Export(_bufferManager));
            }

            // BlockResult
            if (sessionInfo.SendInfo.BlockStopwatch.Elapsed.TotalSeconds > 1)
            {
                sessionInfo.SendInfo.BlockStopwatch.Restart();

                if (_random.Next(0, 100) < (sessionInfo.PriorityManager.GetPriority() * 100))
                {
                    Hash hash = null;

                    lock (sessionInfo.SendInfo.PushBlockResultQueue.ThisLock)
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
                        VintUtils.Write(typeStream, (int)SerializeId.BlockResult);

                        _info.PushBlockResultCount.Increment();

                        return new UniteStream(typeStream, dataStream);
                    }
                }
            }

            // BroadcastMessagesRequest
            if (sessionInfo.SendInfo.PushBroadcastMessageRequestQueue.Count > 0)
            {
                var packet = new BroadcastMessagesRequestPacket(sessionInfo.SendInfo.PushBroadcastMessageRequestQueue);
                sessionInfo.SendInfo.PushBroadcastMessageRequestQueue.Clear();

                Stream typeStream = new BufferStream(_bufferManager);
                VintUtils.Write(typeStream, (int)SerializeId.BroadcastMessagesRequest);

                _info.PushMessageRequestCount.Add(packet.Signatures.Count());

                return new UniteStream(typeStream, packet.Export(_bufferManager));
            }

            // BroadcastMessagesResult
            if (sessionInfo.SendInfo.BroadcastMessageStopwatch.Elapsed.TotalSeconds > 60)
            {
                sessionInfo.SendInfo.BroadcastMessageStopwatch.Restart();

                var broadcastMessages = new List<BroadcastMessage>();

                var signatures = sessionInfo.ReceiveInfo.PullBroadcastMessageRequestSet.ToList();
                _random.Shuffle(signatures);

                foreach (var signature in signatures)
                {
                    foreach (var metadata in _messageManager.GetBroadcastMessages(signature).Randomize())
                    {
                        broadcastMessages.Add(metadata);

                        if (broadcastMessages.Count >= _maxMetadataResultCount) goto End;
                    }
                }

                End:;

                if (broadcastMessages.Count > 0)
                {
                    var packet = new BroadcastMessagesResultPacket(broadcastMessages);
                    broadcastMessages.Clear();

                    Stream typeStream = new BufferStream(_bufferManager);
                    VintUtils.Write(typeStream, (int)SerializeId.BroadcastMessagesResult);

                    _info.PushMessageResultCount.Add(packet.BroadcastMessages.Count());

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }
            }

            // UnicastMessagesRequest
            if (sessionInfo.SendInfo.PushUnicastMessageRequestQueue.Count > 0)
            {
                var packet = new UnicastMessagesRequestPacket(sessionInfo.SendInfo.PushUnicastMessageRequestQueue);
                sessionInfo.SendInfo.PushUnicastMessageRequestQueue.Clear();

                Stream typeStream = new BufferStream(_bufferManager);
                VintUtils.Write(typeStream, (int)SerializeId.UnicastMessagesRequest);

                _info.PushMessageRequestCount.Add(packet.Signatures.Count());

                return new UniteStream(typeStream, packet.Export(_bufferManager));
            }

            // UnicastMessagesResult
            if (sessionInfo.SendInfo.UnicastMessageStopwatch.Elapsed.TotalSeconds > 60)
            {
                sessionInfo.SendInfo.UnicastMessageStopwatch.Restart();

                var unicastMessages = new List<UnicastMessage>();

                var signatures = sessionInfo.ReceiveInfo.PullUnicastMessageRequestSet.ToList();
                _random.Shuffle(signatures);

                foreach (var signature in signatures)
                {
                    foreach (var metadata in _messageManager.GetUnicastMessages(signature).Randomize())
                    {
                        unicastMessages.Add(metadata);

                        if (unicastMessages.Count >= _maxMetadataResultCount) goto End;
                    }
                }

                End:;

                if (unicastMessages.Count > 0)
                {
                    var packet = new UnicastMessagesResultPacket(unicastMessages);
                    unicastMessages.Clear();

                    Stream typeStream = new BufferStream(_bufferManager);
                    VintUtils.Write(typeStream, (int)SerializeId.UnicastMessagesResult);

                    _info.PushMessageResultCount.Add(packet.UnicastMessages.Count());

                    return new UniteStream(typeStream, packet.Export(_bufferManager));
                }
            }

            return null;
        }

        private void Receive(SessionInfo sessionInfo, Stream stream)
        {
            if (!sessionInfo.ReceiveInfo.IsInitialized)
            {
                sessionInfo.ReceiveInfo.IsInitialized = true;

                sessionInfo.Version = (int)VintUtils.Get(stream);

                using (var dataStream = new RangeStream(stream))
                {
                    if (sessionInfo.Version == 0)
                    {
                        var profile = ProfilePacket.Import(dataStream, _bufferManager);

                        sessionInfo.Id = profile.Id;
                        sessionInfo.Location = profile.Location;

                        lock (_thisLock)
                        {
                            if (_connections.ContainsKey(sessionInfo.Connection))
                            {
                                if (!_routeTable.Add(sessionInfo.Id, sessionInfo))
                                {
                                    throw new ArgumentException("RouteTable Overflow");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else
            {
                sessionInfo.ReceiveInfo.Stopwatch.Restart();

                int id = (int)VintUtils.Get(stream);

                using (var dataStream = new RangeStream(stream))
                {
                    if (id == (int)SerializeId.Locations)
                    {
                        if (sessionInfo.ReceiveInfo.PullLocationSet.Count > _maxLocationCount * sessionInfo.ReceiveInfo.PullLocationSet.SurvivalTime.TotalMinutes) return;

                        var packet = LocationsPacket.Import(dataStream, _bufferManager);

                        _info.PullLocationCount.Add(packet.Locations.Count());

                        sessionInfo.ReceiveInfo.PullLocationSet.AddRange(packet.Locations);
                    }
                    else if (id == (int)SerializeId.BlocksLink)
                    {
                        if (sessionInfo.ReceiveInfo.PullBlockLinkSet.Count > _maxBlockLinkCount * sessionInfo.ReceiveInfo.PullBlockLinkSet.SurvivalTime.TotalMinutes) return;

                        var packet = BlocksLinkPacket.Import(dataStream, _bufferManager);

                        _info.PullBlockLinkCount.Add(packet.Hashes.Count());

                        sessionInfo.ReceiveInfo.PullBlockLinkSet.AddRange(packet.Hashes);
                    }
                    else if (id == (int)SerializeId.BlocksRequest)
                    {
                        if (sessionInfo.ReceiveInfo.PullBlockRequestSet.Count > _maxBlockRequestCount * sessionInfo.ReceiveInfo.PullBlockRequestSet.SurvivalTime.TotalMinutes) return;

                        var packet = BlocksRequestPacket.Import(dataStream, _bufferManager);

                        _info.PullBlockRequestCount.Add(packet.Hashes.Count());

                        sessionInfo.ReceiveInfo.PullBlockRequestSet.AddRange(packet.Hashes);
                    }
                    else if (id == (int)SerializeId.BlockResult)
                    {
                        var packet = BlockResultPacket.Import(dataStream, _bufferManager);

                        _info.PullBlockResultCount.Increment();

                        try
                        {
                            _cacheManager[packet.Hash] = packet.Value;

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
                    else if (id == (int)SerializeId.BroadcastMessagesRequest)
                    {
                        if (sessionInfo.ReceiveInfo.PullBroadcastMessageRequestSet.Count > _maxMetadataRequestCount * sessionInfo.ReceiveInfo.PullBroadcastMessageRequestSet.SurvivalTime.TotalMinutes) return;

                        var packet = BroadcastMessagesRequestPacket.Import(dataStream, _bufferManager);

                        _info.PullMessageRequestCount.Add(packet.Signatures.Count());

                        sessionInfo.ReceiveInfo.PullBroadcastMessageRequestSet.AddRange(packet.Signatures);
                    }
                    else if (id == (int)SerializeId.BroadcastMessagesResult)
                    {
                        var packet = BroadcastMessagesResultPacket.Import(dataStream, _bufferManager);

                        _info.PullMessageResultCount.Add(packet.BroadcastMessages.Count());

                        foreach (var metadata in packet.BroadcastMessages)
                        {
                            try
                            {
                                _messageManager.SetMetadata(metadata);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                    else if (id == (int)SerializeId.UnicastMessagesRequest)
                    {
                        if (sessionInfo.ReceiveInfo.PullUnicastMessageRequestSet.Count > _maxMetadataRequestCount * sessionInfo.ReceiveInfo.PullUnicastMessageRequestSet.SurvivalTime.TotalMinutes) return;

                        var packet = UnicastMessagesRequestPacket.Import(dataStream, _bufferManager);

                        _info.PullMessageRequestCount.Add(packet.Signatures.Count());

                        sessionInfo.ReceiveInfo.PullUnicastMessageRequestSet.AddRange(packet.Signatures);
                    }
                    else if (id == (int)SerializeId.UnicastMessagesResult)
                    {
                        var packet = UnicastMessagesResultPacket.Import(dataStream, _bufferManager);

                        _info.PullMessageResultCount.Add(packet.UnicastMessages.Count());

                        foreach (var metadata in packet.UnicastMessages)
                        {
                            try
                            {
                                _messageManager.SetMetadata(metadata);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }
        }

        private enum SessionType
        {
            Connect,
            Accept,
        }

        private class SessionInfo
        {
            public Connection Connection { get; set; }
            public SessionType Type { get; set; }

            public int Version { get; set; }
            public byte[] Id { get; set; }
            public Location Location { get; set; }

            public PriorityManager PriorityManager { get; private set; } = new PriorityManager(new TimeSpan(0, 30, 0));

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

            public VolatileHashSet<Hash> PushBlockRequestSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 30, 0));

            public Stopwatch LocationStopwatch { get; private set; } = Stopwatch.StartNew();
            public Stopwatch BlockStopwatch { get; private set; } = Stopwatch.StartNew();
            public Stopwatch BroadcastMessageStopwatch { get; private set; } = Stopwatch.StartNew();
            public Stopwatch UnicastMessageStopwatch { get; private set; } = Stopwatch.StartNew();

            public LockedQueue<Hash> PushBlockResultQueue { get; private set; } = new LockedQueue<Hash>();

            public LockedList<Hash> PushBlockLinkQueue { get; private set; } = new LockedList<Hash>();
            public LockedList<Hash> PushBlockRequestQueue { get; private set; } = new LockedList<Hash>();

            public LockedList<Signature> PushBroadcastMessageRequestQueue { get; private set; } = new LockedList<Signature>();
            public LockedList<Signature> PushUnicastMessageRequestQueue { get; private set; } = new LockedList<Signature>();

            public void Update()
            {
                this.PushBlockRequestSet.Update();
            }
        }

        private class ReceiveInfo
        {
            public bool IsInitialized { get; set; }

            public Stopwatch Stopwatch { get; private set; } = Stopwatch.StartNew();

            public VolatileHashSet<Location> PullLocationSet { get; private set; } = new VolatileHashSet<Location>(new TimeSpan(0, 10, 0));

            public VolatileHashSet<Hash> PullBlockLinkSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 30, 0));
            public VolatileHashSet<Hash> PullBlockRequestSet { get; private set; } = new VolatileHashSet<Hash>(new TimeSpan(0, 30, 0));

            public VolatileHashSet<Signature> PullBroadcastMessageRequestSet { get; private set; } = new VolatileHashSet<Signature>(new TimeSpan(0, 30, 0));
            public VolatileHashSet<Signature> PullUnicastMessageRequestSet { get; private set; } = new VolatileHashSet<Signature>(new TimeSpan(0, 30, 0));

            public void Update()
            {
                this.PullLocationSet.Update();

                this.PullBlockLinkSet.Update();
                this.PullBlockRequestSet.Update();

                this.PullBroadcastMessageRequestSet.Update();
                this.PullUnicastMessageRequestSet.Update();
            }
        }

        public void SetMyLocation(Location location)
        {
            lock (_thisLock)
            {
                foreach (var connection in _connections.Keys.ToArray())
                {
                    this.RemoveConnection(connection);
                }

                this.UpdateMyId();

                _myLocation = location;
            }
        }

        public void SetCrowdLocations(IEnumerable<Location> locations)
        {
            lock (_thisLock)
            {
                _crowdLocations.AddRange(locations);
            }
        }

        public bool IsDownloadWaiting(Hash hash)
        {
            lock (_thisLock)
            {
                return _pushBlocksRequestSet.Contains(hash);
            }
        }

        public void Download(Hash hash)
        {
            lock (_thisLock)
            {
                _pushBlocksRequestSet.Add(hash);
            }
        }

        public BroadcastMessage GetBroadcastMessage(Signature signature, string type)
        {
            lock (_thisLock)
            {
                _pushBroadcastMessagesRequestSet.Add(signature);

                return _messageManager.GetBroadcastMessage(signature, type);
            }
        }

        public IEnumerable<UnicastMessage> GetUnicastMessages(Signature signature, string type)
        {
            lock (_thisLock)
            {
                _pushUnicastMessagesRequestSet.Add(signature);

                return _messageManager.GetUnicastMessages(signature, type);
            }
        }

        public void Upload(BroadcastMessage message)
        {
            lock (_thisLock)
            {
                _messageManager.SetMetadata(message);
            }
        }

        public void Upload(UnicastMessage message)
        {
            lock (_thisLock)
            {
                _messageManager.SetMetadata(message);
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

        private readonly object _stateLock = new object();

        public override void Start()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    this.UpdateMyId();

                    _computeThread = new Thread(this.ComputeThread);
                    _computeThread.Name = "NetworkManager_ComputeThread";
                    _computeThread.Priority = ThreadPriority.Lowest;
                    _computeThread.Start();

                    for (int i = 0; i < 3; i++)
                    {
                        var thread = new Thread(this.ConnectThread);
                        thread.Name = "NetworkManager_ConnectThread";
                        thread.Priority = ThreadPriority.Lowest;
                        thread.Start();

                        _connectThreads.Add(thread);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        var thread = new Thread(this.AcceptThread);
                        thread.Name = "NetworkManager_AcceptThread";
                        thread.Priority = ThreadPriority.Lowest;
                        thread.Start();

                        _acceptThreads.Add(thread);
                    }

                    _sendConnectionsThread = new Thread(this.SendConnectionsThread);
                    _sendConnectionsThread.Name = "NetworkManager_SendConnectionsThread";
                    _sendConnectionsThread.Priority = ThreadPriority.Lowest;
                    _sendConnectionsThread.Start();

                    _receiveConnectionsThread = new Thread(this.ReceiveConnectionsThread);
                    _receiveConnectionsThread.Name = "NetworkManager_ReceiveConnectionsThread";
                    _receiveConnectionsThread.Priority = ThreadPriority.Lowest;
                    _receiveConnectionsThread.Start();
                }
            }
        }

        public override void Stop()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                _computeThread.Join();
                _computeThread = null;

                foreach (var thread in _connectThreads)
                {
                    thread.Join();
                }
                _connectThreads.Clear();

                foreach (var thread in _acceptThreads)
                {
                    thread.Join();
                }
                _acceptThreads.Clear();

                _sendConnectionsThread.Join();
                _sendConnectionsThread = null;

                _receiveConnectionsThread.Join();
                _receiveConnectionsThread = null;
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_thisLock)
            {
                int version = _settings.Load("Version", () => 0);

                _myLocation = _settings.Load<Location>("MyLocation");
                _crowdLocations.AddRange(_settings.Load<IEnumerable<Location>>("CrowdLocations", () => new List<Location>()));
                _connectionCountLimit = _settings.Load<int>("ConnectionCountLimit", () => 256);
                _bandwidthLimit = _settings.Load<int>("BandwidthLimit", () => 1024 * 1024 * 2);

                // MetadataManager
                {
                    foreach (var metadata in _settings.Load("BroadcastMessages", () => new BroadcastMessage[0]))
                    {
                        try
                        {
                            _messageManager.SetMetadata(metadata);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    foreach (var metadata in _settings.Load("UnicastMessages", () => new UnicastMessage[0]))
                    {
                        try
                        {
                            _messageManager.SetMetadata(metadata);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
        }

        public void Save()
        {
            lock (_thisLock)
            {
                _settings.Save("Version", 0);

                _settings.Save("MyLocation", _myLocation);
                _settings.Save("CrowdLocations", CollectionUtils.Unite(_crowdLocations, _routeTable.ToArray().Select(n => n.Value.Location)));
                _settings.Save("ConnectionCountLimit", _connectionCountLimit);
                _settings.Save("BandwidthLimit", _bandwidthLimit);

                {
                    _settings.Save("BroadcastMessages", _messageManager.GetBroadcastMessages());
                    _settings.Save("UnicastMessages", _messageManager.GetUnicastMessages());
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

            }
        }
    }

    [Serializable]
    class NetworkManagerException : ManagerException
    {
        public NetworkManagerException() : base() { }
        public NetworkManagerException(string message) : base(message) { }
        public NetworkManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
