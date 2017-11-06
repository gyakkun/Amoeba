using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Omnius.Base;
using Omnius.Security;

namespace Amoeba.Messages
{
    [DataContract(Name = nameof(CheckBlocksProgressReport))]
    public sealed class CheckBlocksProgressReport
    {
        private long _badCount;
        private long _checkedCount;
        private long _blockCount;

        public CheckBlocksProgressReport(long badCount, long checkedCount, long blockCount)
        { 
            this.BadCount = badCount;
            this.CheckedCount = checkedCount;
            this.BlockCount = blockCount;
        }

        [DataMember(Name = nameof(BadCount))]
        public long BadCount 
        { 
            get
            {
                return _badCount;
            }
            private set
            {
                _badCount = value;
            }
        }

        [DataMember(Name = nameof(CheckedCount))]
        public long CheckedCount 
        { 
            get
            {
                return _checkedCount;
            }
            private set
            {
                _checkedCount = value;
            }
        }

        [DataMember(Name = nameof(BlockCount))]
        public long BlockCount 
        { 
            get
            {
                return _blockCount;
            }
            private set
            {
                _blockCount = value;
            }
        }
    }

    [DataContract(Name = nameof(ServiceReport))]
    public sealed class ServiceReport
    {
        private CoreReport _core;
        private ConnectionReport _connection;

        public ServiceReport(CoreReport core, ConnectionReport connection)
        { 
            this.Core = core;
            this.Connection = connection;
        }

        [DataMember(Name = nameof(Core))]
        public CoreReport Core 
        { 
            get
            {
                return _core;
            }
            private set
            {
                _core = value;
            }
        }

        [DataMember(Name = nameof(Connection))]
        public ConnectionReport Connection 
        { 
            get
            {
                return _connection;
            }
            private set
            {
                _connection = value;
            }
        }
    }

    [DataContract(Name = nameof(CoreReport))]
    public sealed class CoreReport
    {
        private CacheReport _cache;
        private NetworkReport _network;

        public CoreReport(CacheReport cache, NetworkReport network)
        { 
            this.Cache = cache;
            this.Network = network;
        }

        [DataMember(Name = nameof(Cache))]
        public CacheReport Cache 
        { 
            get
            {
                return _cache;
            }
            private set
            {
                _cache = value;
            }
        }

        [DataMember(Name = nameof(Network))]
        public NetworkReport Network 
        { 
            get
            {
                return _network;
            }
            private set
            {
                _network = value;
            }
        }
    }

    [DataContract(Name = nameof(CacheReport))]
    public sealed class CacheReport
    {
        private long _blockCount;
        private long _usingSpace;
        private long _lockSpace;
        private long _freeSpace;

        public CacheReport(long blockCount, long usingSpace, long lockSpace, long freeSpace)
        { 
            this.BlockCount = blockCount;
            this.UsingSpace = usingSpace;
            this.LockSpace = lockSpace;
            this.FreeSpace = freeSpace;
        }

        [DataMember(Name = nameof(BlockCount))]
        public long BlockCount 
        { 
            get
            {
                return _blockCount;
            }
            private set
            {
                _blockCount = value;
            }
        }

        [DataMember(Name = nameof(UsingSpace))]
        public long UsingSpace 
        { 
            get
            {
                return _usingSpace;
            }
            private set
            {
                _usingSpace = value;
            }
        }

        [DataMember(Name = nameof(LockSpace))]
        public long LockSpace 
        { 
            get
            {
                return _lockSpace;
            }
            private set
            {
                _lockSpace = value;
            }
        }

        [DataMember(Name = nameof(FreeSpace))]
        public long FreeSpace 
        { 
            get
            {
                return _freeSpace;
            }
            private set
            {
                _freeSpace = value;
            }
        }
    }

    [DataContract(Name = nameof(NetworkReport))]
    public sealed class NetworkReport
    {
        private Location _myLocation;
        private long _connectCount;
        private long _acceptCount;
        private int _cloudNodeCount;
        private int _messageCount;
        private int _uploadBlockCount;
        private int _diffusionBlockCount;
        private long _totalReceivedByteCount;
        private long _totalSentByteCount;
        private long _pushLocationCount;
        private long _pushBlockLinkCount;
        private long _pushBlockRequestCount;
        private long _pushBlockResultCount;
        private long _pushMessageRequestCount;
        private long _pushMessageResultCount;
        private long _pullLocationCount;
        private long _pullBlockLinkCount;
        private long _pullBlockRequestCount;
        private long _pullBlockResultCount;
        private long _pullMessageRequestCount;
        private long _pullMessageResultCount;

        public NetworkReport(Location myLocation, long connectCount, long acceptCount, int cloudNodeCount, int messageCount, int uploadBlockCount, int diffusionBlockCount, long totalReceivedByteCount, long totalSentByteCount, long pushLocationCount, long pushBlockLinkCount, long pushBlockRequestCount, long pushBlockResultCount, long pushMessageRequestCount, long pushMessageResultCount, long pullLocationCount, long pullBlockLinkCount, long pullBlockRequestCount, long pullBlockResultCount, long pullMessageRequestCount, long pullMessageResultCount)
        { 
            this.MyLocation = myLocation;
            this.ConnectCount = connectCount;
            this.AcceptCount = acceptCount;
            this.CloudNodeCount = cloudNodeCount;
            this.MessageCount = messageCount;
            this.UploadBlockCount = uploadBlockCount;
            this.DiffusionBlockCount = diffusionBlockCount;
            this.TotalReceivedByteCount = totalReceivedByteCount;
            this.TotalSentByteCount = totalSentByteCount;
            this.PushLocationCount = pushLocationCount;
            this.PushBlockLinkCount = pushBlockLinkCount;
            this.PushBlockRequestCount = pushBlockRequestCount;
            this.PushBlockResultCount = pushBlockResultCount;
            this.PushMessageRequestCount = pushMessageRequestCount;
            this.PushMessageResultCount = pushMessageResultCount;
            this.PullLocationCount = pullLocationCount;
            this.PullBlockLinkCount = pullBlockLinkCount;
            this.PullBlockRequestCount = pullBlockRequestCount;
            this.PullBlockResultCount = pullBlockResultCount;
            this.PullMessageRequestCount = pullMessageRequestCount;
            this.PullMessageResultCount = pullMessageResultCount;
        }

        [DataMember(Name = nameof(MyLocation))]
        public Location MyLocation 
        { 
            get
            {
                return _myLocation;
            }
            private set
            {
                _myLocation = value;
            }
        }

        [DataMember(Name = nameof(ConnectCount))]
        public long ConnectCount 
        { 
            get
            {
                return _connectCount;
            }
            private set
            {
                _connectCount = value;
            }
        }

        [DataMember(Name = nameof(AcceptCount))]
        public long AcceptCount 
        { 
            get
            {
                return _acceptCount;
            }
            private set
            {
                _acceptCount = value;
            }
        }

        [DataMember(Name = nameof(CloudNodeCount))]
        public int CloudNodeCount 
        { 
            get
            {
                return _cloudNodeCount;
            }
            private set
            {
                _cloudNodeCount = value;
            }
        }

        [DataMember(Name = nameof(MessageCount))]
        public int MessageCount 
        { 
            get
            {
                return _messageCount;
            }
            private set
            {
                _messageCount = value;
            }
        }

        [DataMember(Name = nameof(UploadBlockCount))]
        public int UploadBlockCount 
        { 
            get
            {
                return _uploadBlockCount;
            }
            private set
            {
                _uploadBlockCount = value;
            }
        }

        [DataMember(Name = nameof(DiffusionBlockCount))]
        public int DiffusionBlockCount 
        { 
            get
            {
                return _diffusionBlockCount;
            }
            private set
            {
                _diffusionBlockCount = value;
            }
        }

        [DataMember(Name = nameof(TotalReceivedByteCount))]
        public long TotalReceivedByteCount 
        { 
            get
            {
                return _totalReceivedByteCount;
            }
            private set
            {
                _totalReceivedByteCount = value;
            }
        }

        [DataMember(Name = nameof(TotalSentByteCount))]
        public long TotalSentByteCount 
        { 
            get
            {
                return _totalSentByteCount;
            }
            private set
            {
                _totalSentByteCount = value;
            }
        }

        [DataMember(Name = nameof(PushLocationCount))]
        public long PushLocationCount 
        { 
            get
            {
                return _pushLocationCount;
            }
            private set
            {
                _pushLocationCount = value;
            }
        }

        [DataMember(Name = nameof(PushBlockLinkCount))]
        public long PushBlockLinkCount 
        { 
            get
            {
                return _pushBlockLinkCount;
            }
            private set
            {
                _pushBlockLinkCount = value;
            }
        }

        [DataMember(Name = nameof(PushBlockRequestCount))]
        public long PushBlockRequestCount 
        { 
            get
            {
                return _pushBlockRequestCount;
            }
            private set
            {
                _pushBlockRequestCount = value;
            }
        }

        [DataMember(Name = nameof(PushBlockResultCount))]
        public long PushBlockResultCount 
        { 
            get
            {
                return _pushBlockResultCount;
            }
            private set
            {
                _pushBlockResultCount = value;
            }
        }

        [DataMember(Name = nameof(PushMessageRequestCount))]
        public long PushMessageRequestCount 
        { 
            get
            {
                return _pushMessageRequestCount;
            }
            private set
            {
                _pushMessageRequestCount = value;
            }
        }

        [DataMember(Name = nameof(PushMessageResultCount))]
        public long PushMessageResultCount 
        { 
            get
            {
                return _pushMessageResultCount;
            }
            private set
            {
                _pushMessageResultCount = value;
            }
        }

        [DataMember(Name = nameof(PullLocationCount))]
        public long PullLocationCount 
        { 
            get
            {
                return _pullLocationCount;
            }
            private set
            {
                _pullLocationCount = value;
            }
        }

        [DataMember(Name = nameof(PullBlockLinkCount))]
        public long PullBlockLinkCount 
        { 
            get
            {
                return _pullBlockLinkCount;
            }
            private set
            {
                _pullBlockLinkCount = value;
            }
        }

        [DataMember(Name = nameof(PullBlockRequestCount))]
        public long PullBlockRequestCount 
        { 
            get
            {
                return _pullBlockRequestCount;
            }
            private set
            {
                _pullBlockRequestCount = value;
            }
        }

        [DataMember(Name = nameof(PullBlockResultCount))]
        public long PullBlockResultCount 
        { 
            get
            {
                return _pullBlockResultCount;
            }
            private set
            {
                _pullBlockResultCount = value;
            }
        }

        [DataMember(Name = nameof(PullMessageRequestCount))]
        public long PullMessageRequestCount 
        { 
            get
            {
                return _pullMessageRequestCount;
            }
            private set
            {
                _pullMessageRequestCount = value;
            }
        }

        [DataMember(Name = nameof(PullMessageResultCount))]
        public long PullMessageResultCount 
        { 
            get
            {
                return _pullMessageResultCount;
            }
            private set
            {
                _pullMessageResultCount = value;
            }
        }
    }

    [DataContract(Name = nameof(ConnectionReport))]
    public sealed class ConnectionReport
    {
        private TcpConnectionReport _tcp;
        private CustomConnectionReport _custom;

        public ConnectionReport(TcpConnectionReport tcp, CustomConnectionReport custom)
        { 
            this.Tcp = tcp;
            this.Custom = custom;
        }

        [DataMember(Name = nameof(Tcp))]
        public TcpConnectionReport Tcp 
        { 
            get
            {
                return _tcp;
            }
            private set
            {
                _tcp = value;
            }
        }

        [DataMember(Name = nameof(Custom))]
        public CustomConnectionReport Custom 
        { 
            get
            {
                return _custom;
            }
            private set
            {
                _custom = value;
            }
        }
    }

    [DataContract(Name = nameof(TcpConnectionReport))]
    public sealed class TcpConnectionReport
    {
        private long _catharsisBlockCount;

        public TcpConnectionReport(long catharsisBlockCount)
        { 
            this.CatharsisBlockCount = catharsisBlockCount;
        }

        [DataMember(Name = nameof(CatharsisBlockCount))]
        public long CatharsisBlockCount 
        { 
            get
            {
                return _catharsisBlockCount;
            }
            private set
            {
                _catharsisBlockCount = value;
            }
        }
    }

    [DataContract(Name = nameof(CustomConnectionReport))]
    public sealed class CustomConnectionReport
    {
        private long _catharsisBlockCount;

        public CustomConnectionReport(long catharsisBlockCount)
        { 
            this.CatharsisBlockCount = catharsisBlockCount;
        }

        [DataMember(Name = nameof(CatharsisBlockCount))]
        public long CatharsisBlockCount 
        { 
            get
            {
                return _catharsisBlockCount;
            }
            private set
            {
                _catharsisBlockCount = value;
            }
        }
    }

    [DataContract(Name = nameof(NetworkConnectionReport))]
    public sealed class NetworkConnectionReport
    {
        private byte[] _id;
        private SessionType _type;
        private string _uri;
        private Location _location;
        private double _priority;
        private long _receivedByteCount;
        private long _sentByteCount;

        public NetworkConnectionReport(byte[] id, SessionType type, string uri, Location location, double priority, long receivedByteCount, long sentByteCount)
        { 
            this.Id = id;
            this.Type = type;
            this.Uri = uri;
            this.Location = location;
            this.Priority = priority;
            this.ReceivedByteCount = receivedByteCount;
            this.SentByteCount = sentByteCount;
        }

        [DataMember(Name = nameof(Id))]
        public byte[] Id 
        { 
            get
            {
                return _id;
            }
            private set
            {
                _id = value;
            }
        }

        [DataMember(Name = nameof(Type))]
        public SessionType Type 
        { 
            get
            {
                return _type;
            }
            private set
            {
                _type = value;
            }
        }

        [DataMember(Name = nameof(Uri))]
        public string Uri 
        { 
            get
            {
                return _uri;
            }
            private set
            {
                _uri = value;
            }
        }

        [DataMember(Name = nameof(Location))]
        public Location Location 
        { 
            get
            {
                return _location;
            }
            private set
            {
                _location = value;
            }
        }

        [DataMember(Name = nameof(Priority))]
        public double Priority 
        { 
            get
            {
                return _priority;
            }
            private set
            {
                _priority = value;
            }
        }

        [DataMember(Name = nameof(ReceivedByteCount))]
        public long ReceivedByteCount 
        { 
            get
            {
                return _receivedByteCount;
            }
            private set
            {
                _receivedByteCount = value;
            }
        }

        [DataMember(Name = nameof(SentByteCount))]
        public long SentByteCount 
        { 
            get
            {
                return _sentByteCount;
            }
            private set
            {
                _sentByteCount = value;
            }
        }
    }

    [DataContract(Name = nameof(CacheContentReport))]
    public sealed class CacheContentReport
    {
        private DateTime _creationTime;
        private long _length;
        private Metadata _metadata;
        private string _path;

        public CacheContentReport(DateTime creationTime, long length, Metadata metadata, string path)
        { 
            this.CreationTime = creationTime;
            this.Length = length;
            this.Metadata = metadata;
            this.Path = path;
        }

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime 
        { 
            get
            {
                return _creationTime;
            }
            private set
            {
                _creationTime = value;
            }
        }

        [DataMember(Name = nameof(Length))]
        public long Length 
        { 
            get
            {
                return _length;
            }
            private set
            {
                _length = value;
            }
        }

        [DataMember(Name = nameof(Metadata))]
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

        [DataMember(Name = nameof(Path))]
        public string Path 
        { 
            get
            {
                return _path;
            }
            private set
            {
                _path = value;
            }
        }
    }

    [DataContract(Name = nameof(DownloadContentReport))]
    public sealed class DownloadContentReport
    {
        private Metadata _metadata;
        private string _path;
        private DownloadState _state;
        private int _depth;
        private int _blockCount;
        private int _downloadBlockCount;
        private int _parityBlockCount;

        public DownloadContentReport(Metadata metadata, string path, DownloadState state, int depth, int blockCount, int downloadBlockCount, int parityBlockCount)
        { 
            this.Metadata = metadata;
            this.Path = path;
            this.State = state;
            this.Depth = depth;
            this.BlockCount = blockCount;
            this.DownloadBlockCount = downloadBlockCount;
            this.ParityBlockCount = parityBlockCount;
        }

        [DataMember(Name = nameof(Metadata))]
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

        [DataMember(Name = nameof(Path))]
        public string Path 
        { 
            get
            {
                return _path;
            }
            private set
            {
                _path = value;
            }
        }

        [DataMember(Name = nameof(State))]
        public DownloadState State 
        { 
            get
            {
                return _state;
            }
            private set
            {
                _state = value;
            }
        }

        [DataMember(Name = nameof(Depth))]
        public int Depth 
        { 
            get
            {
                return _depth;
            }
            private set
            {
                _depth = value;
            }
        }

        [DataMember(Name = nameof(BlockCount))]
        public int BlockCount 
        { 
            get
            {
                return _blockCount;
            }
            private set
            {
                _blockCount = value;
            }
        }

        [DataMember(Name = nameof(DownloadBlockCount))]
        public int DownloadBlockCount 
        { 
            get
            {
                return _downloadBlockCount;
            }
            private set
            {
                _downloadBlockCount = value;
            }
        }

        [DataMember(Name = nameof(ParityBlockCount))]
        public int ParityBlockCount 
        { 
            get
            {
                return _parityBlockCount;
            }
            private set
            {
                _parityBlockCount = value;
            }
        }
    }

    [DataContract(Name = nameof(ServiceConfig))]
    public sealed class ServiceConfig
    {
        private CoreConfig _core;
        private ConnectionConfig _connection;
        private MessageConfig _message;

        public ServiceConfig(CoreConfig core, ConnectionConfig connection, MessageConfig message)
        { 
            this.Core = core;
            this.Connection = connection;
            this.Message = message;
        }

        [DataMember(Name = nameof(Core))]
        public CoreConfig Core 
        { 
            get
            {
                return _core;
            }
            private set
            {
                _core = value;
            }
        }

        [DataMember(Name = nameof(Connection))]
        public ConnectionConfig Connection 
        { 
            get
            {
                return _connection;
            }
            private set
            {
                _connection = value;
            }
        }

        [DataMember(Name = nameof(Message))]
        public MessageConfig Message 
        { 
            get
            {
                return _message;
            }
            private set
            {
                _message = value;
            }
        }
    }

    [DataContract(Name = nameof(CoreConfig))]
    public sealed class CoreConfig
    {
        private NetworkConfig _network;
        private DownloadConfig _download;

        public CoreConfig(NetworkConfig network, DownloadConfig download)
        { 
            this.Network = network;
            this.Download = download;
        }

        [DataMember(Name = nameof(Network))]
        public NetworkConfig Network 
        { 
            get
            {
                return _network;
            }
            private set
            {
                _network = value;
            }
        }

        [DataMember(Name = nameof(Download))]
        public DownloadConfig Download 
        { 
            get
            {
                return _download;
            }
            private set
            {
                _download = value;
            }
        }
    }

    [DataContract(Name = nameof(NetworkConfig))]
    public sealed class NetworkConfig
    {
        private int _connectionCountLimit;
        private int _bandwidthLimit;

        public NetworkConfig(int connectionCountLimit, int bandwidthLimit)
        { 
            this.ConnectionCountLimit = connectionCountLimit;
            this.BandwidthLimit = bandwidthLimit;
        }

        [DataMember(Name = nameof(ConnectionCountLimit))]
        public int ConnectionCountLimit 
        { 
            get
            {
                return _connectionCountLimit;
            }
            private set
            {
                _connectionCountLimit = value;
            }
        }

        [DataMember(Name = nameof(BandwidthLimit))]
        public int BandwidthLimit 
        { 
            get
            {
                return _bandwidthLimit;
            }
            private set
            {
                _bandwidthLimit = value;
            }
        }
    }

    [DataContract(Name = nameof(DownloadConfig))]
    public sealed class DownloadConfig
    {
        private string _basePath;

        public DownloadConfig(string basePath)
        { 
            this.BasePath = basePath;
        }

        [DataMember(Name = nameof(BasePath))]
        public string BasePath 
        { 
            get
            {
                return _basePath;
            }
            private set
            {
                _basePath = value;
            }
        }
    }

    [DataContract(Name = nameof(ConnectionConfig))]
    public sealed class ConnectionConfig
    {
        private TcpConnectionConfig _tcp;
        private I2pConnectionConfig _i2p;
        private CustomConnectionConfig _custom;
        private CatharsisConfig _catharsis;

        public ConnectionConfig(TcpConnectionConfig tcp, I2pConnectionConfig i2p, CustomConnectionConfig custom, CatharsisConfig catharsis)
        { 
            this.Tcp = tcp;
            this.I2p = i2p;
            this.Custom = custom;
            this.Catharsis = catharsis;
        }

        [DataMember(Name = nameof(Tcp))]
        public TcpConnectionConfig Tcp 
        { 
            get
            {
                return _tcp;
            }
            private set
            {
                _tcp = value;
            }
        }

        [DataMember(Name = nameof(I2p))]
        public I2pConnectionConfig I2p 
        { 
            get
            {
                return _i2p;
            }
            private set
            {
                _i2p = value;
            }
        }

        [DataMember(Name = nameof(Custom))]
        public CustomConnectionConfig Custom 
        { 
            get
            {
                return _custom;
            }
            private set
            {
                _custom = value;
            }
        }

        [DataMember(Name = nameof(Catharsis))]
        public CatharsisConfig Catharsis 
        { 
            get
            {
                return _catharsis;
            }
            private set
            {
                _catharsis = value;
            }
        }
    }

    [DataContract(Name = nameof(CatharsisConfig))]
    public sealed class CatharsisConfig
    {
        private CatharsisIpv4Config _ipv4Config;

        public CatharsisConfig(CatharsisIpv4Config ipv4Config)
        { 
            this.Ipv4Config = ipv4Config;
        }

        [DataMember(Name = nameof(Ipv4Config))]
        public CatharsisIpv4Config Ipv4Config 
        { 
            get
            {
                return _ipv4Config;
            }
            private set
            {
                _ipv4Config = value;
            }
        }
    }

    [DataContract(Name = nameof(CatharsisIpv4Config))]
    public sealed class CatharsisIpv4Config
    {
        private List<string> _urls;
        private List<string> _paths;

        public CatharsisIpv4Config(IEnumerable<string> urls, IEnumerable<string> paths)
        { 
            if (urls != null) this.ProtectedUrls.AddRange(urls);
            if (paths != null) this.ProtectedPaths.AddRange(paths);
        }

        private volatile ReadOnlyCollection<string> _readOnlyUrls;

        public IEnumerable<string> Urls
        {
            get
            {
                if (_readOnlyUrls == null)
                    _readOnlyUrls = new ReadOnlyCollection<string>(this.ProtectedUrls);

                return _readOnlyUrls;
            }
        }

        [DataMember(Name = nameof(Urls))]
        private List<string> ProtectedUrls
        {
            get
            {
                if (_urls == null)
                    _urls = new List<string>();

                return _urls;
            }
        }

        private volatile ReadOnlyCollection<string> _readOnlyPaths;

        public IEnumerable<string> Paths
        {
            get
            {
                if (_readOnlyPaths == null)
                    _readOnlyPaths = new ReadOnlyCollection<string>(this.ProtectedPaths);

                return _readOnlyPaths;
            }
        }

        [DataMember(Name = nameof(Paths))]
        private List<string> ProtectedPaths
        {
            get
            {
                if (_paths == null)
                    _paths = new List<string>();

                return _paths;
            }
        }
    }

    [DataContract(Name = nameof(TcpConnectionConfig))]
    public sealed class TcpConnectionConfig
    {
        private TcpConnectionType _type;
        private ushort _ipv4Port;
        private ushort _ipv6Port;
        private string _proxyUri;

        public TcpConnectionConfig(TcpConnectionType type, ushort ipv4Port, ushort ipv6Port, string proxyUri)
        { 
            this.Type = type;
            this.Ipv4Port = ipv4Port;
            this.Ipv6Port = ipv6Port;
            this.ProxyUri = proxyUri;
        }

        [DataMember(Name = nameof(Type))]
        public TcpConnectionType Type 
        { 
            get
            {
                return _type;
            }
            private set
            {
                _type = value;
            }
        }

        [DataMember(Name = nameof(Ipv4Port))]
        public ushort Ipv4Port 
        { 
            get
            {
                return _ipv4Port;
            }
            private set
            {
                _ipv4Port = value;
            }
        }

        [DataMember(Name = nameof(Ipv6Port))]
        public ushort Ipv6Port 
        { 
            get
            {
                return _ipv6Port;
            }
            private set
            {
                _ipv6Port = value;
            }
        }

        [DataMember(Name = nameof(ProxyUri))]
        public string ProxyUri 
        { 
            get
            {
                return _proxyUri;
            }
            private set
            {
                _proxyUri = value;
            }
        }
    }

    [DataContract(Name = nameof(I2pConnectionConfig))]
    public sealed class I2pConnectionConfig
    {
        private bool _isEnabled;
        private string _samBridgeUri;

        public I2pConnectionConfig(bool isEnabled, string samBridgeUri)
        { 
            this.IsEnabled = isEnabled;
            this.SamBridgeUri = samBridgeUri;
        }

        [DataMember(Name = nameof(IsEnabled))]
        public bool IsEnabled 
        { 
            get
            {
                return _isEnabled;
            }
            private set
            {
                _isEnabled = value;
            }
        }

        [DataMember(Name = nameof(SamBridgeUri))]
        public string SamBridgeUri 
        { 
            get
            {
                return _samBridgeUri;
            }
            private set
            {
                _samBridgeUri = value;
            }
        }
    }

    [DataContract(Name = nameof(CustomConnectionConfig))]
    public sealed class CustomConnectionConfig
    {
        private List<string> _locationUris;
        private List<ConnectionFilter> _connectionFilters;
        private List<string> _listenUris;

        public CustomConnectionConfig(IEnumerable<string> locationUris, IEnumerable<ConnectionFilter> connectionFilters, IEnumerable<string> listenUris)
        { 
            if (locationUris != null) this.ProtectedLocationUris.AddRange(locationUris);
            if (connectionFilters != null) this.ProtectedConnectionFilters.AddRange(connectionFilters);
            if (listenUris != null) this.ProtectedListenUris.AddRange(listenUris);
        }

        private volatile ReadOnlyCollection<string> _readOnlyLocationUris;

        public IEnumerable<string> LocationUris
        {
            get
            {
                if (_readOnlyLocationUris == null)
                    _readOnlyLocationUris = new ReadOnlyCollection<string>(this.ProtectedLocationUris);

                return _readOnlyLocationUris;
            }
        }

        [DataMember(Name = nameof(LocationUris))]
        private List<string> ProtectedLocationUris
        {
            get
            {
                if (_locationUris == null)
                    _locationUris = new List<string>();

                return _locationUris;
            }
        }

        private volatile ReadOnlyCollection<ConnectionFilter> _readOnlyConnectionFilters;

        public IEnumerable<ConnectionFilter> ConnectionFilters
        {
            get
            {
                if (_readOnlyConnectionFilters == null)
                    _readOnlyConnectionFilters = new ReadOnlyCollection<ConnectionFilter>(this.ProtectedConnectionFilters);

                return _readOnlyConnectionFilters;
            }
        }

        [DataMember(Name = nameof(ConnectionFilters))]
        private List<ConnectionFilter> ProtectedConnectionFilters
        {
            get
            {
                if (_connectionFilters == null)
                    _connectionFilters = new List<ConnectionFilter>();

                return _connectionFilters;
            }
        }

        private volatile ReadOnlyCollection<string> _readOnlyListenUris;

        public IEnumerable<string> ListenUris
        {
            get
            {
                if (_readOnlyListenUris == null)
                    _readOnlyListenUris = new ReadOnlyCollection<string>(this.ProtectedListenUris);

                return _readOnlyListenUris;
            }
        }

        [DataMember(Name = nameof(ListenUris))]
        private List<string> ProtectedListenUris
        {
            get
            {
                if (_listenUris == null)
                    _listenUris = new List<string>();

                return _listenUris;
            }
        }
    }

    [DataContract(Name = nameof(MessageConfig))]
    public sealed class MessageConfig
    {
        private List<Signature> _searchSignatures;

        public MessageConfig(IEnumerable<Signature> searchSignatures)
        { 
            if (searchSignatures != null) this.ProtectedSearchSignatures.AddRange(searchSignatures);
        }

        private volatile ReadOnlyCollection<Signature> _readOnlySearchSignatures;

        public IEnumerable<Signature> SearchSignatures
        {
            get
            {
                if (_readOnlySearchSignatures == null)
                    _readOnlySearchSignatures = new ReadOnlyCollection<Signature>(this.ProtectedSearchSignatures);

                return _readOnlySearchSignatures;
            }
        }

        [DataMember(Name = nameof(SearchSignatures))]
        private List<Signature> ProtectedSearchSignatures
        {
            get
            {
                if (_searchSignatures == null)
                    _searchSignatures = new List<Signature>();

                return _searchSignatures;
            }
        }
    }

}
