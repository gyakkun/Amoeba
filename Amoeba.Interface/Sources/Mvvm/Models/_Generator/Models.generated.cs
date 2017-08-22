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
using System.Windows.Media.Imaging;
using Omnius;
using Omnius.Collections;
using Omnius.Security;
using Omnius.Net.Amoeba;

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

    [DataContract(Name = nameof(UpdateInfo))]
    partial class UpdateInfo : INotifyPropertyChanged, ICloneable<UpdateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    this.OnPropertyChanged(nameof(IsEnabled));
                }
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
                if (_signature != value)
                {
                    _signature = value;
                    this.OnPropertyChanged(nameof(Signature));
                }
            }
        }

        public UpdateInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ViewInfo))]
    partial class ViewInfo : INotifyPropertyChanged, ICloneable<ViewInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ViewInfo() { }

        private ViewColorsInfo _colors;

        [DataMember(Name = nameof(Colors))]
        public ViewColorsInfo Colors
        {
            get
            {
                if (_colors == null)
                    _colors = new ViewColorsInfo();

                return _colors;
            }
        }

        public ViewInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ViewColorsInfo))]
    partial class ViewColorsInfo : INotifyPropertyChanged, ICloneable<ViewColorsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ViewColorsInfo() { }

        private string _tree_Hit;

        [DataMember(Name = nameof(Tree_Hit))]
        public string Tree_Hit
        {
            get
            {
                return _tree_Hit;
            }
            set
            {
                if (_tree_Hit != value)
                {
                    _tree_Hit = value;
                    this.OnPropertyChanged(nameof(Tree_Hit));
                }
            }
        }

        private string _link_New;

        [DataMember(Name = nameof(Link_New))]
        public string Link_New
        {
            get
            {
                return _link_New;
            }
            set
            {
                if (_link_New != value)
                {
                    _link_New = value;
                    this.OnPropertyChanged(nameof(Link_New));
                }
            }
        }

        private string _link_Visited;

        [DataMember(Name = nameof(Link_Visited))]
        public string Link_Visited
        {
            get
            {
                return _link_Visited;
            }
            set
            {
                if (_link_Visited != value)
                {
                    _link_Visited = value;
                    this.OnPropertyChanged(nameof(Link_Visited));
                }
            }
        }

        private string _message_Trust;

        [DataMember(Name = nameof(Message_Trust))]
        public string Message_Trust
        {
            get
            {
                return _message_Trust;
            }
            set
            {
                if (_message_Trust != value)
                {
                    _message_Trust = value;
                    this.OnPropertyChanged(nameof(Message_Trust));
                }
            }
        }

        private string _message_Untrust;

        [DataMember(Name = nameof(Message_Untrust))]
        public string Message_Untrust
        {
            get
            {
                return _message_Untrust;
            }
            set
            {
                if (_message_Untrust != value)
                {
                    _message_Untrust = value;
                    this.OnPropertyChanged(nameof(Message_Untrust));
                }
            }
        }

        public ViewColorsInfo Clone()
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

    [DataContract(Name = nameof(RelationSignatureInfo))]
    partial class RelationSignatureInfo : INotifyPropertyChanged, ICloneable<RelationSignatureInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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

    [DataContract(Name = nameof(OptionsInfo))]
    partial class OptionsInfo : INotifyPropertyChanged, ICloneable<OptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class AccountOptionsInfo : INotifyPropertyChanged, ICloneable<AccountOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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

        public AccountOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ConnectionOptionsInfo))]
    partial class ConnectionOptionsInfo : INotifyPropertyChanged, ICloneable<ConnectionOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class TcpOptionsInfo : INotifyPropertyChanged, ICloneable<TcpOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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

        public TcpOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(I2pOptionsInfo))]
    partial class I2pOptionsInfo : INotifyPropertyChanged, ICloneable<I2pOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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

        public I2pOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(CustomOptionsInfo))]
    partial class CustomOptionsInfo : INotifyPropertyChanged, ICloneable<CustomOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class BandwidthOptionsInfo : INotifyPropertyChanged, ICloneable<BandwidthOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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

        public BandwidthOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DataOptionsInfo))]
    partial class DataOptionsInfo : INotifyPropertyChanged, ICloneable<DataOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class CacheOptionsInfo : INotifyPropertyChanged, ICloneable<CacheOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_size != value)
                {
                    _size = value;
                    this.OnPropertyChanged(nameof(Size));
                }
            }
        }

        public CacheOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DownloadOptionsInfo))]
    partial class DownloadOptionsInfo : INotifyPropertyChanged, ICloneable<DownloadOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_directoryPath != value)
                {
                    _directoryPath = value;
                    this.OnPropertyChanged(nameof(DirectoryPath));
                }
            }
        }

        public DownloadOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ViewOptionsInfo))]
    partial class ViewOptionsInfo : INotifyPropertyChanged, ICloneable<ViewOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class SubscribeOptionsInfo : INotifyPropertyChanged, ICloneable<SubscribeOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class UpdateOptionsInfo : INotifyPropertyChanged, ICloneable<UpdateOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    this.OnPropertyChanged(nameof(IsEnabled));
                }
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
                if (_signature != value)
                {
                    _signature = value;
                    this.OnPropertyChanged(nameof(Signature));
                }
            }
        }

        public UpdateOptionsInfo Clone()
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

    [DataContract(Name = nameof(SubscribeListViewItemInfo))]
    partial class SubscribeListViewItemInfo : INotifyPropertyChanged, ICloneable<SubscribeListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SubscribeListViewItemInfo() { }

        private BitmapSource _icon;

        [DataMember(Name = nameof(Icon))]
        public BitmapSource Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    this.OnPropertyChanged(nameof(Icon));
                }
            }
        }

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
                if (_state != value)
                {
                    _state = value;
                    this.OnPropertyChanged(nameof(State));
                }
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
                if (_model != value)
                {
                    _model = value;
                    this.OnPropertyChanged(nameof(Model));
                }
            }
        }

        public SubscribeListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(PublishStoreInfo))]
    partial class PublishStoreInfo : INotifyPropertyChanged, ICloneable<PublishStoreInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class PublishBoxInfo : INotifyPropertyChanged, ICloneable<PublishBoxInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class PublishListViewItemInfo : INotifyPropertyChanged, ICloneable<PublishListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public PublishListViewItemInfo() { }

        private BitmapSource _icon;

        [DataMember(Name = nameof(Icon))]
        public BitmapSource Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    this.OnPropertyChanged(nameof(Icon));
                }
            }
        }

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
                if (_state != value)
                {
                    _state = value;
                    this.OnPropertyChanged(nameof(State));
                }
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
                if (_model != value)
                {
                    _model = value;
                    this.OnPropertyChanged(nameof(Model));
                }
            }
        }

        public PublishListViewItemInfo Clone()
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
    partial class SearchConditionsInfo : INotifyPropertyChanged, ICloneable<SearchConditionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
    partial class SearchListViewItemInfo : INotifyPropertyChanged, ICloneable<SearchListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SearchListViewItemInfo() { }

        private BitmapSource _icon;

        [DataMember(Name = nameof(Icon))]
        public BitmapSource Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    this.OnPropertyChanged(nameof(Icon));
                }
            }
        }

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
                if (_signature != value)
                {
                    _signature = value;
                    this.OnPropertyChanged(nameof(Signature));
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
                if (_state != value)
                {
                    _state = value;
                    this.OnPropertyChanged(nameof(State));
                }
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
                if (_model != value)
                {
                    _model = value;
                    this.OnPropertyChanged(nameof(Model));
                }
            }
        }

        public SearchListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DownloadListViewItemInfo))]
    partial class DownloadListViewItemInfo : INotifyPropertyChanged, ICloneable<DownloadListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public DownloadListViewItemInfo() { }

        private BitmapSource _icon;

        [DataMember(Name = nameof(Icon))]
        public BitmapSource Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    this.OnPropertyChanged(nameof(Icon));
                }
            }
        }

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
                if (_state != value)
                {
                    _state = value;
                    this.OnPropertyChanged(nameof(State));
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
                if (_model != value)
                {
                    _model = value;
                    this.OnPropertyChanged(nameof(Model));
                }
            }
        }

        public DownloadListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DownloadListViewItemRateInfo))]
    partial class DownloadListViewItemRateInfo : INotifyPropertyChanged, ICloneable<DownloadListViewItemRateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
                if (_text != value)
                {
                    _text = value;
                    this.OnPropertyChanged(nameof(Text));
                }
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
                if (_depth != value)
                {
                    _depth = value;
                    this.OnPropertyChanged(nameof(Depth));
                }
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
                if (_value != value)
                {
                    _value = value;
                    this.OnPropertyChanged(nameof(Value));
                }
            }
        }

        public DownloadListViewItemRateInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadListViewItemInfo))]
    partial class UploadListViewItemInfo : INotifyPropertyChanged, ICloneable<UploadListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public UploadListViewItemInfo() { }

        private BitmapSource _icon;

        [DataMember(Name = nameof(Icon))]
        public BitmapSource Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    this.OnPropertyChanged(nameof(Icon));
                }
            }
        }

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
                if (_seed != value)
                {
                    _seed = value;
                    this.OnPropertyChanged(nameof(Seed));
                }
            }
        }

        public UploadListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadPreviewListViewItemInfo))]
    partial class UploadPreviewListViewItemInfo : INotifyPropertyChanged, ICloneable<UploadPreviewListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public UploadPreviewListViewItemInfo() { }

        private BitmapSource _icon;

        [DataMember(Name = nameof(Icon))]
        public BitmapSource Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    this.OnPropertyChanged(nameof(Icon));
                }
            }
        }

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

        public UploadPreviewListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

}
