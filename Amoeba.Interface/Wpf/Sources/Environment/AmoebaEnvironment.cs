using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Nett;
using Omnius.Base;

namespace Amoeba.Interface
{
    class AmoebaEnvironment
    {
        public static Version Version { get; private set; }
        public static EnvironmentPaths Paths { get; private set; }
        public static EnvironmentIcons Icons { get; private set; }
        public static EnvironmentImages Images { get; private set; }

        public static EnvironmentConfig Config { get; private set; }

        static AmoebaEnvironment()
        {
            try
            {
                Version = new Version(5, 0, 61);
                Paths = new EnvironmentPaths();
                Icons = new EnvironmentIcons();
                Images = new EnvironmentImages();

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

            var version = oldConfig?.Version ?? AmoebaEnvironment.Version;
            var cache = oldConfig?.Cache ?? CreateDefaultCacheConfig();

            Toml.WriteFile(new EnvironmentConfig(AmoebaEnvironment.Version, cache), configPath, tomlSettings);
            Config = new EnvironmentConfig(version, cache);

            EnvironmentConfig.CacheConfig CreateDefaultCacheConfig()
            {
                return new EnvironmentConfig.CacheConfig("../Config/Cache.blocks");
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
            public string LanguagesPath { get; private set; }
            public string IconsPath { get; private set; }

            public EnvironmentPaths()
            {
                this.BasePath = "../";
                this.TempPath = "../Temp";
                this.ConfigPath = "../Config";
                this.DownloadsPath = "../Downloads";
                this.UpdatePath = "../Update";
                this.LogPath = "../Log";
                this.WorkPath = "../Work";
                this.LanguagesPath = "./Resources/Languages";
                this.IconsPath = "./Resources/Icons";
            }
        }

        public class EnvironmentIcons
        {
            public BitmapImage Amoeba { get; }
            public BitmapImage Box { get; }

            public EnvironmentIcons()
            {
                this.Amoeba = GetIcon("Amoeba.ico");
                this.Box = GetIcon("Files/Box.ico");
            }

            private static BitmapImage GetIcon(string path)
            {
                try
                {
                    var icon = new BitmapImage();

                    icon.BeginInit();
                    icon.StreamSource = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Resources/Icons/", path), FileMode.Open, FileAccess.Read, FileShare.Read);
                    icon.EndInit();
                    if (icon.CanFreeze) icon.Freeze();

                    return icon;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public class EnvironmentImages
        {
            public BitmapImage Amoeba { get; }
            public BitmapImage BlueBall { get; }
            public BitmapImage GreenBall { get; }
            public BitmapImage YelloBall { get; }

            public EnvironmentImages()
            {
                this.Amoeba = GetImage("Amoeba.png");
                this.BlueBall = GetImage("States/Blue.png");
                this.GreenBall = GetImage("States/Green.png");
                this.YelloBall = GetImage("States/Yello.png");
            }

            private static BitmapImage GetImage(string path)
            {
                try
                {
                    var icon = new BitmapImage();

                    icon.BeginInit();
                    icon.StreamSource = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Resources/Images/", path), FileMode.Open, FileAccess.Read, FileShare.Read);
                    icon.EndInit();
                    if (icon.CanFreeze) icon.Freeze();

                    return icon;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public class EnvironmentConfig
        {
            public Version Version { get; private set; }
            public CacheConfig Cache { get; private set; }

            public EnvironmentConfig() { }

            public EnvironmentConfig(Version version, CacheConfig cache)
            {
                this.Version = version;
                this.Cache = cache;
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
