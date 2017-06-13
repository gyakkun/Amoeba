using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Omnius.Configuration;
using Omnius.Security;
using System.Collections.ObjectModel;
using Omnius.Collections;

namespace Amoeba.Interface
{
    partial class SettingsManager : ISettings, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Settings _settings;

        public static SettingsManager Instance { get; } = new SettingsManager(System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "Settings"));

        public SettingsManager(string configPath)
        {
            _settings = new Settings(configPath);
        }

        public void Load()
        {
            int version = _settings.Load("Version", () => 0);

            this.AccountInfo = _settings.Load(nameof(AccountInfo), () =>
            {
                var info = new AccountInfo();
                info.DigitalSignature = new DigitalSignature("Anonymous", DigitalSignatureAlgorithm.EcDsaP521_Sha256);
                info.Exchange = new Exchange(ExchangeAlgorithm.Rsa4096);

                return info;
            });
            this.SubscribeSignatures.UnionWith(_settings.Load(nameof(SubscribeSignatures), () => new Signature[] { Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU") }));
            this.PublishDirectoryInfos.AddRange(_settings.Load(nameof(PublishDirectoryInfos), () => new ObservableCollection<PublishDirectoryInfo>()));
            this.DownloadItemInfos.UnionWith(_settings.Load(nameof(DownloadItemInfos), () => new LockedHashSet<DownloadItemInfo>()));
        }

        public void Save()
        {
            _settings.Save("Version", 0);

            _settings.Save(nameof(AccountInfo), this.AccountInfo);
            _settings.Save(nameof(SubscribeSignatures), this.SubscribeSignatures);
            _settings.Save(nameof(PublishDirectoryInfos), this.PublishDirectoryInfos);
            _settings.Save(nameof(DownloadItemInfos), this.DownloadItemInfos);
        }
    }
}
