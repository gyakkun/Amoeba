using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Amoeba.Interface;
using Amoeba.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Security;

namespace Amoeba.Interface
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class AccountSetting : INotifyPropertyChanged, ICloneable<AccountSetting>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public AccountSetting() { }
        private DigitalSignature _digitalSignature;
        [JsonProperty]
        public DigitalSignature DigitalSignature
        {
            get { return _digitalSignature; }
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
        [JsonProperty]
        public string Comment
        {
            get { return _comment; }
            set
            {
                if (_comment != value)
                {
                    _comment = value;
                    this.OnPropertyChanged(nameof(Comment));
                }
            }
        }
        private Agreement _agreement;
        [JsonProperty]
        public Agreement Agreement
        {
            get { return _agreement; }
            set
            {
                if (_agreement != value)
                {
                    _agreement = value;
                    this.OnPropertyChanged(nameof(Agreement));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<Signature> TrustSignatures { get; } = new ObservableCollection<Signature>();
        [JsonProperty]
        public ObservableCollection<Signature> UntrustSignatures { get; } = new ObservableCollection<Signature>();
        [JsonProperty]
        public ObservableCollection<Tag> Tags { get; } = new ObservableCollection<Tag>();
        public AccountSetting Clone() => JsonUtils.Clone<AccountSetting>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UpdateSetting : INotifyPropertyChanged, ICloneable<UpdateSetting>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public UpdateSetting() { }
        private bool _isEnabled;
        [JsonProperty]
        public bool IsEnabled
        {
            get { return _isEnabled; }
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
        [JsonProperty]
        public Signature Signature
        {
            get { return _signature; }
            set
            {
                if (_signature != value)
                {
                    _signature = value;
                    this.OnPropertyChanged(nameof(Signature));
                }
            }
        }
        public UpdateSetting Clone() => JsonUtils.Clone<UpdateSetting>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class ViewSetting : INotifyPropertyChanged, ICloneable<ViewSetting>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public ViewSetting() { }
        [JsonProperty]
        public ColorsSetting Colors { get; } = new ColorsSetting();
        [JsonProperty]
        public FontsSetting Fonts { get; } = new FontsSetting();
        public ViewSetting Clone() => JsonUtils.Clone<ViewSetting>(this);
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public sealed partial class ColorsSetting : INotifyPropertyChanged, ICloneable<ColorsSetting>
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            public ColorsSetting() { }
            private string _tree_Hit;
            [JsonProperty]
            public string Tree_Hit
            {
                get { return _tree_Hit; }
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
            [JsonProperty]
            public string Link_New
            {
                get { return _link_New; }
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
            [JsonProperty]
            public string Link_Visited
            {
                get { return _link_Visited; }
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
            [JsonProperty]
            public string Message_Trust
            {
                get { return _message_Trust; }
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
            [JsonProperty]
            public string Message_Untrust
            {
                get { return _message_Untrust; }
                set
                {
                    if (_message_Untrust != value)
                    {
                        _message_Untrust = value;
                        this.OnPropertyChanged(nameof(Message_Untrust));
                    }
                }
            }
            public ColorsSetting Clone() => JsonUtils.Clone<ColorsSetting>(this);
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public sealed partial class FontsSetting : INotifyPropertyChanged, ICloneable<FontsSetting>
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            public FontsSetting() { }
            private FontSetting _chat_Message;
            [JsonProperty]
            public FontSetting Chat_Message
            {
                get { return _chat_Message; }
                set
                {
                    if (_chat_Message != value)
                    {
                        _chat_Message = value;
                        this.OnPropertyChanged(nameof(Chat_Message));
                    }
                }
            }
            public FontsSetting Clone() => JsonUtils.Clone<FontsSetting>(this);
            [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
            public sealed partial class FontSetting : INotifyPropertyChanged, ICloneable<FontSetting>
            {
                public event PropertyChangedEventHandler PropertyChanged;
                private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                public FontSetting() { }
                private string _fontFamily;
                [JsonProperty]
                public string FontFamily
                {
                    get { return _fontFamily; }
                    set
                    {
                        if (_fontFamily != value)
                        {
                            _fontFamily = value;
                            this.OnPropertyChanged(nameof(FontFamily));
                        }
                    }
                }
                private double _fontSize;
                [JsonProperty]
                public double FontSize
                {
                    get { return _fontSize; }
                    set
                    {
                        if (_fontSize != value)
                        {
                            _fontSize = value;
                            this.OnPropertyChanged(nameof(FontSize));
                        }
                    }
                }
                public FontSetting Clone() => JsonUtils.Clone<FontSetting>(this);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class ListSortInfo : INotifyPropertyChanged, ICloneable<ListSortInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public ListSortInfo() { }
        private string _propertyName;
        [JsonProperty]
        public string PropertyName
        {
            get { return _propertyName; }
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
        [JsonProperty]
        public ListSortDirection Direction
        {
            get { return _direction; }
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    this.OnPropertyChanged(nameof(Direction));
                }
            }
        }
        public ListSortInfo Clone() => JsonUtils.Clone<ListSortInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class RelationSignatureInfo : INotifyPropertyChanged, ICloneable<RelationSignatureInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public RelationSignatureInfo() { }
        private Signature _signature;
        [JsonProperty]
        public Signature Signature
        {
            get { return _signature; }
            set
            {
                if (_signature != value)
                {
                    _signature = value;
                    this.OnPropertyChanged(nameof(Signature));
                }
            }
        }
        private BroadcastProfileMessage _profile;
        [JsonProperty]
        public BroadcastProfileMessage Profile
        {
            get { return _profile; }
            set
            {
                if (_profile != value)
                {
                    _profile = value;
                    this.OnPropertyChanged(nameof(Profile));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<RelationSignatureInfo> Children { get; } = new ObservableCollection<RelationSignatureInfo>();
        public RelationSignatureInfo Clone() => JsonUtils.Clone<RelationSignatureInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class OptionsInfo : INotifyPropertyChanged, ICloneable<OptionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public OptionsInfo() { }
        [JsonProperty]
        public AccountInfo Account { get; } = new AccountInfo();
        [JsonProperty]
        public ConnectionInfo Connection { get; } = new ConnectionInfo();
        [JsonProperty]
        public DataInfo Data { get; } = new DataInfo();
        [JsonProperty]
        public ViewInfo View { get; } = new ViewInfo();
        [JsonProperty]
        public UpdateInfo Update { get; } = new UpdateInfo();
        public OptionsInfo Clone() => JsonUtils.Clone<OptionsInfo>(this);
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public sealed partial class AccountInfo : INotifyPropertyChanged, ICloneable<AccountInfo>
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            public AccountInfo() { }
            private DigitalSignature _digitalSignature;
            [JsonProperty]
            public DigitalSignature DigitalSignature
            {
                get { return _digitalSignature; }
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
            [JsonProperty]
            public string Comment
            {
                get { return _comment; }
                set
                {
                    if (_comment != value)
                    {
                        _comment = value;
                        this.OnPropertyChanged(nameof(Comment));
                    }
                }
            }
            [JsonProperty]
            public ObservableCollection<Signature> TrustSignatures { get; } = new ObservableCollection<Signature>();
            [JsonProperty]
            public ObservableCollection<Signature> UntrustSignatures { get; } = new ObservableCollection<Signature>();
            [JsonProperty]
            public ObservableCollection<Tag> Tags { get; } = new ObservableCollection<Tag>();
            public AccountInfo Clone() => JsonUtils.Clone<AccountInfo>(this);
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public sealed partial class ConnectionInfo : INotifyPropertyChanged, ICloneable<ConnectionInfo>
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            public ConnectionInfo() { }
            [JsonProperty]
            public TcpInfo Tcp { get; } = new TcpInfo();
            [JsonProperty]
            public I2pInfo I2p { get; } = new I2pInfo();
            [JsonProperty]
            public CustomInfo Custom { get; } = new CustomInfo();
            [JsonProperty]
            public BandwidthInfo Bandwidth { get; } = new BandwidthInfo();
            public ConnectionInfo Clone() => JsonUtils.Clone<ConnectionInfo>(this);
            [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
            public sealed partial class TcpInfo : INotifyPropertyChanged, ICloneable<TcpInfo>
            {
                public event PropertyChangedEventHandler PropertyChanged;
                private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                public TcpInfo() { }
                private bool _ipv4IsEnabled;
                [JsonProperty]
                public bool Ipv4IsEnabled
                {
                    get { return _ipv4IsEnabled; }
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
                [JsonProperty]
                public ushort Ipv4Port
                {
                    get { return _ipv4Port; }
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
                [JsonProperty]
                public bool Ipv6IsEnabled
                {
                    get { return _ipv6IsEnabled; }
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
                [JsonProperty]
                public ushort Ipv6Port
                {
                    get { return _ipv6Port; }
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
                [JsonProperty]
                public string ProxyUri
                {
                    get { return _proxyUri; }
                    set
                    {
                        if (_proxyUri != value)
                        {
                            _proxyUri = value;
                            this.OnPropertyChanged(nameof(ProxyUri));
                        }
                    }
                }
                public TcpInfo Clone() => JsonUtils.Clone<TcpInfo>(this);
            }
            [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
            public sealed partial class I2pInfo : INotifyPropertyChanged, ICloneable<I2pInfo>
            {
                public event PropertyChangedEventHandler PropertyChanged;
                private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                public I2pInfo() { }
                private bool _isEnabled;
                [JsonProperty]
                public bool IsEnabled
                {
                    get { return _isEnabled; }
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
                [JsonProperty]
                public string SamBridgeUri
                {
                    get { return _samBridgeUri; }
                    set
                    {
                        if (_samBridgeUri != value)
                        {
                            _samBridgeUri = value;
                            this.OnPropertyChanged(nameof(SamBridgeUri));
                        }
                    }
                }
                public I2pInfo Clone() => JsonUtils.Clone<I2pInfo>(this);
            }
            [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
            public sealed partial class CustomInfo : INotifyPropertyChanged, ICloneable<CustomInfo>
            {
                public event PropertyChangedEventHandler PropertyChanged;
                private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                public CustomInfo() { }
                [JsonProperty]
                public ObservableCollection<string> LocationUris { get; } = new ObservableCollection<string>();
                [JsonProperty]
                public ObservableCollection<ConnectionFilter> ConnectionFilters { get; } = new ObservableCollection<ConnectionFilter>();
                [JsonProperty]
                public ObservableCollection<string> ListenUris { get; } = new ObservableCollection<string>();
                public CustomInfo Clone() => JsonUtils.Clone<CustomInfo>(this);
            }
            [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
            public sealed partial class BandwidthInfo : INotifyPropertyChanged, ICloneable<BandwidthInfo>
            {
                public event PropertyChangedEventHandler PropertyChanged;
                private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                public BandwidthInfo() { }
                private int _connectionCountLimit;
                [JsonProperty]
                public int ConnectionCountLimit
                {
                    get { return _connectionCountLimit; }
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
                [JsonProperty]
                public int BandwidthLimit
                {
                    get { return _bandwidthLimit; }
                    set
                    {
                        if (_bandwidthLimit != value)
                        {
                            _bandwidthLimit = value;
                            this.OnPropertyChanged(nameof(BandwidthLimit));
                        }
                    }
                }
                public BandwidthInfo Clone() => JsonUtils.Clone<BandwidthInfo>(this);
            }
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public sealed partial class DataInfo : INotifyPropertyChanged, ICloneable<DataInfo>
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            public DataInfo() { }
            [JsonProperty]
            public CacheInfo Cache { get; } = new CacheInfo();
            [JsonProperty]
            public DownloadInfo Download { get; } = new DownloadInfo();
            public DataInfo Clone() => JsonUtils.Clone<DataInfo>(this);
            [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
            public sealed partial class CacheInfo : INotifyPropertyChanged, ICloneable<CacheInfo>
            {
                public event PropertyChangedEventHandler PropertyChanged;
                private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                public CacheInfo() { }
                private long _size;
                [JsonProperty]
                public long Size
                {
                    get { return _size; }
                    set
                    {
                        if (_size != value)
                        {
                            _size = value;
                            this.OnPropertyChanged(nameof(Size));
                        }
                    }
                }
                public CacheInfo Clone() => JsonUtils.Clone<CacheInfo>(this);
            }
            [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
            public sealed partial class DownloadInfo : INotifyPropertyChanged, ICloneable<DownloadInfo>
            {
                public event PropertyChangedEventHandler PropertyChanged;
                private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                public DownloadInfo() { }
                private string _directoryPath;
                [JsonProperty]
                public string DirectoryPath
                {
                    get { return _directoryPath; }
                    set
                    {
                        if (_directoryPath != value)
                        {
                            _directoryPath = value;
                            this.OnPropertyChanged(nameof(DirectoryPath));
                        }
                    }
                }
                private int _protectedPercentage;
                [JsonProperty]
                public int ProtectedPercentage
                {
                    get { return _protectedPercentage; }
                    set
                    {
                        if (_protectedPercentage != value)
                        {
                            _protectedPercentage = value;
                            this.OnPropertyChanged(nameof(ProtectedPercentage));
                        }
                    }
                }
                public DownloadInfo Clone() => JsonUtils.Clone<DownloadInfo>(this);
            }
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public sealed partial class ViewInfo : INotifyPropertyChanged, ICloneable<ViewInfo>
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            public ViewInfo() { }
            [JsonProperty]
            public ObservableCollection<Signature> SubscribeSignatures { get; } = new ObservableCollection<Signature>();
            public ViewInfo Clone() => JsonUtils.Clone<ViewInfo>(this);
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public sealed partial class UpdateInfo : INotifyPropertyChanged, ICloneable<UpdateInfo>
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            public UpdateInfo() { }
            private bool _isEnabled;
            [JsonProperty]
            public bool IsEnabled
            {
                get { return _isEnabled; }
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
            [JsonProperty]
            public Signature Signature
            {
                get { return _signature; }
                set
                {
                    if (_signature != value)
                    {
                        _signature = value;
                        this.OnPropertyChanged(nameof(Signature));
                    }
                }
            }
            public UpdateInfo Clone() => JsonUtils.Clone<UpdateInfo>(this);
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class CloudStateInfo : INotifyPropertyChanged, ICloneable<CloudStateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public CloudStateInfo() { }
        private string _location;
        [JsonProperty]
        public string Location
        {
            get { return _location; }
            set
            {
                if (_location != value)
                {
                    _location = value;
                    this.OnPropertyChanged(nameof(Location));
                }
            }
        }
        public CloudStateInfo Clone() => JsonUtils.Clone<CloudStateInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class ChatCategoryInfo : INotifyPropertyChanged, ICloneable<ChatCategoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public ChatCategoryInfo() { }
        private string _name;
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<ChatThreadInfo> ThreadInfos { get; } = new ObservableCollection<ChatThreadInfo>();
        [JsonProperty]
        public ObservableCollection<ChatCategoryInfo> CategoryInfos { get; } = new ObservableCollection<ChatCategoryInfo>();
        public ChatCategoryInfo Clone() => JsonUtils.Clone<ChatCategoryInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class ChatThreadInfo : INotifyPropertyChanged, ICloneable<ChatThreadInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public ChatThreadInfo() { }
        private bool _isUpdated;
        [JsonProperty]
        public bool IsUpdated
        {
            get { return _isUpdated; }
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
        [JsonProperty]
        public Tag Tag
        {
            get { return _tag; }
            set
            {
                if (_tag != value)
                {
                    _tag = value;
                    this.OnPropertyChanged(nameof(Tag));
                }
            }
        }
        private bool _isTrustMessageOnly;
        [JsonProperty]
        public bool IsTrustMessageOnly
        {
            get { return _isTrustMessageOnly; }
            set
            {
                if (_isTrustMessageOnly != value)
                {
                    _isTrustMessageOnly = value;
                    this.OnPropertyChanged(nameof(IsTrustMessageOnly));
                }
            }
        }
        private bool _isNewMessageOnly;
        [JsonProperty]
        public bool IsNewMessageOnly
        {
            get { return _isNewMessageOnly; }
            set
            {
                if (_isNewMessageOnly != value)
                {
                    _isNewMessageOnly = value;
                    this.OnPropertyChanged(nameof(IsNewMessageOnly));
                }
            }
        }
        [JsonProperty]
        public LockedList<ChatMessageInfo> Messages { get; } = new LockedList<ChatMessageInfo>();
        public ChatThreadInfo Clone() => JsonUtils.Clone<ChatThreadInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class ChatMessageInfo : INotifyPropertyChanged, ICloneable<ChatMessageInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public ChatMessageInfo() { }
        private ChatMessageState _state;
        [JsonProperty]
        public ChatMessageState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    this.OnPropertyChanged(nameof(State));
                }
            }
        }
        private MulticastCommentMessage _message;
        [JsonProperty]
        public MulticastCommentMessage Message
        {
            get { return _message; }
            set
            {
                if (_message != value)
                {
                    _message = value;
                    this.OnPropertyChanged(nameof(Message));
                }
            }
        }
        public ChatMessageInfo Clone() => JsonUtils.Clone<ChatMessageInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class StoreCategoryInfo : INotifyPropertyChanged, ICloneable<StoreCategoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public StoreCategoryInfo() { }
        private string _name;
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<StoreSignatureInfo> SignatureInfos { get; } = new ObservableCollection<StoreSignatureInfo>();
        [JsonProperty]
        public ObservableCollection<StoreCategoryInfo> CategoryInfos { get; } = new ObservableCollection<StoreCategoryInfo>();
        public StoreCategoryInfo Clone() => JsonUtils.Clone<StoreCategoryInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class StoreSignatureInfo : INotifyPropertyChanged, ICloneable<StoreSignatureInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public StoreSignatureInfo() { }
        private Signature _authorSignature;
        [JsonProperty]
        public Signature AuthorSignature
        {
            get { return _authorSignature; }
            set
            {
                if (_authorSignature != value)
                {
                    _authorSignature = value;
                    this.OnPropertyChanged(nameof(AuthorSignature));
                }
            }
        }
        private DateTime _updateTime;
        [JsonProperty]
        public DateTime UpdateTime
        {
            get { return _updateTime; }
            set
            {
                if (_updateTime != value)
                {
                    _updateTime = value;
                    this.OnPropertyChanged(nameof(UpdateTime));
                }
            }
        }
        private bool _isExpanded;
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
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
        [JsonProperty]
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set
            {
                if (_isUpdated != value)
                {
                    _isUpdated = value;
                    this.OnPropertyChanged(nameof(IsUpdated));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<StoreBoxInfo> BoxInfos { get; } = new ObservableCollection<StoreBoxInfo>();
        public StoreSignatureInfo Clone() => JsonUtils.Clone<StoreSignatureInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class StoreBoxInfo : INotifyPropertyChanged, ICloneable<StoreBoxInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public StoreBoxInfo() { }
        private string _name;
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<Seed> Seeds { get; } = new ObservableCollection<Seed>();
        [JsonProperty]
        public ObservableCollection<StoreBoxInfo> BoxInfos { get; } = new ObservableCollection<StoreBoxInfo>();
        public StoreBoxInfo Clone() => JsonUtils.Clone<StoreBoxInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class StoreListViewItemInfo : INotifyPropertyChanged, ICloneable<StoreListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public StoreListViewItemInfo() { }
        private BitmapSource _icon;
        [JsonProperty]
        public BitmapSource Icon
        {
            get { return _icon; }
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
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public long Length
        {
            get { return _length; }
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
        [JsonProperty]
        public DateTime CreationTime
        {
            get { return _creationTime; }
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
        [JsonProperty]
        public SearchState State
        {
            get { return _state; }
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
        [JsonProperty]
        public object Model
        {
            get { return _model; }
            set
            {
                if (_model != value)
                {
                    _model = value;
                    this.OnPropertyChanged(nameof(Model));
                }
            }
        }
        public StoreListViewItemInfo Clone() => JsonUtils.Clone<StoreListViewItemInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class SearchInfo : INotifyPropertyChanged, ICloneable<SearchInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public SearchInfo() { }
        private string _name;
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
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
        [JsonProperty]
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set
            {
                if (_isUpdated != value)
                {
                    _isUpdated = value;
                    this.OnPropertyChanged(nameof(IsUpdated));
                }
            }
        }
        [JsonProperty]
        public SearchConditionsInfo Conditions { get; } = new SearchConditionsInfo();
        [JsonProperty]
        public ObservableCollection<SearchInfo> Children { get; } = new ObservableCollection<SearchInfo>();
        public SearchInfo Clone() => JsonUtils.Clone<SearchInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class SearchConditionsInfo : INotifyPropertyChanged, ICloneable<SearchConditionsInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public SearchConditionsInfo() { }
        [JsonProperty]
        public ObservableCollection<SearchCondition<string>> SearchNames { get; } = new ObservableCollection<SearchCondition<string>>();
        [JsonProperty]
        public ObservableCollection<SearchCondition<SearchRegex>> SearchRegexes { get; } = new ObservableCollection<SearchCondition<SearchRegex>>();
        [JsonProperty]
        public ObservableCollection<SearchCondition<Signature>> SearchSignatures { get; } = new ObservableCollection<SearchCondition<Signature>>();
        [JsonProperty]
        public ObservableCollection<SearchCondition<SearchRange<DateTime>>> SearchCreationTimeRanges { get; } = new ObservableCollection<SearchCondition<SearchRange<DateTime>>>();
        [JsonProperty]
        public ObservableCollection<SearchCondition<SearchRange<long>>> SearchLengthRanges { get; } = new ObservableCollection<SearchCondition<SearchRange<long>>>();
        [JsonProperty]
        public ObservableCollection<SearchCondition<SearchState>> SearchStates { get; } = new ObservableCollection<SearchCondition<SearchState>>();
        public SearchConditionsInfo Clone() => JsonUtils.Clone<SearchConditionsInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class SearchListViewItemInfo : INotifyPropertyChanged, ICloneable<SearchListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public SearchListViewItemInfo() { }
        private BitmapSource _icon;
        [JsonProperty]
        public BitmapSource Icon
        {
            get { return _icon; }
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
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public Signature Signature
        {
            get { return _signature; }
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
        [JsonProperty]
        public long Length
        {
            get { return _length; }
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
        [JsonProperty]
        public DateTime CreationTime
        {
            get { return _creationTime; }
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
        [JsonProperty]
        public SearchState State
        {
            get { return _state; }
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
        [JsonProperty]
        public Seed Model
        {
            get { return _model; }
            set
            {
                if (_model != value)
                {
                    _model = value;
                    this.OnPropertyChanged(nameof(Model));
                }
            }
        }
        public SearchListViewItemInfo Clone() => JsonUtils.Clone<SearchListViewItemInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class DownloadListViewItemInfo : INotifyPropertyChanged, ICloneable<DownloadListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public DownloadListViewItemInfo() { }
        private BitmapSource _icon;
        [JsonProperty]
        public BitmapSource Icon
        {
            get { return _icon; }
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
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public long Length
        {
            get { return _length; }
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
        [JsonProperty]
        public DateTime CreationTime
        {
            get { return _creationTime; }
            set
            {
                if (_creationTime != value)
                {
                    _creationTime = value;
                    this.OnPropertyChanged(nameof(CreationTime));
                }
            }
        }
        [JsonProperty]
        public DownloadListViewItemRateInfo Rate { get; } = new DownloadListViewItemRateInfo();
        private DownloadState _state;
        [JsonProperty]
        public DownloadState State
        {
            get { return _state; }
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
        [JsonProperty]
        public string Path
        {
            get { return _path; }
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
        [JsonProperty]
        public DownloadItemInfo Model
        {
            get { return _model; }
            set
            {
                if (_model != value)
                {
                    _model = value;
                    this.OnPropertyChanged(nameof(Model));
                }
            }
        }
        public DownloadListViewItemInfo Clone() => JsonUtils.Clone<DownloadListViewItemInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class DownloadListViewItemRateInfo : INotifyPropertyChanged, ICloneable<DownloadListViewItemRateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public DownloadListViewItemRateInfo() { }
        private string _text;
        [JsonProperty]
        public string Text
        {
            get { return _text; }
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
        [JsonProperty]
        public int Depth
        {
            get { return _depth; }
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
        [JsonProperty]
        public double Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    this.OnPropertyChanged(nameof(Value));
                }
            }
        }
        public DownloadListViewItemRateInfo Clone() => JsonUtils.Clone<DownloadListViewItemRateInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UploadStoreInfo : INotifyPropertyChanged, ICloneable<UploadStoreInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public UploadStoreInfo() { }
        private bool _isExpanded;
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
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
        [JsonProperty]
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set
            {
                if (_isUpdated != value)
                {
                    _isUpdated = value;
                    this.OnPropertyChanged(nameof(IsUpdated));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<UploadDirectoryInfo> DirectoryInfos { get; } = new ObservableCollection<UploadDirectoryInfo>();
        [JsonProperty]
        public ObservableCollection<UploadCategoryInfo> CategoryInfos { get; } = new ObservableCollection<UploadCategoryInfo>();
        public UploadStoreInfo Clone() => JsonUtils.Clone<UploadStoreInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UploadCategoryInfo : INotifyPropertyChanged, ICloneable<UploadCategoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public UploadCategoryInfo() { }
        private string _name;
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<UploadDirectoryInfo> DirectoryInfos { get; } = new ObservableCollection<UploadDirectoryInfo>();
        [JsonProperty]
        public ObservableCollection<UploadCategoryInfo> CategoryInfos { get; } = new ObservableCollection<UploadCategoryInfo>();
        public UploadCategoryInfo Clone() => JsonUtils.Clone<UploadCategoryInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UploadDirectoryInfo : INotifyPropertyChanged, ICloneable<UploadDirectoryInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public UploadDirectoryInfo() { }
        private string _name;
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public string Path
        {
            get { return _path; }
            set
            {
                if (_path != value)
                {
                    _path = value;
                    this.OnPropertyChanged(nameof(Path));
                }
            }
        }
        private bool _isExpanded;
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<Seed> Seeds { get; } = new ObservableCollection<Seed>();
        [JsonProperty]
        public ObservableCollection<UploadBoxInfo> BoxInfos { get; } = new ObservableCollection<UploadBoxInfo>();
        public UploadDirectoryInfo Clone() => JsonUtils.Clone<UploadDirectoryInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UploadBoxInfo : INotifyPropertyChanged, ICloneable<UploadBoxInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public UploadBoxInfo() { }
        private string _name;
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        [JsonProperty]
        public ObservableCollection<Seed> Seeds { get; } = new ObservableCollection<Seed>();
        [JsonProperty]
        public ObservableCollection<UploadBoxInfo> BoxInfos { get; } = new ObservableCollection<UploadBoxInfo>();
        public UploadBoxInfo Clone() => JsonUtils.Clone<UploadBoxInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UploadListViewItemInfo : INotifyPropertyChanged, ICloneable<UploadListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public UploadListViewItemInfo() { }
        private int _group;
        [JsonProperty]
        public int Group
        {
            get { return _group; }
            set
            {
                if (_group != value)
                {
                    _group = value;
                    this.OnPropertyChanged(nameof(Group));
                }
            }
        }
        private BitmapSource _icon;
        [JsonProperty]
        public BitmapSource Icon
        {
            get { return _icon; }
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
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public long Length
        {
            get { return _length; }
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
        [JsonProperty]
        public DateTime CreationTime
        {
            get { return _creationTime; }
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
        [JsonProperty]
        public SearchState State
        {
            get { return _state; }
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
        [JsonProperty]
        public string Path
        {
            get { return _path; }
            set
            {
                if (_path != value)
                {
                    _path = value;
                    this.OnPropertyChanged(nameof(Path));
                }
            }
        }
        private object _model;
        [JsonProperty]
        public object Model
        {
            get { return _model; }
            set
            {
                if (_model != value)
                {
                    _model = value;
                    this.OnPropertyChanged(nameof(Model));
                }
            }
        }
        public UploadListViewItemInfo Clone() => JsonUtils.Clone<UploadListViewItemInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UploadPreviewListViewItemInfo : INotifyPropertyChanged, ICloneable<UploadPreviewListViewItemInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public UploadPreviewListViewItemInfo() { }
        private BitmapSource _icon;
        [JsonProperty]
        public BitmapSource Icon
        {
            get { return _icon; }
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
        [JsonProperty]
        public string Name
        {
            get { return _name; }
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
        [JsonProperty]
        public long Length
        {
            get { return _length; }
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
        [JsonProperty]
        public string Path
        {
            get { return _path; }
            set
            {
                if (_path != value)
                {
                    _path = value;
                    this.OnPropertyChanged(nameof(Path));
                }
            }
        }
        public UploadPreviewListViewItemInfo Clone() => JsonUtils.Clone<UploadPreviewListViewItemInfo>(this);
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UploadSyncRateInfo : INotifyPropertyChanged, ICloneable<UploadSyncRateInfo>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public UploadSyncRateInfo() { }
        private string _text;
        [JsonProperty]
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    this.OnPropertyChanged(nameof(Text));
                }
            }
        }
        private double _value;
        [JsonProperty]
        public double Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    this.OnPropertyChanged(nameof(Value));
                }
            }
        }
        public UploadSyncRateInfo Clone() => JsonUtils.Clone<UploadSyncRateInfo>(this);
    }

}
