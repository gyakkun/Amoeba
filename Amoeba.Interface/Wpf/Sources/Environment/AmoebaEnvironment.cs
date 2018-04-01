using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using Nett;
using Omnius.Base;

namespace Amoeba.Interface
{
    class AmoebaEnvironment
    {
        public static Version Version { get; private set; }
        public static PathsEnvironment Paths { get; private set; }
        public static IconsEnvironment Icons { get; private set; }
        public static ImagesEnvironment Images { get; private set; }

        public static InterfaceConfig Config { get; private set; }

        static AmoebaEnvironment()
        {
            try
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

                Version = new Version(5, 1, 6);
                Paths = new PathsEnvironment();
                Icons = new IconsEnvironment();
                Images = new ImagesEnvironment();

                LoadConfig();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static void LoadConfig()
        {
            string configPath = Path.Combine(Paths.ConfigDirectoryPath, "Interface.toml");

            var tomlSettings = TomlSettings.Create(builder => builder
                .ConfigureType<Version>(type => type
                    .WithConversionFor<TomlString>(convert => convert
                        .ToToml(tt => tt.ToString())
                        .FromToml(ft => Version.Parse(ft.Value)))));

            var oldConfig = File.Exists(configPath) ? Toml.ReadFile<InterfaceConfig>(configPath, tomlSettings) : null;

            var version = oldConfig?.Version ?? AmoebaEnvironment.Version;
            var communication = oldConfig?.Communication ?? CreateDefaultCommunicationConfig();

            Toml.WriteFile(new InterfaceConfig(AmoebaEnvironment.Version, communication), configPath, tomlSettings);
            Config = new InterfaceConfig(version, communication);

            InterfaceConfig.CommunicationConfig CreateDefaultCommunicationConfig()
            {
                return new InterfaceConfig.CommunicationConfig("tcp:127.0.0.1:4040");
            }
        }

        public class PathsEnvironment
        {
            public string BaseDirectoryPath { get; private set; }
            public string CoreDirectoryPath { get; private set; }
            public string TempDirectoryPath { get; private set; }
            public string ConfigDirectoryPath { get; private set; }
            public string DownloadsDirectoryPath { get; private set; }
            public string UpdateDirectoryPath { get; private set; }
            public string LogDirectoryPath { get; private set; }
            public string WorkDirectoryPath { get; private set; }
            public string DaemonDirectoryPath { get; private set; }
            public string InterfaceDirectoryPath { get; private set; }
            public string LanguagesDirectoryPath { get; private set; }
            public string IconsDirectoryPath { get; private set; }

            public PathsEnvironment()
            {
                this.BaseDirectoryPath = "../../";
                this.CoreDirectoryPath = "../../Core";
                this.TempDirectoryPath = "../../Temp";
                this.ConfigDirectoryPath = "../../Config";
                this.DownloadsDirectoryPath = "../../Downloads";
                this.UpdateDirectoryPath = "../../Update";
                this.LogDirectoryPath = "../../Log";
                this.WorkDirectoryPath = "../../Work";
                this.InterfaceDirectoryPath = "../Interface";
                this.LanguagesDirectoryPath = "./Resources/Languages";
                this.IconsDirectoryPath = "./Resources/Icons";

#if DEBUG
                this.DaemonDirectoryPath = "../Daemon";
#else
                if (Environment.Is64BitProcess)
                {
                    this.DaemonDirectoryPath = "../Daemon-x64";
                }
                else
                {
                    this.DaemonDirectoryPath = "../Daemon-x86";
                }
#endif
            }
        }

        public class IconsEnvironment
        {
            public BitmapImage Amoeba { get; }
            public BitmapImage Box { get; }

            public IconsEnvironment()
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

        public class ImagesEnvironment
        {
            public BitmapImage Amoeba { get; }
            public BitmapImage BlueBall { get; }
            public BitmapImage GreenBall { get; }
            public BitmapImage YelloBall { get; }

            public ImagesEnvironment()
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

        public class InterfaceConfig
        {
            public InterfaceConfig() { }

            public InterfaceConfig(Version version, CommunicationConfig communication)
            {
                this.Version = version;
                this.Communication = communication;
            }

            public Version Version { get; private set; }
            public CommunicationConfig Communication { get; private set; }

            public class CommunicationConfig
            {
                public CommunicationConfig() { }

                public CommunicationConfig(string targetUri)
                {
                    this.TargetUri = targetUri;
                }

                public string TargetUri { get; private set; }
            }
        }
    }
}
