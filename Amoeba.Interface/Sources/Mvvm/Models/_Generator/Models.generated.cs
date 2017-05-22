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

        public ServiceOptionsInfo Clone()
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

        public ServiceTcpOptionsInfo Clone()
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

        public ServiceAccountOptionsInfo Clone()
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

        private ObservableCollection<ChatInfo> _chatInfos;

        [DataMember(Name = nameof(ChatInfos))]
        public ObservableCollection<ChatInfo> ChatInfos
        {
            get
            {
                if (_chatInfos == null)
                    _chatInfos = new ObservableCollection<ChatInfo>();

                return _chatInfos;
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

    [DataContract(Name = nameof(ChatInfo))]
    partial class ChatInfo : INotifyPropertyChanged, ICloneable<ChatInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ChatInfo() { }

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

        public ChatInfo Clone()
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

}
