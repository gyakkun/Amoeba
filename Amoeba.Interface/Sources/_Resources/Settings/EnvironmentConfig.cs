using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    class EnvironmentConfig
    {
        public static readonly Version AmoebaVersion;
        public static readonly EnvironmentPathConfig Paths;

        static EnvironmentConfig()
        {
            AmoebaVersion = new Version(5, 0, 0);
            Paths = new EnvironmentPathConfig();
        }
    }

    class EnvironmentPathConfig
    {
        public string BasePath { get; private set; }
        public string ConfigPath { get; private set; }
        public string UpdatePath { get; private set; }
        public string LogPath { get; private set; }
        public string WorkPath { get; private set; }
        public string LanguagesPath { get; private set; }
        public string SettingsPath { get; private set; }

        public EnvironmentPathConfig()
        {
            this.BasePath = @"../";
            this.ConfigPath = @"../Config";
            this.UpdatePath = @"../Update";
            this.LogPath = @"../Log";
            this.WorkPath = @"../Work";
            this.LanguagesPath = "./Resources/Languages";
            this.SettingsPath = Path.Combine(ConfigPath, "Settings");
        }
    }
}
