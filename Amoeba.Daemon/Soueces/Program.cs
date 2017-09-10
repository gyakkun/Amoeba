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
            string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Service");
            if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

            using (var bufferManager = new BufferManager(1024 * 1024 * 256, 1024 * 1024 * 256))
            using (var serviceManager = new ServiceManager(configPath, AmoebaEnvironment.Config.Cache.BlocksPath, bufferManager))
            {
                serviceManager.Load();
                serviceManager.Start();

                try
                {
                    using (var server = new AmoebaServerManager())
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
    }
}
