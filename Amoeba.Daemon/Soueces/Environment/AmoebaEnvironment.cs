using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Omnius.Base;
using Omnius.Toml;

namespace Amoeba.Daemon
{
    class AmoebaEnvironment
    {
        public static Version Version { get; private set; }
        public static EnvironmentPaths Paths { get; private set; }

        public static EnvironmentConfig Config { get; private set; }

        static AmoebaEnvironment()
        {
            try
            {
                Version = new Version(5, 0, 37);
                Paths = new EnvironmentPaths();

                LoadConfig();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void LoadConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "Config.toml");

            var tomlSettings = TomlSettings.Create(builder => builder
                .ConfigureType<Version>(type => type
                    .WithConversionFor<TomlString>(convert => convert
                        .ToToml(tt => tt.ToString())
                        .FromToml(ft => Version.Parse(ft.Value)))));

            var oldConfig = File.Exists(configPath) ? Toml.ReadFile<EnvironmentConfig>(configPath, tomlSettings) : null;

            var version = oldConfig?.Version ?? new Version(0, 0, 0);
            var daemon = oldConfig?.Daemon ?? CreateDefaultDaemonConfig();
            var cache = oldConfig?.Cache ?? CreateDefaultCacheConfig();

            Toml.WriteFile(new EnvironmentConfig(AmoebaEnvironment.Version, daemon, cache), configPath, tomlSettings);
            Config = new EnvironmentConfig(version, daemon, cache);

            EnvironmentConfig.CacheConfig CreateDefaultCacheConfig()
            {
                return new EnvironmentConfig.CacheConfig("../Config/Cache.blocks");
            }

            EnvironmentConfig.DaemonConfig CreateDefaultDaemonConfig()
            {
                return new EnvironmentConfig.DaemonConfig("tcp:127.0.0.1:4040");
            }
        }

        public class EnvironmentPaths
        {
            public string BasePath { get; private set; }
            public string TempPath { get; private set; }
            public string ConfigPath { get; private set; }
            public string DownloadsPath { get; private set; }
            public string UpdatePath { get; private set; }
            public string LogPath { get; private set; }
            public string WorkPath { get; private set; }

            public EnvironmentPaths()
            {
                this.BasePath = "../";
                this.TempPath = "../Temp";
                this.ConfigPath = "../Config";
                this.DownloadsPath = "../Downloads";
                this.UpdatePath = "../Update";
                this.LogPath = "../Log";
                this.WorkPath = "../Work";
            }
        }

        public class EnvironmentConfig
        {
            public Version Version { get; private set; }
            public DaemonConfig Daemon { get; private set; }
            public CacheConfig Cache { get; private set; }

            public EnvironmentConfig() { }

            public EnvironmentConfig(Version version, DaemonConfig daemon, CacheConfig cache)
            {
                this.Version = version;
                this.Cache = cache;
                this.Daemon = daemon;
            }

            public class DaemonConfig
            {
                public string ListenUri { get; private set; }

                public DaemonConfig() { }

                public DaemonConfig(string listenUri)
                {
                    this.ListenUri = listenUri;
                }
            }

            public class CacheConfig
            {
                public string BlocksPath { get; private set; }

                public CacheConfig() { }

                public CacheConfig(string blocksPath)
                {
                    this.BlocksPath = blocksPath;
                }
            }
        }
    }
}
