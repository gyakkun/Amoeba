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

        private AccountSetting _accountSetting;

        [DataMember(Name = nameof(AccountSetting))]
        public AccountSetting AccountSetting
        {
            get
            {
                return _accountSetting;
            }
            set
            {
                if (_accountSetting != value)
                {
                    _accountSetting = value;
                    this.OnPropertyChanged(nameof(AccountSetting));
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

        private UpdateSetting _updateSetting;

        [DataMember(Name = nameof(UpdateSetting))]
        public UpdateSetting UpdateSetting
        {
            get
            {
                return _updateSetting;
            }
            set
            {
                if (_updateSetting != value)
                {
                    _updateSetting = value;
                    this.OnPropertyChanged(nameof(UpdateSetting));
                }
            }
        }

        private ViewSetting _viewSetting;

        [DataMember(Name = nameof(ViewSetting))]
        public ViewSetting ViewSetting
        {
            get
            {
                return _viewSetting;
            }
            set
            {
                if (_viewSetting != value)
                {
                    _viewSetting = value;
                    this.OnPropertyChanged(nameof(ViewSetting));
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

    }
}
