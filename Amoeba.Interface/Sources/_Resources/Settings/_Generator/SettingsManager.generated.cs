using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Omnius.Configuration;
using System.Collections.ObjectModel;
using Omnius.Security;
using Omnius.Collections;

namespace Amoeba.Interface
{
    partial class SettingsManager
    {
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
