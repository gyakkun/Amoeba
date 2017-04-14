using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Omnius.Configuration;
using System.Collections.ObjectModel;
using Omnius.Security;

namespace Amoeba.Interface
{
    partial class SettingsManager : ISettings
    {
        private Settings _settings;

        public SettingsManager(string configPath)
        {
            _settings = new Settings(configPath);

            this.Init();
        }

        public void Load()
        {
            this.DigitalSignature = _settings.Load("DigitalSignature", () => this.DigitalSignature);
        }

        public void Save()
        {
            _settings.Save("DigitalSignature", this.DigitalSignature);
        }


        private DigitalSignature _digitalSignature;

        [DataMember(Name = nameof(DigitalSignature))]
        public DigitalSignature DigitalSignature
        {
            get
            {
                return _digitalSignature;
            }
            set
            {
                if (_digitalSignature != value)
                {
                    _digitalSignature = value;
                    this.OnPropertyChanged(nameof(DigitalSignature));
                }
            }
        }
    }
}
