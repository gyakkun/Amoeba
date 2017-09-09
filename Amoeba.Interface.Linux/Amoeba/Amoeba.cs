using System.Collections.Generic;
using System.IO;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;

namespace Amoeba.Interface
{
    static class Amoeba
    {
        private static ServiceManager _serviceManager;
        private static MessageManager _messageManager;
        private static WatchManager _watchManager;

        static Amoeba()
        {

        }

        public static ServiceManager Service { get => _serviceManager; }
        public static MessageManager Message { get => _messageManager; }

        public static void Run()
        {
            SettingsManager.Instance.Load();

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Service");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _serviceManager = new ServiceManager(configPath, Path.Combine(AmoebaEnvironment.Paths.CorePath, AmoebaEnvironment.Config.Cache.BlocksPath), BufferManager.Instance);
                _serviceManager.Load();

                {
                    var locations = new List<Location>();

                    using (var reader = new StreamReader(Path.Combine(AmoebaEnvironment.Paths.CorePath, "Locations.txt")))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            locations.Add(AmoebaConverter.FromLocationString(line));
                        }
                    }

                    _serviceManager.SetCloudLocations(locations);
                }

                if (_serviceManager.Config.Core.Download.BasePath == null)
                {
                    var oldConfig = _serviceManager.Config;
                    _serviceManager.SetConfig(new ServiceConfig(new CoreConfig(oldConfig.Core.Network, new DownloadConfig(AmoebaEnvironment.Paths.DownloadsPath)), oldConfig.Connection, oldConfig.Message));
                }

                _serviceManager.Start();
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "Control", "Message");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _messageManager = new MessageManager(configPath, _serviceManager);
                _messageManager.Load();
            }

            {
                _watchManager = new WatchManager(_serviceManager);
                _watchManager.SaveEvent += () =>
                {
                    _serviceManager.Save();
                    _messageManager.Save();
                };
            }
        }

        public static void Exit()
        {
            _watchManager.Dispose();

            _messageManager.Save();
            _messageManager.Dispose();

            _serviceManager.Stop();
            _serviceManager.Save();
            _serviceManager.Dispose();
        }
    }
}
