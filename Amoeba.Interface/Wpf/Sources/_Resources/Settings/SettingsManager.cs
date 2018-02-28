using Amoeba.Messages;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Security;
using System.ComponentModel;
using System.Globalization;

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

        public static SettingsManager Instance { get; } = new SettingsManager(System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", "Settings"));

        private SettingsManager(string configPath)
        {
            _settings = new Settings(configPath);
        }

        public void Load()
        {
            int version = _settings.Load("Version", () => 0);

            this.UseLanguage = _settings.Load(nameof(UseLanguage), () =>
            {
                if (CultureInfo.CurrentUICulture.Name == "ja-JP")
                {
                    return "Japanese";
                }
                else
                {
                    return "English";
                }
            });
            this.AccountInfo = _settings.Load(nameof(AccountInfo), () =>
            {
                var info = new AccountInfo();
                info.DigitalSignature = new DigitalSignature("Anonymous", DigitalSignatureAlgorithm.EcDsaP521_Sha256_v3);
                info.Agreement = new Agreement(AgreementAlgorithm.EcDhP521_Sha256);

                return info;
            });
            this.SubscribeSignatures.UnionWith(_settings.Load(nameof(SubscribeSignatures), () => new Signature[] { Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU") }));
            this.UpdateInfo = _settings.Load(nameof(UpdateInfo), () =>
            {
                var info = new UpdateInfo();
                info.IsEnabled = true;
                info.Signature = Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU");

                return info;
            });
            this.ViewInfo = _settings.Load(nameof(ViewInfo), () => new ViewInfo());
            this.DownloadItemInfos.UnionWith(_settings.Load(nameof(DownloadItemInfos), () => new LockedHashSet<DownloadItemInfo>()));
            this.DownloadedSeeds.UnionWith(_settings.Load(nameof(DownloadedSeeds), () => new LockedHashSet<Seed>()));

            // ViewInfo
            {
                if (this.ViewInfo.Colors.Tree_Hit == null) this.ViewInfo.Colors.Tree_Hit = System.Windows.Media.Colors.LightPink.ToString();
                if (this.ViewInfo.Colors.Link_New == null) this.ViewInfo.Colors.Link_New = System.Windows.Media.Colors.LightPink.ToString();
                if (this.ViewInfo.Colors.Link_Visited == null) this.ViewInfo.Colors.Link_Visited = System.Windows.Media.Colors.SkyBlue.ToString();
                if (this.ViewInfo.Colors.Message_Trust == null) this.ViewInfo.Colors.Message_Trust = System.Windows.Media.Colors.SkyBlue.ToString();
                if (this.ViewInfo.Colors.Message_Untrust == null) this.ViewInfo.Colors.Message_Untrust = System.Windows.Media.Colors.LightPink.ToString();
                if (this.ViewInfo.Fonts.Chat_Message == null) this.ViewInfo.Fonts.Chat_Message = new FontInfo() { FontFamily = "MS PGothic", FontSize = 12 };
            }
        }

        public void Save()
        {
            _settings.Save("Version", 0);

            _settings.Save(nameof(UseLanguage), this.UseLanguage);
            _settings.Save(nameof(AccountInfo), this.AccountInfo);
            _settings.Save(nameof(SubscribeSignatures), this.SubscribeSignatures);
            _settings.Save(nameof(UpdateInfo), this.UpdateInfo);
            _settings.Save(nameof(ViewInfo), this.ViewInfo);
            _settings.Save(nameof(DownloadItemInfos), this.DownloadItemInfos);
            _settings.Save(nameof(DownloadedSeeds), this.DownloadedSeeds);
        }
    }
}
