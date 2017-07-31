using System.ComponentModel;
using Omnius.Net.Amoeba;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;

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

        private SettingsManager(string configPath)
        {
            _settings = new Settings(configPath);
        }

        public void Load()
        {
            int version = _settings.Load("Version", () => 0);

            this.UseLanguage = _settings.Load(nameof(UseLanguage), () => "English");
            this.SubscribeSignatures.UnionWith(_settings.Load(nameof(SubscribeSignatures), () => new Signature[] { Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU") }));
            this.AccountInfo = _settings.Load(nameof(AccountInfo), () =>
            {
                var info = new AccountInfo();
                info.DigitalSignature = new DigitalSignature("Anonymous", DigitalSignatureAlgorithm.EcDsaP521_Sha256);
                info.Exchange = new Exchange(ExchangeAlgorithm.Rsa4096);

                return info;
            });
            this.UpdateInfo = _settings.Load(nameof(UpdateInfo), () =>
            {
                var info = new UpdateInfo();
                info.IsEnabled = true;
                info.Signature = Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU");

                return info;
            });
            this.DownloadItemInfos.UnionWith(_settings.Load(nameof(DownloadItemInfos), () => new LockedHashSet<DownloadItemInfo>()));
            this.DownloadedSeeds.UnionWith(_settings.Load(nameof(DownloadedSeeds), () => new LockedHashSet<Seed>()));
        }

        public void Save()
        {
            _settings.Save("Version", 0);

            _settings.Save(nameof(UseLanguage), this.UseLanguage);
            _settings.Save(nameof(SubscribeSignatures), this.SubscribeSignatures);
            _settings.Save(nameof(AccountInfo), this.AccountInfo);
            _settings.Save(nameof(UpdateInfo), this.UpdateInfo);
            _settings.Save(nameof(DownloadItemInfos), this.DownloadItemInfos);
            _settings.Save(nameof(DownloadedSeeds), this.DownloadedSeeds);
        }
    }
}
