using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.Serialization;
using Omnius.Base;
using System.Collections.ObjectModel;
using System.Windows;
using Amoeba.Service;
using Omnius;
using Omnius.Collections;
using Omnius.Security;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(AccountInfo))]
    partial class AccountInfo : INotifyPropertyChanged, ICloneable<AccountInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_digitalSignature != value)
                {
                    _digitalSignature = value;
                    this.OnPropertyChanged(nameof(DigitalSignature));
                }
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
                if (_comment != value)
                {
                    _comment = value;
                    this.OnPropertyChanged(nameof(Comment));
                }
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
                if (_exchange != value)
                {
                    _exchange = value;
                    this.OnPropertyChanged(nameof(Exchange));
                }
            }
        }

        private ObservableCollection<Omnius.Security.Signature> _trustSignatures;

        [DataMember(Name = nameof(TrustSignatures))]
        public ObservableCollection<Omnius.Security.Signature> TrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new ObservableCollection<Omnius.Security.Signature>();

                return _trustSignatures;
            }
        }

        private ObservableCollection<Omnius.Security.Signature> _untrustSignatures;

        [DataMember(Name = nameof(UntrustSignatures))]
        public ObservableCollection<Omnius.Security.Signature> UntrustSignatures
        {
            get
            {
                if (_untrustSignatures == null)
                    _untrustSignatures = new ObservableCollection<Omnius.Security.Signature>();

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

    [DataContract(Name = nameof(PublishDirectoryInfo))]
    partial class PublishDirectoryInfo : INotifyPropertyChanged, ICloneable<PublishDirectoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public PublishDirectoryInfo() { }

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
                if (_name != value)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
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
                if (_path != value)
                {
                    _path = value;
                    this.OnPropertyChanged(nameof(Path));
                }
            }
        }

        public PublishDirectoryInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ListSortInfo))]
    partial class ListSortInfo : INotifyPropertyChanged, ICloneable<ListSortInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ListSortInfo() { }

        private string _propertyName;

        [DataMember(Name = nameof(PropertyName))]
        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
            set
            {
                if (_propertyName != value)
                {
                    _propertyName = value;
                    this.OnPropertyChanged(nameof(PropertyName));
                }
            }
        }

        private ListSortDirection _direction;

        [DataMember(Name = nameof(Direction))]
        public ListSortDirection Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    this.OnPropertyChanged(nameof(Direction));
                }
            }
        }

        public ListSortInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(CloudStateInfo))]
    partial class CloudStateInfo : INotifyPropertyChanged, ICloneable<CloudStateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_location != value)
                {
                    _location = value;
                    this.OnPropertyChanged(nameof(Location));
                }
            }
        }

        public CloudStateInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(OptionsInfo))]
    partial class OptionsInfo : INotifyPropertyChanged, ICloneable<OptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public OptionsInfo() { }

        private OptionsAccountInfo _account;

        [DataMember(Name = nameof(Account))]
        public OptionsAccountInfo Account
        {
            get
            {
                if (_account == null)
                    _account = new OptionsAccountInfo();

                return _account;
            }
        }

        private OptionsSubscribeInfo _subscribe;

        [DataMember(Name = nameof(Subscribe))]
        public OptionsSubscribeInfo Subscribe
        {
            get
            {
                if (_subscribe == null)
                    _subscribe = new OptionsSubscribeInfo();

                return _subscribe;
            }
        }

        private OptionsTcpInfo _tcp;

        [DataMember(Name = nameof(Tcp))]
        public OptionsTcpInfo Tcp
        {
            get
            {
                if (_tcp == null)
                    _tcp = new OptionsTcpInfo();

                return _tcp;
            }
        }

        private OptionsI2pInfo _i2p;

        [DataMember(Name = nameof(I2p))]
        public OptionsI2pInfo I2p
        {
            get
            {
                if (_i2p == null)
                    _i2p = new OptionsI2pInfo();

                return _i2p;
            }
        }

        private OptionsBandwidthInfo _bandwidth;

        [DataMember(Name = nameof(Bandwidth))]
        public OptionsBandwidthInfo Bandwidth
        {
            get
            {
                if (_bandwidth == null)
                    _bandwidth = new OptionsBandwidthInfo();

                return _bandwidth;
            }
        }

        private OptionsDataInfo _data;

        [DataMember(Name = nameof(Data))]
        public OptionsDataInfo Data
        {
            get
            {
                if (_data == null)
                    _data = new OptionsDataInfo();

                return _data;
            }
        }

        public OptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(OptionsAccountInfo))]
    partial class OptionsAccountInfo : INotifyPropertyChanged, ICloneable<OptionsAccountInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public OptionsAccountInfo() { }

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
                if (_digitalSignature != value)
                {
                    _digitalSignature = value;
                    this.OnPropertyChanged(nameof(DigitalSignature));
                }
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
                if (_comment != value)
                {
                    _comment = value;
                    this.OnPropertyChanged(nameof(Comment));
                }
            }
        }

        private ObservableCollection<Omnius.Security.Signature> _trustSignatures;

        [DataMember(Name = nameof(TrustSignatures))]
        public ObservableCollection<Omnius.Security.Signature> TrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new ObservableCollection<Omnius.Security.Signature>();

                return _trustSignatures;
            }
        }

        private ObservableCollection<Omnius.Security.Signature> _untrustSignatures;

        [DataMember(Name = nameof(UntrustSignatures))]
        public ObservableCollection<Omnius.Security.Signature> UntrustSignatures
        {
            get
            {
                if (_untrustSignatures == null)
                    _untrustSignatures = new ObservableCollection<Omnius.Security.Signature>();

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

        public OptionsAccountInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(OptionsSubscribeInfo))]
    partial class OptionsSubscribeInfo : INotifyPropertyChanged, ICloneable<OptionsSubscribeInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public OptionsSubscribeInfo() { }

        private ObservableCollection<Omnius.Security.Signature> _subscribeSignatures;

        [DataMember(Name = nameof(SubscribeSignatures))]
        public ObservableCollection<Omnius.Security.Signature> SubscribeSignatures
        {
            get
            {
                if (_subscribeSignatures == null)
                    _subscribeSignatures = new ObservableCollection<Omnius.Security.Signature>();

                return _subscribeSignatures;
            }
        }

        public OptionsSubscribeInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(OptionsTcpInfo))]
    partial class OptionsTcpInfo : INotifyPropertyChanged, ICloneable<OptionsTcpInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public OptionsTcpInfo() { }

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
                if (_ipv4IsEnabled != value)
                {
                    _ipv4IsEnabled = value;
                    this.OnPropertyChanged(nameof(Ipv4IsEnabled));
                }
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
                if (_ipv4Port != value)
                {
                    _ipv4Port = value;
                    this.OnPropertyChanged(nameof(Ipv4Port));
                }
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
                if (_ipv6IsEnabled != value)
                {
                    _ipv6IsEnabled = value;
                    this.OnPropertyChanged(nameof(Ipv6IsEnabled));
                }
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
                if (_ipv6Port != value)
                {
                    _ipv6Port = value;
                    this.OnPropertyChanged(nameof(Ipv6Port));
                }
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
                if (_proxyUri != value)
                {
                    _proxyUri = value;
                    this.OnPropertyChanged(nameof(ProxyUri));
                }
            }
        }

        public OptionsTcpInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(OptionsI2pInfo))]
    partial class OptionsI2pInfo : INotifyPropertyChanged, ICloneable<OptionsI2pInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public OptionsI2pInfo() { }

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
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    this.OnPropertyChanged(nameof(IsEnabled));
                }
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
                if (_samBridgeUri != value)
                {
                    _samBridgeUri = value;
                    this.OnPropertyChanged(nameof(SamBridgeUri));
                }
            }
        }

        public OptionsI2pInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(OptionsBandwidthInfo))]
    partial class OptionsBandwidthInfo : INotifyPropertyChanged, ICloneable<OptionsBandwidthInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public OptionsBandwidthInfo() { }

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
                if (_connectionCountLimit != value)
                {
                    _connectionCountLimit = value;
                    this.OnPropertyChanged(nameof(ConnectionCountLimit));
                }
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
                if (_bandwidthLimit != value)
                {
                    _bandwidthLimit = value;
                    this.OnPropertyChanged(nameof(BandwidthLimit));
                }
            }
        }

        public OptionsBandwidthInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(OptionsDataInfo))]
    partial class OptionsDataInfo : INotifyPropertyChanged, ICloneable<OptionsDataInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public OptionsDataInfo() { }

        private long _cacheSize;

        [DataMember(Name = nameof(CacheSize))]
        public long CacheSize
        {
            get
            {
                return _cacheSize;
            }
            set
            {
                if (_cacheSize != value)
                {
                    _cacheSize = value;
                    this.OnPropertyChanged(nameof(CacheSize));
                }
            }
        }

        private string _downloadDirectoryPath;

        [DataMember(Name = nameof(DownloadDirectoryPath))]
        public string DownloadDirectoryPath
        {
            get
            {
                return _downloadDirectoryPath;
            }
            set
            {
                if (_downloadDirectoryPath != value)
                {
                    _downloadDirectoryPath = value;
                    this.OnPropertyChanged(nameof(DownloadDirectoryPath));
                }
            }
        }

        public OptionsDataInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(RelationSignatureInfo))]
    partial class RelationSignatureInfo : INotifyPropertyChanged, ICloneable<RelationSignatureInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public RelationSignatureInfo() { }

        private Omnius.Security.Signature _signature;

        [DataMember(Name = nameof(Signature))]
        public Signature Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                if (_signature != value)
                {
                    _signature = value;
                    this.OnPropertyChanged(nameof(Signature));
                }
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
                if (_profile != value)
                {
                    _profile = value;
                    this.OnPropertyChanged(nameof(Profile));
                }
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

    [DataContract(Name = nameof(ChatCategoryInfo))]
    partial class ChatCategoryInfo : INotifyPropertyChanged, ICloneable<ChatCategoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_name != value)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
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
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
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
    partial class ChatThreadInfo : INotifyPropertyChanged, ICloneable<ChatThreadInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ChatThreadInfo() { }

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
                if (_tag != value)
                {
                    _tag = value;
                    this.OnPropertyChanged(nameof(Tag));
                }
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
    partial class ChatMessageInfo : INotifyPropertyChanged, ICloneable<ChatMessageInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_state != value)
                {
                    _state = value;
                    this.OnPropertyChanged(nameof(State));
                }
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
                if (_message != value)
                {
                    _message = value;
                    this.OnPropertyChanged(nameof(Message));
                }
            }
        }

        public ChatMessageInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SearchInfo))]
    partial class SearchInfo : INotifyPropertyChanged, ICloneable<SearchInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_name != value)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
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
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
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
                if (_isUpdated != value)
                {
                    _isUpdated = value;
                    this.OnPropertyChanged(nameof(IsUpdated));
                }
            }
        }

        private SearchCondition _condition;

        [DataMember(Name = nameof(Condition))]
        public SearchCondition Condition
        {
            get
            {
                if (_condition == null)
                    _condition = new SearchCondition();

                return _condition;
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

    [DataContract(Name = nameof(SearchCondition))]
    partial class SearchCondition : INotifyPropertyChanged, ICloneable<SearchCondition>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SearchCondition() { }

        private ObservableCollection<SearchContains<string>> _searchNames;

        [DataMember(Name = nameof(SearchNames))]
        public ObservableCollection<SearchContains<string>> SearchNames
        {
            get
            {
                if (_searchNames == null)
                    _searchNames = new ObservableCollection<SearchContains<string>>();

                return _searchNames;
            }
        }

        private ObservableCollection<SearchContains<SearchRegex>> _searchRegexes;

        [DataMember(Name = nameof(SearchRegexes))]
        public ObservableCollection<SearchContains<SearchRegex>> SearchRegexes
        {
            get
            {
                if (_searchRegexes == null)
                    _searchRegexes = new ObservableCollection<SearchContains<SearchRegex>>();

                return _searchRegexes;
            }
        }

        private ObservableCollection<SearchContains<Omnius.Security.Signature>> _searchSignatures;

        [DataMember(Name = nameof(SearchSignatures))]
        public ObservableCollection<SearchContains<Omnius.Security.Signature>> SearchSignatures
        {
            get
            {
                if (_searchSignatures == null)
                    _searchSignatures = new ObservableCollection<SearchContains<Omnius.Security.Signature>>();

                return _searchSignatures;
            }
        }

        private ObservableCollection<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRanges;

        [DataMember(Name = nameof(SearchCreationTimeRanges))]
        public ObservableCollection<SearchContains<SearchRange<DateTime>>> SearchCreationTimeRanges
        {
            get
            {
                if (_searchCreationTimeRanges == null)
                    _searchCreationTimeRanges = new ObservableCollection<SearchContains<SearchRange<DateTime>>>();

                return _searchCreationTimeRanges;
            }
        }

        private ObservableCollection<SearchContains<SearchRange<long>>> _searchLengthRanges;

        [DataMember(Name = nameof(SearchLengthRanges))]
        public ObservableCollection<SearchContains<SearchRange<long>>> SearchLengthRanges
        {
            get
            {
                if (_searchLengthRanges == null)
                    _searchLengthRanges = new ObservableCollection<SearchContains<SearchRange<long>>>();

                return _searchLengthRanges;
            }
        }

        private ObservableCollection<SearchContains<Metadata>> _searchMetadatas;

        [DataMember(Name = nameof(SearchMetadatas))]
        public ObservableCollection<SearchContains<Metadata>> SearchMetadatas
        {
            get
            {
                if (_searchMetadatas == null)
                    _searchMetadatas = new ObservableCollection<SearchContains<Metadata>>();

                return _searchMetadatas;
            }
        }

        private ObservableCollection<SearchContains<SearchState>> _searchStates;

        [DataMember(Name = nameof(SearchStates))]
        public ObservableCollection<SearchContains<SearchState>> SearchStates
        {
            get
            {
                if (_searchStates == null)
                    _searchStates = new ObservableCollection<SearchContains<SearchState>>();

                return _searchStates;
            }
        }

        public SearchCondition Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SubscribeCategoryInfo))]
    partial class SubscribeCategoryInfo : INotifyPropertyChanged, ICloneable<SubscribeCategoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_name != value)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
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
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
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
    partial class SubscribeStoreInfo : INotifyPropertyChanged, ICloneable<SubscribeStoreInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SubscribeStoreInfo() { }

        private Omnius.Security.Signature _authorSignature;

        [DataMember(Name = nameof(AuthorSignature))]
        public Signature AuthorSignature
        {
            get
            {
                return _authorSignature;
            }
            set
            {
                if (_authorSignature != value)
                {
                    _authorSignature = value;
                    this.OnPropertyChanged(nameof(AuthorSignature));
                }
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
                if (_creationTime != value)
                {
                    _creationTime = value;
                    this.OnPropertyChanged(nameof(CreationTime));
                }
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
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
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
                if (_isUpdated != value)
                {
                    _isUpdated = value;
                    this.OnPropertyChanged(nameof(IsUpdated));
                }
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
    partial class SubscribeBoxInfo : INotifyPropertyChanged, ICloneable<SubscribeBoxInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_name != value)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
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
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
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

    [DataContract(Name = nameof(PublishPreviewBoxInfo))]
    partial class PublishPreviewBoxInfo : INotifyPropertyChanged, ICloneable<PublishPreviewBoxInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public PublishPreviewBoxInfo() { }

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
                if (_name != value)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
            }
        }

        private ObservableCollection<PublishPreviewSeedInfo> _seedInfos;

        [DataMember(Name = nameof(SeedInfos))]
        public ObservableCollection<PublishPreviewSeedInfo> SeedInfos
        {
            get
            {
                if (_seedInfos == null)
                    _seedInfos = new ObservableCollection<PublishPreviewSeedInfo>();

                return _seedInfos;
            }
        }

        private ObservableCollection<PublishPreviewBoxInfo> _boxInfos;

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<PublishPreviewBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<PublishPreviewBoxInfo>();

                return _boxInfos;
            }
        }

        public PublishPreviewBoxInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(PublishPreviewSeedInfo))]
    partial class PublishPreviewSeedInfo : INotifyPropertyChanged, ICloneable<PublishPreviewSeedInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public PublishPreviewSeedInfo() { }

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
                if (_name != value)
                {
                    _name = value;
                    this.OnPropertyChanged(nameof(Name));
                }
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
                if (_length != value)
                {
                    _length = value;
                    this.OnPropertyChanged(nameof(Length));
                }
            }
        }

        public PublishPreviewSeedInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

}
