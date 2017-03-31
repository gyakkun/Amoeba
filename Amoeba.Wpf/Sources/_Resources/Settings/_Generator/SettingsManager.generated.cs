using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omnius.Configuration;
using System.Collections.ObjectModel;

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
            this.Test = _settings.Load("Test", () => this.Test);
            this.Test2 = _settings.Load("Test2", () => this.Test2);
            this.Test3 = _settings.Load("Test3", () => this.Test3);
            this.BoxInfos = _settings.Load("BoxInfos", () => this.BoxInfos);
        }

        public void Save()
        {
            _settings.Save("Test", this.Test);
            _settings.Save("Test2", this.Test2);
            _settings.Save("Test3", this.Test3);
            _settings.Save("BoxInfos", this.BoxInfos);
        }

        public string Test { get; private set; }
        public string Test2 { get; set; }
        public string Test3 { get; private set; }
        public ObservableCollection<BoxInfo> BoxInfos { get; private set; }
    }
}
