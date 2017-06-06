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

    [DataContract(Name = nameof(CrowdStateInfo))]
    partial class CrowdStateInfo : INotifyPropertyChanged, ICloneable<CrowdStateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public CrowdStateInfo() { }

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

        public CrowdStateInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ServiceOptionsInfo))]
    partial class ServiceOptionsInfo : INotifyPropertyChanged, ICloneable<ServiceOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ServiceOptionsInfo() { }

        private ServiceAccountOptionsInfo _account;

        [DataMember(Name = nameof(Account))]
        public ServiceAccountOptionsInfo Account
        {
            get
            {
                if (_account == null)
                    _account = new ServiceAccountOptionsInfo();

                return _account;
            }
        }

        private ServiceTcpOptionsInfo _tcp;

        [DataMember(Name = nameof(Tcp))]
        public ServiceTcpOptionsInfo Tcp
        {
            get
            {
                if (_tcp == null)
                    _tcp = new ServiceTcpOptionsInfo();

                return _tcp;
            }
        }

        private ServiceI2pOptionsInfo _i2p;

        [DataMember(Name = nameof(I2p))]
        public ServiceI2pOptionsInfo I2p
        {
            get
            {
                if (_i2p == null)
                    _i2p = new ServiceI2pOptionsInfo();

                return _i2p;
            }
        }

        private ServiceBandwidthOptionsInfo _bandwidth;

        [DataMember(Name = nameof(Bandwidth))]
        public ServiceBandwidthOptionsInfo Bandwidth
        {
            get
            {
                if (_bandwidth == null)
                    _bandwidth = new ServiceBandwidthOptionsInfo();

                return _bandwidth;
            }
        }

        private ServiceDataOptionsInfo _data;

        [DataMember(Name = nameof(Data))]
        public ServiceDataOptionsInfo Data
        {
            get
            {
                if (_data == null)
                    _data = new ServiceDataOptionsInfo();

                return _data;
            }
        }

        public ServiceOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ServiceAccountOptionsInfo))]
    partial class ServiceAccountOptionsInfo : INotifyPropertyChanged, ICloneable<ServiceAccountOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ServiceAccountOptionsInfo() { }

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

        public ServiceAccountOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ServiceTcpOptionsInfo))]
    partial class ServiceTcpOptionsInfo : INotifyPropertyChanged, ICloneable<ServiceTcpOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ServiceTcpOptionsInfo() { }

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

        public ServiceTcpOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ServiceI2pOptionsInfo))]
    partial class ServiceI2pOptionsInfo : INotifyPropertyChanged, ICloneable<ServiceI2pOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ServiceI2pOptionsInfo() { }

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

        public ServiceI2pOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ServiceBandwidthOptionsInfo))]
    partial class ServiceBandwidthOptionsInfo : INotifyPropertyChanged, ICloneable<ServiceBandwidthOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ServiceBandwidthOptionsInfo() { }

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

        public ServiceBandwidthOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ServiceDataOptionsInfo))]
    partial class ServiceDataOptionsInfo : INotifyPropertyChanged, ICloneable<ServiceDataOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ServiceDataOptionsInfo() { }

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

        public ServiceDataOptionsInfo Clone()
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

        private ObservableCollection<SearchContains<Signature>> _searchSignatures;

        [DataMember(Name = nameof(SearchSignatures))]
        public ObservableCollection<SearchContains<Signature>> SearchSignatures
        {
            get
            {
                if (_searchSignatures == null)
                    _searchSignatures = new ObservableCollection<SearchContains<Signature>>();

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
