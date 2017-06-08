using System;
using System.IO;
using System.Windows.Media.Imaging;
using Nett;

namespace Amoeba.Interface
{
    class AmoebaEnvironment
    {
        public static Version Version { get; private set; }
        public static EnvironmentPaths Paths { get; private set; }
        public static EnvironmentIcons Icons { get; private set; }

        public static EnvironmentConfig Config { get; private set; }

        static AmoebaEnvironment()
        {
            Version = new Version(5, 0, 2);
            Paths = new EnvironmentPaths();
            Icons = new EnvironmentIcons();

            LoadConfig();
        }

        private static void LoadConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "Config.toml");

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
            public string DownloadsPath { get; private set; }
            public string UpdatePath { get; private set; }
            public string LogPath { get; private set; }
            public string WorkPath { get; private set; }
            public string LanguagesPath { get; private set; }

            public EnvironmentPaths()
            {
                this.BasePath = "../";
                this.ConfigPath = "../Config";
                this.DownloadsPath = "../Downloads";
                this.UpdatePath = "../Update";
                this.LogPath = "../Log";
                this.WorkPath = "../Work";
                this.LanguagesPath = "./Resources/Languages";
            }
        }

        public class EnvironmentIcons
        {
            public BitmapImage AmoebaIcon { get; }
            public BitmapImage BoxIcon { get; }
            public BitmapImage GreenIcon { get; }
            public BitmapImage RedIcon { get; }
            public BitmapImage YelloIcon { get; }

            public EnvironmentIcons()
            {
                this.AmoebaIcon = GetIcon("Amoeba.ico");
                this.BoxIcon = GetIcon("Files/Box.ico");
                this.GreenIcon = GetIcon("States/Green.png");
                this.RedIcon = GetIcon("States/Red.png");
                this.YelloIcon = GetIcon("States/Yello.png");
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

        public class EnvironmentConfig
        {
            public CacheConfig Cache { get; private set; }
            public ColorsConfig Colors { get; private set; }

            public EnvironmentConfig()
            {
                this.Cache = new CacheConfig();
                this.Colors = new ColorsConfig();
            }

            public class CacheConfig
            {
                public CacheConfig()
                {
                    this.BlocksPath = "../Config/Cache.blocks";
                }

                public string BlocksPath { get; private set; }
            }

            public class ColorsConfig
            {
                public ColorsConfig()
                {
                    this.Tree_Hit = System.Windows.Media.Colors.LightPink.ToString();
                    this.Link = System.Windows.Media.Colors.SkyBlue.ToString();
                    this.Link_New = System.Windows.Media.Colors.LightPink.ToString();
                    this.Message_Trust = System.Windows.Media.Colors.SkyBlue.ToString();
                    this.Message_Untrust = System.Windows.Media.Colors.LightPink.ToString();
                }

                public string Tree_Hit { get; set; }
                public string Link { get; set; }
                public string Link_New { get; set; }
                public string Message_Trust { get; set; }
                public string Message_Untrust { get; set; }
            }
        }
    }
}
