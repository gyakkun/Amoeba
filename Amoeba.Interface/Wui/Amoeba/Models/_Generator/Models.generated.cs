using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Security;
using Amoeba.Messages;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(AccountInfo))]
    public class AccountInfo : ICloneable<AccountInfo>
    {
        public AccountInfo() { }

        private DigitalSignature _digitalSignature;

        [DataMember(Name = nameof(DigitalSignature))]
        public DigitalSignature DigitalSignature
        {
            get
            {
                return _digitalSignature;
            }
            set
            {
                _digitalSignature = value;
            }
        }

        private string _comment;

        [DataMember(Name = nameof(Comment))]
        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value;
            }
        }

        private Exchange _exchange;

        [DataMember(Name = nameof(Exchange))]
        public Exchange Exchange
        {
            get
            {
                return _exchange;
            }
            set
            {
                _exchange = value;
            }
        }

        private ObservableCollection<Signature> _trustSignatures;

        [DataMember(Name = nameof(TrustSignatures))]
        public ObservableCollection<Signature> TrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new ObservableCollection<Signature>();

                return _trustSignatures;
            }
        }

        private ObservableCollection<Signature> _untrustSignatures;

        [DataMember(Name = nameof(UntrustSignatures))]
        public ObservableCollection<Signature> UntrustSignatures
        {
            get
            {
                if (_untrustSignatures == null)
                    _untrustSignatures = new ObservableCollection<Signature>();

                return _untrustSignatures;
            }
        }

        private ObservableCollection<Tag> _tags;

        [DataMember(Name = nameof(Tags))]
        public ObservableCollection<Tag> Tags
        {
            get
            {
                if (_tags == null)
                    _tags = new ObservableCollection<Tag>();

                return _tags;
            }
        }

        public AccountInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UpdateInfo))]
    public class UpdateInfo : ICloneable<UpdateInfo>
    {
        public UpdateInfo() { }

        private bool _isEnabled;

        [DataMember(Name = nameof(IsEnabled))]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
            }
        }

        private Signature _signature;

        [DataMember(Name = nameof(Signature))]
        public Signature Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                _signature = value;
            }
        }

        public UpdateInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(RelationSignatureInfo))]
    public class RelationSignatureInfo : ICloneable<RelationSignatureInfo>
    {
        public RelationSignatureInfo() { }

        private Signature _signature;

        [DataMember(Name = nameof(Signature))]
        public Signature Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                _signature = value;
            }
        }

        private BroadcastMessage<Profile> _profile;

        [DataMember(Name = nameof(Profile))]
        public BroadcastMessage<Profile> Profile
        {
            get
            {
                return _profile;
            }
            set
            {
                _profile = value;
            }
        }

        private ObservableCollection<RelationSignatureInfo> _children;

        [DataMember(Name = nameof(Children))]
        public ObservableCollection<RelationSignatureInfo> Children
        {
            get
            {
                if (_children == null)
                    _children = new ObservableCollection<RelationSignatureInfo>();

                return _children;
            }
        }

        public RelationSignatureInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(OptionsInfo))]
    public class OptionsInfo : ICloneable<OptionsInfo>
    {
        public OptionsInfo() { }

        private AccountOptionsInfo _account;

        [DataMember(Name = nameof(Account))]
        public AccountOptionsInfo Account
        {
            get
            {
                if (_account == null)
                    _account = new AccountOptionsInfo();

                return _account;
            }
        }

        private ConnectionOptionsInfo _connection;

        [DataMember(Name = nameof(Connection))]
        public ConnectionOptionsInfo Connection
        {
            get
            {
                if (_connection == null)
                    _connection = new ConnectionOptionsInfo();

                return _connection;
            }
        }

        private DataOptionsInfo _data;

        [DataMember(Name = nameof(Data))]
        public DataOptionsInfo Data
        {
            get
            {
                if (_data == null)
                    _data = new DataOptionsInfo();

                return _data;
            }
        }

        private ViewOptionsInfo _view;

        [DataMember(Name = nameof(View))]
        public ViewOptionsInfo View
        {
            get
            {
                if (_view == null)
                    _view = new ViewOptionsInfo();

                return _view;
            }
        }

        private UpdateOptionsInfo _update;

        [DataMember(Name = nameof(Update))]
        public UpdateOptionsInfo Update
        {
            get
            {
                if (_update == null)
                    _update = new UpdateOptionsInfo();

                return _update;
            }
        }

        public OptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(AccountOptionsInfo))]
    public class AccountOptionsInfo : ICloneable<AccountOptionsInfo>
    {
        public AccountOptionsInfo() { }

        private DigitalSignature _digitalSignature;

        [DataMember(Name = nameof(DigitalSignature))]
        public DigitalSignature DigitalSignature
        {
            get
            {
                return _digitalSignature;
            }
            set
            {
                _digitalSignature = value;
            }
        }

        private string _comment;

        [DataMember(Name = nameof(Comment))]
        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value;
            }
        }

        private ObservableCollection<Signature> _trustSignatures;

        [DataMember(Name = nameof(TrustSignatures))]
        public ObservableCollection<Signature> TrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new ObservableCollection<Signature>();

                return _trustSignatures;
            }
        }

        private ObservableCollection<Signature> _untrustSignatures;

        [DataMember(Name = nameof(UntrustSignatures))]
        public ObservableCollection<Signature> UntrustSignatures
        {
            get
            {
                if (_untrustSignatures == null)
                    _untrustSignatures = new ObservableCollection<Signature>();

                return _untrustSignatures;
            }
        }

        private ObservableCollection<Tag> _tags;

        [DataMember(Name = nameof(Tags))]
        public ObservableCollection<Tag> Tags
        {
            get
            {
                if (_tags == null)
                    _tags = new ObservableCollection<Tag>();

                return _tags;
            }
        }

        public AccountOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ConnectionOptionsInfo))]
    public class ConnectionOptionsInfo : ICloneable<ConnectionOptionsInfo>
    {
        public ConnectionOptionsInfo() { }

        private TcpOptionsInfo _tcp;

        [DataMember(Name = nameof(Tcp))]
        public TcpOptionsInfo Tcp
        {
            get
            {
                if (_tcp == null)
                    _tcp = new TcpOptionsInfo();

                return _tcp;
            }
        }

        private I2pOptionsInfo _i2p;

        [DataMember(Name = nameof(I2p))]
        public I2pOptionsInfo I2p
        {
            get
            {
                if (_i2p == null)
                    _i2p = new I2pOptionsInfo();

                return _i2p;
            }
        }

        private CustomOptionsInfo _custom;

        [DataMember(Name = nameof(Custom))]
        public CustomOptionsInfo Custom
        {
            get
            {
                if (_custom == null)
                    _custom = new CustomOptionsInfo();

                return _custom;
            }
        }

        private BandwidthOptionsInfo _bandwidth;

        [DataMember(Name = nameof(Bandwidth))]
        public BandwidthOptionsInfo Bandwidth
        {
            get
            {
                if (_bandwidth == null)
                    _bandwidth = new BandwidthOptionsInfo();

                return _bandwidth;
            }
        }

        public ConnectionOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(TcpOptionsInfo))]
    public class TcpOptionsInfo : ICloneable<TcpOptionsInfo>
    {
        public TcpOptionsInfo() { }

        private bool _ipv4IsEnabled;

        [DataMember(Name = nameof(Ipv4IsEnabled))]
        public bool Ipv4IsEnabled
        {
            get
            {
                return _ipv4IsEnabled;
            }
            set
            {
                _ipv4IsEnabled = value;
            }
        }

        private ushort _ipv4Port;

        [DataMember(Name = nameof(Ipv4Port))]
        public ushort Ipv4Port
        {
            get
            {
                return _ipv4Port;
            }
            set
            {
                _ipv4Port = value;
            }
        }

        private bool _ipv6IsEnabled;

        [DataMember(Name = nameof(Ipv6IsEnabled))]
        public bool Ipv6IsEnabled
        {
            get
            {
                return _ipv6IsEnabled;
            }
            set
            {
                _ipv6IsEnabled = value;
            }
        }

        private ushort _ipv6Port;

        [DataMember(Name = nameof(Ipv6Port))]
        public ushort Ipv6Port
        {
            get
            {
                return _ipv6Port;
            }
            set
            {
                _ipv6Port = value;
            }
        }

        private string _proxyUri;

        [DataMember(Name = nameof(ProxyUri))]
        public string ProxyUri
        {
            get
            {
                return _proxyUri;
            }
            set
            {
                _proxyUri = value;
            }
        }

        public TcpOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(I2pOptionsInfo))]
    public class I2pOptionsInfo : ICloneable<I2pOptionsInfo>
    {
        public I2pOptionsInfo() { }

        private bool _isEnabled;

        [DataMember(Name = nameof(IsEnabled))]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
            }
        }

        private string _samBridgeUri;

        [DataMember(Name = nameof(SamBridgeUri))]
        public string SamBridgeUri
        {
            get
            {
                return _samBridgeUri;
            }
            set
            {
                _samBridgeUri = value;
            }
        }

        public I2pOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(CustomOptionsInfo))]
    public class CustomOptionsInfo : ICloneable<CustomOptionsInfo>
    {
        public CustomOptionsInfo() { }

        private ObservableCollection<string> _locationUris;

        [DataMember(Name = nameof(LocationUris))]
        public ObservableCollection<string> LocationUris
        {
            get
            {
                if (_locationUris == null)
                    _locationUris = new ObservableCollection<string>();

                return _locationUris;
            }
        }

        private ObservableCollection<ConnectionFilter> _connectionFilters;

        [DataMember(Name = nameof(ConnectionFilters))]
        public ObservableCollection<ConnectionFilter> ConnectionFilters
        {
            get
            {
                if (_connectionFilters == null)
                    _connectionFilters = new ObservableCollection<ConnectionFilter>();

                return _connectionFilters;
            }
        }

        private ObservableCollection<string> _listenUris;

        [DataMember(Name = nameof(ListenUris))]
        public ObservableCollection<string> ListenUris
        {
            get
            {
                if (_listenUris == null)
                    _listenUris = new ObservableCollection<string>();

                return _listenUris;
            }
        }

        public CustomOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(BandwidthOptionsInfo))]
    public class BandwidthOptionsInfo : ICloneable<BandwidthOptionsInfo>
    {
        public BandwidthOptionsInfo() { }

        private int _connectionCountLimit;

        [DataMember(Name = nameof(ConnectionCountLimit))]
        public int ConnectionCountLimit
        {
            get
            {
                return _connectionCountLimit;
            }
            set
            {
                _connectionCountLimit = value;
            }
        }

        private int _bandwidthLimit;

        [DataMember(Name = nameof(BandwidthLimit))]
        public int BandwidthLimit
        {
            get
            {
                return _bandwidthLimit;
            }
            set
            {
                _bandwidthLimit = value;
            }
        }

        public BandwidthOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DataOptionsInfo))]
    public class DataOptionsInfo : ICloneable<DataOptionsInfo>
    {
        public DataOptionsInfo() { }

        private CacheOptionsInfo _cache;

        [DataMember(Name = nameof(Cache))]
        public CacheOptionsInfo Cache
        {
            get
            {
                if (_cache == null)
                    _cache = new CacheOptionsInfo();

                return _cache;
            }
        }

        private DownloadOptionsInfo _download;

        [DataMember(Name = nameof(Download))]
        public DownloadOptionsInfo Download
        {
            get
            {
                if (_download == null)
                    _download = new DownloadOptionsInfo();

                return _download;
            }
        }

        public DataOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(CacheOptionsInfo))]
    public class CacheOptionsInfo : ICloneable<CacheOptionsInfo>
    {
        public CacheOptionsInfo() { }

        private long _size;

        [DataMember(Name = nameof(Size))]
        public long Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        public CacheOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DownloadOptionsInfo))]
    public class DownloadOptionsInfo : ICloneable<DownloadOptionsInfo>
    {
        public DownloadOptionsInfo() { }

        private string _directoryPath;

        [DataMember(Name = nameof(DirectoryPath))]
        public string DirectoryPath
        {
            get
            {
                return _directoryPath;
            }
            set
            {
                _directoryPath = value;
            }
        }

        public DownloadOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ViewOptionsInfo))]
    public class ViewOptionsInfo : ICloneable<ViewOptionsInfo>
    {
        public ViewOptionsInfo() { }

        private SubscribeOptionsInfo _subscribe;

        [DataMember(Name = nameof(Subscribe))]
        public SubscribeOptionsInfo Subscribe
        {
            get
            {
                if (_subscribe == null)
                    _subscribe = new SubscribeOptionsInfo();

                return _subscribe;
            }
        }

        public ViewOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SubscribeOptionsInfo))]
    public class SubscribeOptionsInfo : ICloneable<SubscribeOptionsInfo>
    {
        public SubscribeOptionsInfo() { }

        private ObservableCollection<Signature> _subscribeSignatures;

        [DataMember(Name = nameof(SubscribeSignatures))]
        public ObservableCollection<Signature> SubscribeSignatures
        {
            get
            {
                if (_subscribeSignatures == null)
                    _subscribeSignatures = new ObservableCollection<Signature>();

                return _subscribeSignatures;
            }
        }

        public SubscribeOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UpdateOptionsInfo))]
    public class UpdateOptionsInfo : ICloneable<UpdateOptionsInfo>
    {
        public UpdateOptionsInfo() { }

        private bool _isEnabled;

        [DataMember(Name = nameof(IsEnabled))]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
            }
        }

        private Signature _signature;

        [DataMember(Name = nameof(Signature))]
        public Signature Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                _signature = value;
            }
        }

        public UpdateOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(CloudStateInfo))]
    public class CloudStateInfo : ICloneable<CloudStateInfo>
    {
        public CloudStateInfo() { }

        private string _location;

        [DataMember(Name = nameof(Location))]
        public string Location
        {
            get
            {
                return _location;
            }
            set
            {
                _location = value;
            }
        }

        public CloudStateInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ChatCategoryInfo))]
    public class ChatCategoryInfo : ICloneable<ChatCategoryInfo>
    {
        public ChatCategoryInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
            }
        }

        private ObservableCollection<ChatThreadInfo> _threadInfos;

        [DataMember(Name = nameof(ThreadInfos))]
        public ObservableCollection<ChatThreadInfo> ThreadInfos
        {
            get
            {
                if (_threadInfos == null)
                    _threadInfos = new ObservableCollection<ChatThreadInfo>();

                return _threadInfos;
            }
        }

        private ObservableCollection<ChatCategoryInfo> _categoryInfos;

        [DataMember(Name = nameof(CategoryInfos))]
        public ObservableCollection<ChatCategoryInfo> CategoryInfos
        {
            get
            {
                if (_categoryInfos == null)
                    _categoryInfos = new ObservableCollection<ChatCategoryInfo>();

                return _categoryInfos;
            }
        }

        public ChatCategoryInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ChatThreadInfo))]
    public class ChatThreadInfo : ICloneable<ChatThreadInfo>
    {
        public ChatThreadInfo() { }

        private bool _isUpdated;

        [DataMember(Name = nameof(IsUpdated))]
        public bool IsUpdated
        {
            get
            {
                return _isUpdated;
            }
            set
            {
                _isUpdated = value;
            }
        }

        private Tag _tag;

        [DataMember(Name = nameof(Tag))]
        public Tag Tag
        {
            get
            {
                return _tag;
            }
            set
            {
                _tag = value;
            }
        }

        private bool _isTrustMessageOnly;

        [DataMember(Name = nameof(IsTrustMessageOnly))]
        public bool IsTrustMessageOnly
        {
            get
            {
                return _isTrustMessageOnly;
            }
            set
            {
                _isTrustMessageOnly = value;
            }
        }

        private bool _isNewMessageOnly;

        [DataMember(Name = nameof(IsNewMessageOnly))]
        public bool IsNewMessageOnly
        {
            get
            {
                return _isNewMessageOnly;
            }
            set
            {
                _isNewMessageOnly = value;
            }
        }

        private LockedList<ChatMessageInfo> _messages;

        [DataMember(Name = nameof(Messages))]
        public LockedList<ChatMessageInfo> Messages
        {
            get
            {
                if (_messages == null)
                    _messages = new LockedList<ChatMessageInfo>();

                return _messages;
            }
        }

        public ChatThreadInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ChatMessageInfo))]
    public class ChatMessageInfo : ICloneable<ChatMessageInfo>
    {
        public ChatMessageInfo() { }

        private ChatMessageState _state;

        [DataMember(Name = nameof(State))]
        public ChatMessageState State
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

        private MulticastMessage<ChatMessage> _message;

        [DataMember(Name = nameof(Message))]
        public MulticastMessage<ChatMessage> Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
            }
        }

        public ChatMessageInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SubscribeCategoryInfo))]
    public class SubscribeCategoryInfo : ICloneable<SubscribeCategoryInfo>
    {
        public SubscribeCategoryInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
            }
        }

        private ObservableCollection<SubscribeStoreInfo> _storeInfos;

        [DataMember(Name = nameof(StoreInfos))]
        public ObservableCollection<SubscribeStoreInfo> StoreInfos
        {
            get
            {
                if (_storeInfos == null)
                    _storeInfos = new ObservableCollection<SubscribeStoreInfo>();

                return _storeInfos;
            }
        }

        private ObservableCollection<SubscribeCategoryInfo> _categoryInfos;

        [DataMember(Name = nameof(CategoryInfos))]
        public ObservableCollection<SubscribeCategoryInfo> CategoryInfos
        {
            get
            {
                if (_categoryInfos == null)
                    _categoryInfos = new ObservableCollection<SubscribeCategoryInfo>();

                return _categoryInfos;
            }
        }

        public SubscribeCategoryInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SubscribeStoreInfo))]
    public class SubscribeStoreInfo : ICloneable<SubscribeStoreInfo>
    {
        public SubscribeStoreInfo() { }

        private Signature _authorSignature;

        [DataMember(Name = nameof(AuthorSignature))]
        public Signature AuthorSignature
        {
            get
            {
                return _authorSignature;
            }
            set
            {
                _authorSignature = value;
            }
        }

        private DateTime _creationTime;

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            set
            {
                _creationTime = value;
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
            }
        }

        private bool _isUpdated;

        [DataMember(Name = nameof(IsUpdated))]
        public bool IsUpdated
        {
            get
            {
                return _isUpdated;
            }
            set
            {
                _isUpdated = value;
            }
        }

        private ObservableCollection<SubscribeBoxInfo> _boxInfos;

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<SubscribeBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<SubscribeBoxInfo>();

                return _boxInfos;
            }
        }

        public SubscribeStoreInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SubscribeBoxInfo))]
    public class SubscribeBoxInfo : ICloneable<SubscribeBoxInfo>
    {
        public SubscribeBoxInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
            }
        }

        private ObservableCollection<Seed> _seeds;

        [DataMember(Name = nameof(Seeds))]
        public ObservableCollection<Seed> Seeds
        {
            get
            {
                if (_seeds == null)
                    _seeds = new ObservableCollection<Seed>();

                return _seeds;
            }
        }

        private ObservableCollection<SubscribeBoxInfo> _boxInfos;

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<SubscribeBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<SubscribeBoxInfo>();

                return _boxInfos;
            }
        }

        public SubscribeBoxInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SubscribeListViewItemInfo))]
    public class SubscribeListViewItemInfo : ICloneable<SubscribeListViewItemInfo>
    {
        public SubscribeListViewItemInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private long _length;

        [DataMember(Name = nameof(Length))]
        public long Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        private DateTime _creationTime;

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            set
            {
                _creationTime = value;
            }
        }

        private SearchState _state;

        [DataMember(Name = nameof(State))]
        public SearchState State
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

        private object _model;

        [DataMember(Name = nameof(Model))]
        public object Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
            }
        }

        public SubscribeListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(PublishStoreInfo))]
    public class PublishStoreInfo : ICloneable<PublishStoreInfo>
    {
        public PublishStoreInfo() { }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
            }
        }

        private bool _isUpdated;

        [DataMember(Name = nameof(IsUpdated))]
        public bool IsUpdated
        {
            get
            {
                return _isUpdated;
            }
            set
            {
                _isUpdated = value;
            }
        }

        private ObservableCollection<PublishBoxInfo> _boxInfos;

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<PublishBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<PublishBoxInfo>();

                return _boxInfos;
            }
        }

        public PublishStoreInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(PublishBoxInfo))]
    public class PublishBoxInfo : ICloneable<PublishBoxInfo>
    {
        public PublishBoxInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
            }
        }

        private ObservableCollection<Seed> _seeds;

        [DataMember(Name = nameof(Seeds))]
        public ObservableCollection<Seed> Seeds
        {
            get
            {
                if (_seeds == null)
                    _seeds = new ObservableCollection<Seed>();

                return _seeds;
            }
        }

        private ObservableCollection<PublishBoxInfo> _boxInfos;

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<PublishBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<PublishBoxInfo>();

                return _boxInfos;
            }
        }

        public PublishBoxInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(PublishListViewItemInfo))]
    public class PublishListViewItemInfo : ICloneable<PublishListViewItemInfo>
    {
        public PublishListViewItemInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private long _length;

        [DataMember(Name = nameof(Length))]
        public long Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        private DateTime _creationTime;

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            set
            {
                _creationTime = value;
            }
        }

        private SearchState _state;

        [DataMember(Name = nameof(State))]
        public SearchState State
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

        private object _model;

        [DataMember(Name = nameof(Model))]
        public object Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
            }
        }

        public PublishListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SearchInfo))]
    public class SearchInfo : ICloneable<SearchInfo>
    {
        public SearchInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private bool _isExpanded;

        [DataMember(Name = nameof(IsExpanded))]
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
            }
        }

        private bool _isUpdated;

        [DataMember(Name = nameof(IsUpdated))]
        public bool IsUpdated
        {
            get
            {
                return _isUpdated;
            }
            set
            {
                _isUpdated = value;
            }
        }

        private SearchConditionsInfo _conditions;

        [DataMember(Name = nameof(Conditions))]
        public SearchConditionsInfo Conditions
        {
            get
            {
                if (_conditions == null)
                    _conditions = new SearchConditionsInfo();

                return _conditions;
            }
        }

        private ObservableCollection<SearchInfo> _children;

        [DataMember(Name = nameof(Children))]
        public ObservableCollection<SearchInfo> Children
        {
            get
            {
                if (_children == null)
                    _children = new ObservableCollection<SearchInfo>();

                return _children;
            }
        }

        public SearchInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SearchConditionsInfo))]
    public class SearchConditionsInfo : ICloneable<SearchConditionsInfo>
    {
        public SearchConditionsInfo() { }

        private ObservableCollection<SearchCondition<string>> _searchNames;

        [DataMember(Name = nameof(SearchNames))]
        public ObservableCollection<SearchCondition<string>> SearchNames
        {
            get
            {
                if (_searchNames == null)
                    _searchNames = new ObservableCollection<SearchCondition<string>>();

                return _searchNames;
            }
        }

        private ObservableCollection<SearchCondition<SearchRegex>> _searchRegexes;

        [DataMember(Name = nameof(SearchRegexes))]
        public ObservableCollection<SearchCondition<SearchRegex>> SearchRegexes
        {
            get
            {
                if (_searchRegexes == null)
                    _searchRegexes = new ObservableCollection<SearchCondition<SearchRegex>>();

                return _searchRegexes;
            }
        }

        private ObservableCollection<SearchCondition<Signature>> _searchSignatures;

        [DataMember(Name = nameof(SearchSignatures))]
        public ObservableCollection<SearchCondition<Signature>> SearchSignatures
        {
            get
            {
                if (_searchSignatures == null)
                    _searchSignatures = new ObservableCollection<SearchCondition<Signature>>();

                return _searchSignatures;
            }
        }

        private ObservableCollection<SearchCondition<SearchRange<DateTime>>> _searchCreationTimeRanges;

        [DataMember(Name = nameof(SearchCreationTimeRanges))]
        public ObservableCollection<SearchCondition<SearchRange<DateTime>>> SearchCreationTimeRanges
        {
            get
            {
                if (_searchCreationTimeRanges == null)
                    _searchCreationTimeRanges = new ObservableCollection<SearchCondition<SearchRange<DateTime>>>();

                return _searchCreationTimeRanges;
            }
        }

        private ObservableCollection<SearchCondition<SearchRange<long>>> _searchLengthRanges;

        [DataMember(Name = nameof(SearchLengthRanges))]
        public ObservableCollection<SearchCondition<SearchRange<long>>> SearchLengthRanges
        {
            get
            {
                if (_searchLengthRanges == null)
                    _searchLengthRanges = new ObservableCollection<SearchCondition<SearchRange<long>>>();

                return _searchLengthRanges;
            }
        }

        private ObservableCollection<SearchCondition<SearchState>> _searchStates;

        [DataMember(Name = nameof(SearchStates))]
        public ObservableCollection<SearchCondition<SearchState>> SearchStates
        {
            get
            {
                if (_searchStates == null)
                    _searchStates = new ObservableCollection<SearchCondition<SearchState>>();

                return _searchStates;
            }
        }

        public SearchConditionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SearchListViewItemInfo))]
    public class SearchListViewItemInfo : ICloneable<SearchListViewItemInfo>
    {
        public SearchListViewItemInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private Signature _signature;

        [DataMember(Name = nameof(Signature))]
        public Signature Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                _signature = value;
            }
        }

        private long _length;

        [DataMember(Name = nameof(Length))]
        public long Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        private DateTime _creationTime;

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            set
            {
                _creationTime = value;
            }
        }

        private SearchState _state;

        [DataMember(Name = nameof(State))]
        public SearchState State
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

        private Seed _model;

        [DataMember(Name = nameof(Model))]
        public Seed Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
            }
        }

        public SearchListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DownloadListViewItemInfo))]
    public class DownloadListViewItemInfo : ICloneable<DownloadListViewItemInfo>
    {
        public DownloadListViewItemInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private long _length;

        [DataMember(Name = nameof(Length))]
        public long Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        private DateTime _creationTime;

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            set
            {
                _creationTime = value;
            }
        }

        private DownloadListViewItemRateInfo _rate;

        [DataMember(Name = nameof(Rate))]
        public DownloadListViewItemRateInfo Rate
        {
            get
            {
                if (_rate == null)
                    _rate = new DownloadListViewItemRateInfo();

                return _rate;
            }
        }

        private DownloadState _state;

        [DataMember(Name = nameof(State))]
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

        private string _path;

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

        private DownloadItemInfo _model;

        [DataMember(Name = nameof(Model))]
        public DownloadItemInfo Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
            }
        }

        public DownloadListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DownloadListViewItemRateInfo))]
    public class DownloadListViewItemRateInfo : ICloneable<DownloadListViewItemRateInfo>
    {
        public DownloadListViewItemRateInfo() { }

        private string _text;

        [DataMember(Name = nameof(Text))]
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }

        private int _depth;

        [DataMember(Name = nameof(Depth))]
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

        private double _value;

        [DataMember(Name = nameof(Value))]
        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public DownloadListViewItemRateInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadListViewItemInfo))]
    public class UploadListViewItemInfo : ICloneable<UploadListViewItemInfo>
    {
        public UploadListViewItemInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private long _length;

        [DataMember(Name = nameof(Length))]
        public long Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        private DateTime _creationTime;

        [DataMember(Name = nameof(CreationTime))]
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
            set
            {
                _creationTime = value;
            }
        }

        private string _path;

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

        private Seed _seed;

        [DataMember(Name = nameof(Seed))]
        public Seed Seed
        {
            get
            {
                return _seed;
            }
            set
            {
                _seed = value;
            }
        }

        public UploadListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadPreviewListViewItemInfo))]
    public class UploadPreviewListViewItemInfo : ICloneable<UploadPreviewListViewItemInfo>
    {
        public UploadPreviewListViewItemInfo() { }

        private string _name;

        [DataMember(Name = nameof(Name))]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private long _length;

        [DataMember(Name = nameof(Length))]
        public long Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        private string _path;

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

        public UploadPreviewListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

}
