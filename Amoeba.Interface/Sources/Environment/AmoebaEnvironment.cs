using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Nett;

namespace Amoeba.Interface
{
    class AmoebaEnvironment
    {
        public static Version Version { get; private set; }
        public static EnvironmentPaths Paths { get; private set; }
        public static EnvironmentConfig Config { get; private set; }

        static AmoebaEnvironment()
        {
            Version = new Version(5, 0, 0);
            Paths = new EnvironmentPaths();

            Load();
        }

        private static void Load()
        {
            var configPath = Path.Combine(Paths.ConfigPath, "Config.toml");

            if (!File.Exists(configPath))
            {
                Toml.WriteFile(new EnvironmentConfig(), configPath);
            }

            Config = Toml.ReadFile<EnvironmentConfig>(configPath);
        }

        public class EnvironmentPaths
        {
            public string BasePath { get; private set; }
            public string ConfigPath { get; private set; }
            public string UpdatePath { get; private set; }
            public string LogPath { get; private set; }
            public string WorkPath { get; private set; }
            public string LanguagesPath { get; private set; }

            public EnvironmentPaths()
            {
                this.BasePath = "../";
                this.ConfigPath = "../Config";
                this.UpdatePath = "../Update";
                this.LogPath = "../Log";
                this.WorkPath = "../Work";
                this.LanguagesPath = "./Resources/Languages";
            }
        }

        public class EnvironmentConfig
        {
            public CacheConfig Cache { get; private set; }

            public EnvironmentConfig()
            {
                this.Cache = new CacheConfig();
            }

            public class CacheConfig
            {
                public CacheConfig()
                {
                    this.BlocksPath = "../Config/Cache.blocks";
                }

                public string BlocksPath { get; private set; }
            }
        }
    }
}
