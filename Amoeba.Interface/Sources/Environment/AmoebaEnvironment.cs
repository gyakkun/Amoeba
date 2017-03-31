using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Nett;
using System.Windows.Media.Imaging;

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
			Version = new Version(5, 0, 0);
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

		public class EnvironmentIcons
		{
			public BitmapImage AmoebaIcon { get; }

			public EnvironmentIcons()
			{
				this.AmoebaIcon = GetIcon("Amoeba.ico");
			}

			private static BitmapImage GetIcon(string path)
			{
				var icon = new BitmapImage();

				icon.BeginInit();
				icon.StreamSource = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "Resources/Icons/", path), FileMode.Open, FileAccess.Read, FileShare.Read);
				icon.EndInit();
				if (icon.CanFreeze) icon.Freeze();

				return icon;
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
