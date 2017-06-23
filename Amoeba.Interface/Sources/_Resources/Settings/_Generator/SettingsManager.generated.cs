using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Omnius.Collections;
using Omnius.Security;

namespace Amoeba.Interface
{
    partial class SettingsManager
    {
        private string _useLanguage;

        [DataMember(Name = nameof(UseLanguage))]
        public string UseLanguage
        {
            get
            {
                return _useLanguage;
            }
            set
            {
                if (_useLanguage != value)
                {
                    _useLanguage = value;
                    this.OnPropertyChanged(nameof(UseLanguage));
                }
            }
        }

        private AccountInfo _accountInfo;

        [DataMember(Name = nameof(AccountInfo))]
        public AccountInfo AccountInfo
        {
            get
            {
                return _accountInfo;
            }
            set
            {
                if (_accountInfo != value)
                {
                    _accountInfo = value;
                    this.OnPropertyChanged(nameof(AccountInfo));
                }
            }
        }

        private LockedHashSet<Signature> _subscribeSignatures;

        [DataMember(Name = nameof(SubscribeSignatures))]
        public LockedHashSet<Signature> SubscribeSignatures
        {
            get
            {
                if (_subscribeSignatures == null)
                    _subscribeSignatures = new LockedHashSet<Signature>();

                return _subscribeSignatures;
            }
        }

        private ObservableCollection<PublishDirectoryInfo> _publishDirectoryInfos;

        [DataMember(Name = nameof(PublishDirectoryInfos))]
        public ObservableCollection<PublishDirectoryInfo> PublishDirectoryInfos
        {
            get
            {
                if (_publishDirectoryInfos == null)
                    _publishDirectoryInfos = new ObservableCollection<PublishDirectoryInfo>();

                return _publishDirectoryInfos;
            }
        }

        private LockedHashSet<DownloadItemInfo> _downloadItemInfos;

        [DataMember(Name = nameof(DownloadItemInfos))]
        public LockedHashSet<DownloadItemInfo> DownloadItemInfos
        {
            get
            {
                if (_downloadItemInfos == null)
                    _downloadItemInfos = new LockedHashSet<DownloadItemInfo>();

                return _downloadItemInfos;
            }
        }

    }
}
