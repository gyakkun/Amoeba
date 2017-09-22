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

        private UpdateInfo _updateInfo;

        [DataMember(Name = nameof(UpdateInfo))]
        public UpdateInfo UpdateInfo
        {
            get
            {
                return _updateInfo;
            }
            set
            {
                if (_updateInfo != value)
                {
                    _updateInfo = value;
                    this.OnPropertyChanged(nameof(UpdateInfo));
                }
            }
        }

        private ViewInfo _viewInfo;

        [DataMember(Name = nameof(ViewInfo))]
        public ViewInfo ViewInfo
        {
            get
            {
                return _viewInfo;
            }
            set
            {
                if (_viewInfo != value)
                {
                    _viewInfo = value;
                    this.OnPropertyChanged(nameof(ViewInfo));
                }
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

        private LockedHashSet<Seed> _downloadedSeeds;

        [DataMember(Name = nameof(DownloadedSeeds))]
        public LockedHashSet<Seed> DownloadedSeeds
        {
            get
            {
                if (_downloadedSeeds == null)
                    _downloadedSeeds = new LockedHashSet<Seed>();

                return _downloadedSeeds;
            }
        }

        private LockedHashSet<UploadItemInfo> _uploadItemInfos;

        [DataMember(Name = nameof(UploadItemInfos))]
        public LockedHashSet<UploadItemInfo> UploadItemInfos
        {
            get
            {
                if (_uploadItemInfos == null)
                    _uploadItemInfos = new LockedHashSet<UploadItemInfo>();

                return _uploadItemInfos;
            }
        }

    }
}
