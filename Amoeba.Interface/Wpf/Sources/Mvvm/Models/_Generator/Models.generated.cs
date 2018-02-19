using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media.Imaging;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Security;
using Amoeba.Messages;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(AccountInfo))]
    sealed partial class AccountInfo : INotifyPropertyChanged, ICloneable<AccountInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private DigitalSignature _digitalSignature;
        private string _comment;
        private Exchange _exchange;
        private ObservableCollection<Signature> _trustSignatures;
        private ObservableCollection<Signature> _untrustSignatures;
        private ObservableCollection<Tag> _tags;

        public AccountInfo() { }

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
    sealed partial class UpdateInfo : INotifyPropertyChanged, ICloneable<UpdateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool _isEnabled;
        private Signature _signature;

        public UpdateInfo() { }

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
    sealed partial class ViewInfo : INotifyPropertyChanged, ICloneable<ViewInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ViewColorsInfo _colors;
        private ViewFontsInfo _fonts;

        public ViewInfo() { }

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

        [DataMember(Name = nameof(Fonts))]
        public ViewFontsInfo Fonts
        {
            get
            {
                if (_fonts == null)
                    _fonts = new ViewFontsInfo();

                return _fonts;
            }
        }

        public ViewInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ViewColorsInfo))]
    sealed partial class ViewColorsInfo : INotifyPropertyChanged, ICloneable<ViewColorsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _tree_Hit;
        private string _link_New;
        private string _link_Visited;
        private string _message_Trust;
        private string _message_Untrust;

        public ViewColorsInfo() { }

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

    [DataContract(Name = nameof(ViewFontsInfo))]
    sealed partial class ViewFontsInfo : INotifyPropertyChanged, ICloneable<ViewFontsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private FontInfo _chat_Message;

        public ViewFontsInfo() { }

        [DataMember(Name = nameof(Chat_Message))]
        public FontInfo Chat_Message
        {
            get
            {
                return _chat_Message;
            }
            set
            {
                if (_chat_Message != value)
                {
                    _chat_Message = value;
                    this.OnPropertyChanged(nameof(Chat_Message));
                }
            }
        }

        public ViewFontsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(FontInfo))]
    sealed partial class FontInfo : INotifyPropertyChanged, ICloneable<FontInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _fontFamily;
        private double _fontSize;

        public FontInfo() { }

        [DataMember(Name = nameof(FontFamily))]
        public string FontFamily
        {
            get
            {
                return _fontFamily;
            }
            set
            {
                if (_fontFamily != value)
                {
                    _fontFamily = value;
                    this.OnPropertyChanged(nameof(FontFamily));
                }
            }
        }

        [DataMember(Name = nameof(FontSize))]
        public double FontSize
        {
            get
            {
                return _fontSize;
            }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    this.OnPropertyChanged(nameof(FontSize));
                }
            }
        }

        public FontInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ListSortInfo))]
    sealed partial class ListSortInfo : INotifyPropertyChanged, ICloneable<ListSortInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _propertyName;
        private ListSortDirection _direction;

        public ListSortInfo() { }

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
    sealed partial class RelationSignatureInfo : INotifyPropertyChanged, ICloneable<RelationSignatureInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Signature _signature;
        private BroadcastMessage<Profile> _profile;
        private ObservableCollection<RelationSignatureInfo> _children;

        public RelationSignatureInfo() { }

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
    sealed partial class OptionsInfo : INotifyPropertyChanged, ICloneable<OptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private AccountOptionsInfo _account;
        private ConnectionOptionsInfo _connection;
        private DataOptionsInfo _data;
        private ViewOptionsInfo _view;
        private UpdateOptionsInfo _update;

        public OptionsInfo() { }

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
    sealed partial class AccountOptionsInfo : INotifyPropertyChanged, ICloneable<AccountOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private DigitalSignature _digitalSignature;
        private string _comment;
        private ObservableCollection<Signature> _trustSignatures;
        private ObservableCollection<Signature> _untrustSignatures;
        private ObservableCollection<Tag> _tags;

        public AccountOptionsInfo() { }

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
    sealed partial class ConnectionOptionsInfo : INotifyPropertyChanged, ICloneable<ConnectionOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ConnectionOptionsTcpInfo _tcp;
        private ConnectionOptionsI2pInfo _i2p;
        private ConnectionOptionsCustomInfo _custom;
        private ConnectionOptionsBandwidthInfo _bandwidth;

        public ConnectionOptionsInfo() { }

        [DataMember(Name = nameof(Tcp))]
        public ConnectionOptionsTcpInfo Tcp
        {
            get
            {
                if (_tcp == null)
                    _tcp = new ConnectionOptionsTcpInfo();

                return _tcp;
            }
        }

        [DataMember(Name = nameof(I2p))]
        public ConnectionOptionsI2pInfo I2p
        {
            get
            {
                if (_i2p == null)
                    _i2p = new ConnectionOptionsI2pInfo();

                return _i2p;
            }
        }

        [DataMember(Name = nameof(Custom))]
        public ConnectionOptionsCustomInfo Custom
        {
            get
            {
                if (_custom == null)
                    _custom = new ConnectionOptionsCustomInfo();

                return _custom;
            }
        }

        [DataMember(Name = nameof(Bandwidth))]
        public ConnectionOptionsBandwidthInfo Bandwidth
        {
            get
            {
                if (_bandwidth == null)
                    _bandwidth = new ConnectionOptionsBandwidthInfo();

                return _bandwidth;
            }
        }

        public ConnectionOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ConnectionOptionsTcpInfo))]
    sealed partial class ConnectionOptionsTcpInfo : INotifyPropertyChanged, ICloneable<ConnectionOptionsTcpInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool _ipv4IsEnabled;
        private ushort _ipv4Port;
        private bool _ipv6IsEnabled;
        private ushort _ipv6Port;
        private string _proxyUri;

        public ConnectionOptionsTcpInfo() { }

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

        public ConnectionOptionsTcpInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ConnectionOptionsI2pInfo))]
    sealed partial class ConnectionOptionsI2pInfo : INotifyPropertyChanged, ICloneable<ConnectionOptionsI2pInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool _isEnabled;
        private string _samBridgeUri;

        public ConnectionOptionsI2pInfo() { }

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

        public ConnectionOptionsI2pInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ConnectionOptionsCustomInfo))]
    sealed partial class ConnectionOptionsCustomInfo : INotifyPropertyChanged, ICloneable<ConnectionOptionsCustomInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ObservableCollection<string> _locationUris;
        private ObservableCollection<ConnectionFilter> _connectionFilters;
        private ObservableCollection<string> _listenUris;

        public ConnectionOptionsCustomInfo() { }

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

        public ConnectionOptionsCustomInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ConnectionOptionsBandwidthInfo))]
    sealed partial class ConnectionOptionsBandwidthInfo : INotifyPropertyChanged, ICloneable<ConnectionOptionsBandwidthInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int _connectionCountLimit;
        private int _bandwidthLimit;

        public ConnectionOptionsBandwidthInfo() { }

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

        public ConnectionOptionsBandwidthInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DataOptionsInfo))]
    sealed partial class DataOptionsInfo : INotifyPropertyChanged, ICloneable<DataOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private DataOptionsCacheInfo _cache;
        private DataOptionsDownloadInfo _download;

        public DataOptionsInfo() { }

        [DataMember(Name = nameof(Cache))]
        public DataOptionsCacheInfo Cache
        {
            get
            {
                if (_cache == null)
                    _cache = new DataOptionsCacheInfo();

                return _cache;
            }
        }

        [DataMember(Name = nameof(Download))]
        public DataOptionsDownloadInfo Download
        {
            get
            {
                if (_download == null)
                    _download = new DataOptionsDownloadInfo();

                return _download;
            }
        }

        public DataOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DataOptionsCacheInfo))]
    sealed partial class DataOptionsCacheInfo : INotifyPropertyChanged, ICloneable<DataOptionsCacheInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private long _size;

        public DataOptionsCacheInfo() { }

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

        public DataOptionsCacheInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(DataOptionsDownloadInfo))]
    sealed partial class DataOptionsDownloadInfo : INotifyPropertyChanged, ICloneable<DataOptionsDownloadInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _directoryPath;
        private int _protectedPercentage;

        public DataOptionsDownloadInfo() { }

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

        [DataMember(Name = nameof(ProtectedPercentage))]
        public int ProtectedPercentage
        {
            get
            {
                return _protectedPercentage;
            }
            set
            {
                if (_protectedPercentage != value)
                {
                    _protectedPercentage = value;
                    this.OnPropertyChanged(nameof(ProtectedPercentage));
                }
            }
        }

        public DataOptionsDownloadInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ViewOptionsInfo))]
    sealed partial class ViewOptionsInfo : INotifyPropertyChanged, ICloneable<ViewOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ViewOptionsSubscribeInfo _subscribe;

        public ViewOptionsInfo() { }

        [DataMember(Name = nameof(Subscribe))]
        public ViewOptionsSubscribeInfo Subscribe
        {
            get
            {
                if (_subscribe == null)
                    _subscribe = new ViewOptionsSubscribeInfo();

                return _subscribe;
            }
        }

        public ViewOptionsInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(ViewOptionsSubscribeInfo))]
    sealed partial class ViewOptionsSubscribeInfo : INotifyPropertyChanged, ICloneable<ViewOptionsSubscribeInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ObservableCollection<Signature> _signatures;

        public ViewOptionsSubscribeInfo() { }

        [DataMember(Name = nameof(Signatures))]
        public ObservableCollection<Signature> Signatures
        {
            get
            {
                if (_signatures == null)
                    _signatures = new ObservableCollection<Signature>();

                return _signatures;
            }
        }

        public ViewOptionsSubscribeInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UpdateOptionsInfo))]
    sealed partial class UpdateOptionsInfo : INotifyPropertyChanged, ICloneable<UpdateOptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool _isEnabled;
        private Signature _signature;

        public UpdateOptionsInfo() { }

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
    sealed partial class CloudStateInfo : INotifyPropertyChanged, ICloneable<CloudStateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _location;

        public CloudStateInfo() { }

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
    sealed partial class ChatCategoryInfo : INotifyPropertyChanged, ICloneable<ChatCategoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;
        private bool _isExpanded;
        private ObservableCollection<ChatThreadInfo> _threadInfos;
        private ObservableCollection<ChatCategoryInfo> _categoryInfos;

        public ChatCategoryInfo() { }

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
    sealed partial class ChatThreadInfo : INotifyPropertyChanged, ICloneable<ChatThreadInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool _isUpdated;
        private Tag _tag;
        private bool _isTrustMessageOnly;
        private bool _isNewMessageOnly;
        private LockedList<ChatMessageInfo> _messages;

        public ChatThreadInfo() { }

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

        [DataMember(Name = nameof(IsTrustMessageOnly))]
        public bool IsTrustMessageOnly
        {
            get
            {
                return _isTrustMessageOnly;
            }
            set
            {
                if (_isTrustMessageOnly != value)
                {
                    _isTrustMessageOnly = value;
                    this.OnPropertyChanged(nameof(IsTrustMessageOnly));
                }
            }
        }

        [DataMember(Name = nameof(IsNewMessageOnly))]
        public bool IsNewMessageOnly
        {
            get
            {
                return _isNewMessageOnly;
            }
            set
            {
                if (_isNewMessageOnly != value)
                {
                    _isNewMessageOnly = value;
                    this.OnPropertyChanged(nameof(IsNewMessageOnly));
                }
            }
        }

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
    sealed partial class ChatMessageInfo : INotifyPropertyChanged, ICloneable<ChatMessageInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ChatMessageState _state;
        private MulticastMessage<ChatMessage> _message;

        public ChatMessageInfo() { }

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

    [DataContract(Name = nameof(StoreCategoryInfo))]
    sealed partial class StoreCategoryInfo : INotifyPropertyChanged, ICloneable<StoreCategoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;
        private bool _isExpanded;
        private ObservableCollection<StoreSignatureInfo> _signatureInfos;
        private ObservableCollection<StoreCategoryInfo> _categoryInfos;

        public StoreCategoryInfo() { }

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

        [DataMember(Name = nameof(SignatureInfos))]
        public ObservableCollection<StoreSignatureInfo> SignatureInfos
        {
            get
            {
                if (_signatureInfos == null)
                    _signatureInfos = new ObservableCollection<StoreSignatureInfo>();

                return _signatureInfos;
            }
        }

        [DataMember(Name = nameof(CategoryInfos))]
        public ObservableCollection<StoreCategoryInfo> CategoryInfos
        {
            get
            {
                if (_categoryInfos == null)
                    _categoryInfos = new ObservableCollection<StoreCategoryInfo>();

                return _categoryInfos;
            }
        }

        public StoreCategoryInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(StoreSignatureInfo))]
    sealed partial class StoreSignatureInfo : INotifyPropertyChanged, ICloneable<StoreSignatureInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Signature _authorSignature;
        private DateTime _updateTime;
        private bool _isExpanded;
        private bool _isUpdated;
        private ObservableCollection<StoreBoxInfo> _boxInfos;

        public StoreSignatureInfo() { }

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

        [DataMember(Name = nameof(UpdateTime))]
        public DateTime UpdateTime
        {
            get
            {
                return _updateTime;
            }
            set
            {
                if (_updateTime != value)
                {
                    _updateTime = value;
                    this.OnPropertyChanged(nameof(UpdateTime));
                }
            }
        }

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

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<StoreBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<StoreBoxInfo>();

                return _boxInfos;
            }
        }

        public StoreSignatureInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(StoreBoxInfo))]
    sealed partial class StoreBoxInfo : INotifyPropertyChanged, ICloneable<StoreBoxInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;
        private bool _isExpanded;
        private ObservableCollection<Seed> _seeds;
        private ObservableCollection<StoreBoxInfo> _boxInfos;

        public StoreBoxInfo() { }

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

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<StoreBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<StoreBoxInfo>();

                return _boxInfos;
            }
        }

        public StoreBoxInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(StoreListViewItemInfo))]
    sealed partial class StoreListViewItemInfo : INotifyPropertyChanged, ICloneable<StoreListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private BitmapSource _icon;
        private string _name;
        private long _length;
        private DateTime _creationTime;
        private SearchState _state;
        private object _model;

        public StoreListViewItemInfo() { }

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

        public StoreListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(SearchInfo))]
    sealed partial class SearchInfo : INotifyPropertyChanged, ICloneable<SearchInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;
        private bool _isExpanded;
        private bool _isUpdated;
        private SearchConditionsInfo _conditions;
        private ObservableCollection<SearchInfo> _children;

        public SearchInfo() { }

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
    sealed partial class SearchConditionsInfo : INotifyPropertyChanged, ICloneable<SearchConditionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ObservableCollection<SearchCondition<string>> _searchNames;
        private ObservableCollection<SearchCondition<SearchRegex>> _searchRegexes;
        private ObservableCollection<SearchCondition<Signature>> _searchSignatures;
        private ObservableCollection<SearchCondition<SearchRange<DateTime>>> _searchCreationTimeRanges;
        private ObservableCollection<SearchCondition<SearchRange<long>>> _searchLengthRanges;
        private ObservableCollection<SearchCondition<SearchState>> _searchStates;

        public SearchConditionsInfo() { }

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
    sealed partial class SearchListViewItemInfo : INotifyPropertyChanged, ICloneable<SearchListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private BitmapSource _icon;
        private string _name;
        private Signature _signature;
        private long _length;
        private DateTime _creationTime;
        private SearchState _state;
        private Seed _model;

        public SearchListViewItemInfo() { }

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
    sealed partial class DownloadListViewItemInfo : INotifyPropertyChanged, ICloneable<DownloadListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private BitmapSource _icon;
        private string _name;
        private long _length;
        private DateTime _creationTime;
        private DownloadListViewItemRateInfo _rate;
        private DownloadState _state;
        private string _path;
        private DownloadItemInfo _model;

        public DownloadListViewItemInfo() { }

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
    sealed partial class DownloadListViewItemRateInfo : INotifyPropertyChanged, ICloneable<DownloadListViewItemRateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _text;
        private int _depth;
        private double _value;

        public DownloadListViewItemRateInfo() { }

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

    [DataContract(Name = nameof(UploadStoreInfo))]
    sealed partial class UploadStoreInfo : INotifyPropertyChanged, ICloneable<UploadStoreInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool _isExpanded;
        private bool _isUpdated;
        private ObservableCollection<UploadDirectoryInfo> _directoryInfos;
        private ObservableCollection<UploadCategoryInfo> _categoryInfos;

        public UploadStoreInfo() { }

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

        [DataMember(Name = nameof(DirectoryInfos))]
        public ObservableCollection<UploadDirectoryInfo> DirectoryInfos
        {
            get
            {
                if (_directoryInfos == null)
                    _directoryInfos = new ObservableCollection<UploadDirectoryInfo>();

                return _directoryInfos;
            }
        }

        [DataMember(Name = nameof(CategoryInfos))]
        public ObservableCollection<UploadCategoryInfo> CategoryInfos
        {
            get
            {
                if (_categoryInfos == null)
                    _categoryInfos = new ObservableCollection<UploadCategoryInfo>();

                return _categoryInfos;
            }
        }

        public UploadStoreInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadCategoryInfo))]
    sealed partial class UploadCategoryInfo : INotifyPropertyChanged, ICloneable<UploadCategoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;
        private bool _isExpanded;
        private ObservableCollection<UploadDirectoryInfo> _directoryInfos;
        private ObservableCollection<UploadCategoryInfo> _categoryInfos;

        public UploadCategoryInfo() { }

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

        [DataMember(Name = nameof(DirectoryInfos))]
        public ObservableCollection<UploadDirectoryInfo> DirectoryInfos
        {
            get
            {
                if (_directoryInfos == null)
                    _directoryInfos = new ObservableCollection<UploadDirectoryInfo>();

                return _directoryInfos;
            }
        }

        [DataMember(Name = nameof(CategoryInfos))]
        public ObservableCollection<UploadCategoryInfo> CategoryInfos
        {
            get
            {
                if (_categoryInfos == null)
                    _categoryInfos = new ObservableCollection<UploadCategoryInfo>();

                return _categoryInfos;
            }
        }

        public UploadCategoryInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadDirectoryInfo))]
    sealed partial class UploadDirectoryInfo : INotifyPropertyChanged, ICloneable<UploadDirectoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;
        private string _path;
        private bool _isExpanded;
        private ObservableCollection<Seed> _seeds;
        private ObservableCollection<UploadBoxInfo> _boxInfos;

        public UploadDirectoryInfo() { }

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

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<UploadBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<UploadBoxInfo>();

                return _boxInfos;
            }
        }

        public UploadDirectoryInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadBoxInfo))]
    sealed partial class UploadBoxInfo : INotifyPropertyChanged, ICloneable<UploadBoxInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _name;
        private bool _isExpanded;
        private ObservableCollection<Seed> _seeds;
        private ObservableCollection<UploadBoxInfo> _boxInfos;

        public UploadBoxInfo() { }

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

        [DataMember(Name = nameof(BoxInfos))]
        public ObservableCollection<UploadBoxInfo> BoxInfos
        {
            get
            {
                if (_boxInfos == null)
                    _boxInfos = new ObservableCollection<UploadBoxInfo>();

                return _boxInfos;
            }
        }

        public UploadBoxInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadListViewItemInfo))]
    sealed partial class UploadListViewItemInfo : INotifyPropertyChanged, ICloneable<UploadListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private int _group;
        private BitmapSource _icon;
        private string _name;
        private long _length;
        private DateTime _creationTime;
        private SearchState _state;
        private string _path;
        private object _model;

        public UploadListViewItemInfo() { }

        [DataMember(Name = nameof(Group))]
        public int Group
        {
            get
            {
                return _group;
            }
            set
            {
                if (_group != value)
                {
                    _group = value;
                    this.OnPropertyChanged(nameof(Group));
                }
            }
        }

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

        public UploadListViewItemInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

    [DataContract(Name = nameof(UploadPreviewListViewItemInfo))]
    sealed partial class UploadPreviewListViewItemInfo : INotifyPropertyChanged, ICloneable<UploadPreviewListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private BitmapSource _icon;
        private string _name;
        private long _length;
        private string _path;

        public UploadPreviewListViewItemInfo() { }

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

    [DataContract(Name = nameof(UploadSyncRateInfo))]
    sealed partial class UploadSyncRateInfo : INotifyPropertyChanged, ICloneable<UploadSyncRateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _text;
        private double _value;

        public UploadSyncRateInfo() { }

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

        public UploadSyncRateInfo Clone()
        {
            return JsonUtils.Clone(this);
        }
    }

}
