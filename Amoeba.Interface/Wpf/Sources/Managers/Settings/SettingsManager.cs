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

        public static SettingsManager Instance { get; } = new SettingsManager(System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "Control", "Settings"));

        private SettingsManager(string configPath)
        {
            _settings = new Settings(configPath);
        }

        public void Load()
        {
            int version = _settings.Load("Version", () => 0);

            this.UseLanguage = _settings.Load(nameof(this.UseLanguage), () =>
            {
                if (CultureInfo.CurrentUICulture.Name == "ja-JP")
                {
                    return "Japanese";
                }
                else if (CultureInfo.CurrentUICulture.Name == "zh-CN")
                {
                    return "Chinese";
                }
                else
                {
                    return "English";
                }
            });
            this.AccountSetting = _settings.Load(nameof(this.AccountSetting), () =>
            {
                var info = new AccountSetting();
                info.DigitalSignature = new DigitalSignature("Anonymous", DigitalSignatureAlgorithm.EcDsaP521_Sha256_v3);
                info.Agreement = new Agreement(AgreementAlgorithm.EcDhP521_Sha256);

                return info;
            });
            this.SubscribeSignatures.UnionWith(_settings.Load(nameof(this.SubscribeSignatures), () => new Signature[] { Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU") }));
            this.UpdateSetting = _settings.Load(nameof(this.UpdateSetting), () =>
            {
                var info = new UpdateSetting();
                info.IsEnabled = true;
                info.Signature = Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU");

                return info;
            });
            this.ViewSetting = _settings.Load(nameof(this.ViewSetting), () => new ViewSetting());
            this.DownloadItemInfos.UnionWith(_settings.Load(nameof(this.DownloadItemInfos), () => new LockedHashSet<DownloadItemInfo>()));
            this.DownloadedSeeds.UnionWith(_settings.Load(nameof(this.DownloadedSeeds), () => new LockedHashSet<Seed>()));

            // ViewSetting
            {
                if (this.ViewSetting.Colors.Tree_Hit == null) this.ViewSetting.Colors.Tree_Hit = System.Windows.Media.Colors.LightPink.ToString();
                if (this.ViewSetting.Colors.Link_New == null) this.ViewSetting.Colors.Link_New = System.Windows.Media.Colors.LightPink.ToString();
                if (this.ViewSetting.Colors.Link_Visited == null) this.ViewSetting.Colors.Link_Visited = System.Windows.Media.Colors.SkyBlue.ToString();
                if (this.ViewSetting.Colors.Message_Trust == null) this.ViewSetting.Colors.Message_Trust = System.Windows.Media.Colors.SkyBlue.ToString();
                if (this.ViewSetting.Colors.Message_Untrust == null) this.ViewSetting.Colors.Message_Untrust = System.Windows.Media.Colors.LightPink.ToString();
                if (this.ViewSetting.Fonts.Chat_Message == null) this.ViewSetting.Fonts.Chat_Message = new ViewSetting.FontsSetting.FontSetting() { FontFamily = "MS PGothic", FontSize = 12 };
            }
        }

        public void Save()
        {
            _settings.Save("Version", 0);

            _settings.Save(nameof(this.UseLanguage), this.UseLanguage);
            _settings.Save(nameof(this.AccountSetting), this.AccountSetting);
            _settings.Save(nameof(this.SubscribeSignatures), this.SubscribeSignatures);
            _settings.Save(nameof(this.UpdateSetting), this.UpdateSetting);
            _settings.Save(nameof(this.ViewSetting), this.ViewSetting);
            _settings.Save(nameof(this.DownloadItemInfos), this.DownloadItemInfos);
            _settings.Save(nameof(this.DownloadedSeeds), this.DownloadedSeeds);
        }
    }
}
