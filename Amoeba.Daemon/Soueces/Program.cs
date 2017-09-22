using System;
using System.IO;
using System.Net;
using Amoeba.Rpc;
using Amoeba.Service;
using Omnius.Base;

namespace Amoeba.Daemon
{
    class Program
    {
        static void Main(string[] args)
        {
            Init();

            string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Service");
            if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

            using (var bufferManager = new BufferManager(1024 * 1024 * 256, 1024 * 1024 * 256))
            using (var serviceManager = new ServiceManager(configPath, AmoebaEnvironment.Config.Cache.BlocksPath, bufferManager))
            {
                serviceManager.Load();
                serviceManager.Start();

                try
                {
                    using (var server = new AmoebaDaemonManager())
                    {
                        var info = UriUtils.Parse(AmoebaEnvironment.Config.Daemon.ListenUri);
                        var endpoint = new IPEndPoint(IPAddress.Parse(info.GetValue<string>("Address")), info.GetValue<int>("Port"));

                        server.Watch(serviceManager, endpoint).Wait();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                serviceManager.Stop();
                serviceManager.Save();
            }
        }

        private static void Init()
        {
            // 既定のフォルダを作成する。
            {
                foreach (var propertyInfo in typeof(AmoebaEnvironment.EnvironmentPaths).GetProperties())
                {
                    string path = propertyInfo.GetValue(AmoebaEnvironment.Paths) as string;
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                }
            }

            // Tempフォルダを環境変数に登録。
            {
                // Tempフォルダ内を掃除。
                try
                {
                    foreach (string path in Directory.GetFiles(AmoebaEnvironment.Paths.TempPath, "*", SearchOption.AllDirectories))
                    {
                        File.Delete(path);
                    }

                    foreach (string path in Directory.GetDirectories(AmoebaEnvironment.Paths.TempPath, "*", SearchOption.AllDirectories))
                    {
                        Directory.Delete(path, true);
                    }
                }
                catch (Exception)
                {

                }

                Environment.SetEnvironmentVariable("TMP", Path.GetFullPath(AmoebaEnvironment.Paths.TempPath), EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("TEMP", Path.GetFullPath(AmoebaEnvironment.Paths.TempPath), EnvironmentVariableTarget.Process);
            }
        }
    }
}
