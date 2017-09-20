using System.Collections.Generic;
using System.IO;
using System.Net;
using Amoeba.Messages;
using Amoeba.Rpc;

namespace Amoeba.Interface
{
    static class Amoeba
    {
        private static AmoebaInterfaceManager _serviceManager;
        private static MessageManager _messageManager;
        private static WatchManager _watchManager;

        static Amoeba()
        {

        }

        public static AmoebaInterfaceManager Service { get => _serviceManager; }
        public static MessageManager Message { get => _messageManager; }

        public static void Run()
        {
            SettingsManager.Instance.Load();

            {
                _serviceManager = new AmoebaInterfaceManager();
                _serviceManager.Connect(new IPEndPoint(IPAddress.Loopback, 4040));

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
            _serviceManager.Dispose();
        }
    }
}
