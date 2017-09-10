using System;
using System.IO;
using System.Reflection;
using Omnius.Base;

namespace Amoeba.Interface
{
    class AmoebaEnvironment
    {
        public static Version Version { get; private set; }
        public static EnvironmentPaths Paths { get; private set; }

        static AmoebaEnvironment()
        {
            try
            {
                Version = new Version(5, 0, 0);
                Paths = new EnvironmentPaths();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public class EnvironmentPaths
        {
            public string CorePath { get; private set; }
            public string TempPath { get; private set; }
            public string ConfigPath { get; private set; }
            public string DownloadsPath { get; private set; }
            public string UpdatePath { get; private set; }
            public string LogPath { get; private set; }
            public string WorkPath { get; private set; }
            public string LanguagesPath { get; private set; }

            public EnvironmentPaths()
            {
                string corePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                this.CorePath = corePath;
                this.TempPath = Path.GetFullPath(Path.Combine(corePath, "../Temp"));
                this.ConfigPath = Path.GetFullPath(Path.Combine(corePath, "../Config"));
                this.DownloadsPath = Path.GetFullPath(Path.Combine(corePath, "../Downloads"));
                this.UpdatePath = Path.GetFullPath(Path.Combine(corePath, "../Update"));
                this.LogPath = Path.GetFullPath(Path.Combine(corePath, "../Log"));
                this.WorkPath = Path.GetFullPath(Path.Combine(corePath, "../Work"));
                this.LanguagesPath = Path.GetFullPath(Path.Combine(corePath, "./Resources/Languages"));
            }
        }
    }
}
