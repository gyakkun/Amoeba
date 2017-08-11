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
        public static EnvironmentVariables Variables { get; private set; }
        public static EnvironmentPaths Paths { get; private set; }
        public static EnvironmentIcons Icons { get; private set; }
        public static EnvironmentImages Images { get; private set; }

        public static EnvironmentConfig Config { get; private set; }

        static AmoebaEnvironment()
        {
            try
            {
                Variables = new EnvironmentVariables();
                Version = new Version(5, 0, 27);
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

            if (!File.Exists(configPath))
            {
                Toml.WriteFile(new EnvironmentConfig(), configPath);
            }

            Config = Toml.ReadFile<EnvironmentConfig>(configPath);
        }

        public class EnvironmentVariables
        {
            public double CaptionHeight { get; } = 8;
            public Thickness ResizeBorderThickness { get; } = SystemParameters.WindowResizeBorderThickness;
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

            public EnvironmentImages()
            {
                this.Amoeba = GetImage("Amoeba.png");
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
            public CacheConfig Cache { get; private set; }
            public ColorsConfig Colors { get; private set; }

            public EnvironmentConfig()
            {
                this.Cache = new CacheConfig();
                this.Colors = new ColorsConfig();
            }

            public class CacheConfig
            {
                public string BlocksPath { get; private set; }

                public CacheConfig()
                {
                    this.BlocksPath = "../Config/Cache.blocks";
                }
            }

            public class ColorsConfig
            {
                public string Tree_Hit { get; set; }
                public string Link_New { get; set; }
                public string Link_Visited { get; set; }
                public string Message_Trust { get; set; }
                public string Message_Untrust { get; set; }

                public ColorsConfig()
                {
                    this.Tree_Hit = System.Windows.Media.Colors.LightPink.ToString();
                    this.Link_New = System.Windows.Media.Colors.LightPink.ToString();
                    this.Link_Visited = System.Windows.Media.Colors.SkyBlue.ToString();
                    this.Message_Trust = System.Windows.Media.Colors.SkyBlue.ToString();
                    this.Message_Untrust = System.Windows.Media.Colors.LightPink.ToString();
                }
            }
        }
    }
}
