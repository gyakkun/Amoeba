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
            this.DigitalSignature = _settings.Load(nameof(DigitalSignature), () => new DigitalSignature("Anonymous", DigitalSignatureAlgorithm.EcDsaP521_Sha256));
            this.PublishDirectoryInfos.AddRange(_settings.Load(nameof(PublishDirectoryInfos), () => new ObservableCollection<PublishDirectoryInfo>()));
        }

        public void Save()
        {
            _settings.Save(nameof(DigitalSignature), this.DigitalSignature);
            _settings.Save(nameof(PublishDirectoryInfos), this.PublishDirectoryInfos);
        }
    }
}
