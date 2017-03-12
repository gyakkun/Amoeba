using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Core;
using Omnius.Base;
using Omnius.Io;
using Omnius.Net;
using Omnius.Security;

namespace Amoeba.IntegrationTest
{
    class Test_CoreManager
    {
        private BufferManager _bufferManager = BufferManager.Instance;

        private readonly string _workPath = "Test_CoreManager";
        private readonly Random _random = new Random();

        public void Setup()
        {
            if (Directory.Exists(_workPath)) Directory.Delete(_workPath, true);
            Directory.CreateDirectory(_workPath);
        }

        public void Shutdown()
        {
            if (Directory.Exists(_workPath)) Directory.Delete(_workPath, true);
        }

        private DisposeWrapper<CoreManager> CreateCoreManager(int type)
        {
            var targetPath = Path.Combine(_workPath, type.ToString());
            var configPath = Path.Combine(targetPath, "CoreManager");
            var blockPath = Path.Combine(targetPath, "cache.blocks"); ;

            Directory.CreateDirectory(targetPath);
            Directory.CreateDirectory(configPath);

            var coreManager = new CoreManager(configPath, blockPath, _bufferManager);
            coreManager.Load();

            var listener = new TcpListener(IPAddress.Loopback, type);
            listener.Start(8);

            // ConnectionSetting
            {
                coreManager.AcceptCapEvent += (_) => this.AcceptCap(listener);
                coreManager.ConnectCapEvent += (_, uri) => this.ConnectCap(uri);
            }

            coreManager.SetMyLocation(new Location(new string[] { $"{IPAddress.Loopback}:{type}" }));

            return new DisposeWrapper<CoreManager>(coreManager, () =>
            {
                listener.Stop();
                listener.Server.Dispose();

                coreManager.Stop();
                coreManager.Dispose();
            });
        }

        private Cap AcceptCap(TcpListener listener)
        {
            try
            {
                if (!listener.Pending()) return null;
                return new SocketCap(listener.AcceptSocket());
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Cap ConnectCap(string uri)
        {
            try
            {
                var regex = new Regex(@"(.*?):(.*)");
                var match = regex.Match(uri);

                var ipAddress = IPAddress.Parse(match.Groups[1].Value);
                var port = int.Parse(match.Groups[2].Value);

                return new SocketCap(Connect(new IPEndPoint(ipAddress, port), new TimeSpan(0, 10, 0)));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Socket Connect(IPEndPoint remoteEndPoint, TimeSpan timeout)
        {
            Socket socket = null;

            try
            {
                socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.ReceiveTimeout = (int)timeout.TotalMilliseconds;
                socket.SendTimeout = (int)timeout.TotalMilliseconds;

                socket.Connect(remoteEndPoint);

                return socket;
            }
            catch (SocketException)
            {
                if (socket != null) socket.Dispose();
            }

            throw new Exception();
        }

        public void Test_SendReceive()
        {
            var wrapperList = new List<DisposeWrapper<CoreManager>>();

            try
            {
                for (int i = 0; i < 32; i++)
                {
                    var wrapper = this.CreateCoreManager(60000 + i);
                    wrapper.Value.Start();
                    wrapperList.Add(wrapper);
                }

                foreach (var wrapper in wrapperList.Skip(1))
                {
                    wrapper.Value.SetCrowdLocations(new Location[] { wrapperList[0].Value.MyLocation });
                }

                //this.MetadataUploadAndDownload(wrapperList.Select(n => n.Value));
                this.MessageUploadAndDownload(wrapperList.Select(n => n.Value));
            }
            finally
            {
                foreach (var wrapper in wrapperList)
                {
                    wrapper.Dispose();
                }
            }
        }

        private void MetadataUploadAndDownload(IEnumerable<CoreManager> coreManagers)
        {
            Console.WriteLine("----- CoreManager Metadata Send and Receive Test -----");
            Console.WriteLine();

            var sw = Stopwatch.StartNew();

            var coreManagerList = coreManagers.ToList();

            foreach (var coreManager in coreManagerList)
            {
                coreManager.Resize(1024 * 1024 * 256);
            }

            var broadcastMetadataList = new List<BroadcastMetadata>();
            var unicastMetadataList = new List<UnicastMetadata>();
            var multicastMetadataList = new List<MulticastMetadata>();

            for (int i = 0; i < 8; i++)
            {
                BroadcastMetadata broadcastMetadata;
                {
                    var digitalSignature = new DigitalSignature("Test", DigitalSignatureAlgorithm.EcDsaP521_Sha256);
                    var metadata = new Metadata(0, new Hash(HashAlgorithm.Sha256, _random.GetBytes(32)));
                    broadcastMetadata = new BroadcastMetadata("Test", DateTime.UtcNow, metadata, digitalSignature);
                }

                coreManagerList[_random.Next(1, coreManagerList.Count)].UploadMetadata(broadcastMetadata);
                broadcastMetadataList.Add(broadcastMetadata);
            }

            for (int i = 0; i < 8; i++)
            {
                UnicastMetadata unicastMetadata;
                {
                    var targetSignature = new DigitalSignature("Test", DigitalSignatureAlgorithm.EcDsaP521_Sha256).GetSignature();
                    var digitalSignature = new DigitalSignature("Test", DigitalSignatureAlgorithm.EcDsaP521_Sha256);
                    var metadata = new Metadata(0, new Hash(HashAlgorithm.Sha256, _random.GetBytes(32)));
                    unicastMetadata = new UnicastMetadata("Test", targetSignature, DateTime.UtcNow, metadata, digitalSignature);
                }

                coreManagerList[_random.Next(1, coreManagerList.Count)].UploadMetadata(unicastMetadata);
                unicastMetadataList.Add(unicastMetadata);
            }

            for (int i = 0; i < 8; i++)
            {
                MulticastMetadata multicastMetadata;
                using (var tokenSource = new CancellationTokenSource())
                {
                    var tag = new Tag("Test", _random.GetBytes(32));
                    var digitalSignature = new DigitalSignature("Test", DigitalSignatureAlgorithm.EcDsaP521_Sha256);
                    var metadata = new Metadata(0, new Hash(HashAlgorithm.Sha256, _random.GetBytes(32)));
                    multicastMetadata = new MulticastMetadata("Test", tag, DateTime.UtcNow, metadata, digitalSignature, null, tokenSource.Token);
                }

                coreManagerList[_random.Next(1, coreManagerList.Count)].UploadMetadata(multicastMetadata);
                multicastMetadataList.Add(multicastMetadata);
            }

            {
                var targetCoreManager = coreManagerList[0];

                for (;;)
                {
                    Thread.Sleep(1000);

                    foreach (var broadcastMetadata in broadcastMetadataList.ToArray())
                    {
                        if (targetCoreManager.GetBroadcastMetadata(broadcastMetadata.Certificate.GetSignature(), "Test") == broadcastMetadata)
                        {
                            broadcastMetadataList.Remove(broadcastMetadata);

                            Console.WriteLine($"{sw.Elapsed.ToString("hh\\:mm\\:ss")}: Success BroadcastMetadata");
                        }
                    }

                    foreach (var unicastMetadata in unicastMetadataList.ToArray())
                    {
                        if (targetCoreManager.GetUnicastMetadatas(unicastMetadata.Signature, "Test").Contains(unicastMetadata))
                        {
                            unicastMetadataList.Remove(unicastMetadata);

                            Console.WriteLine($"{sw.Elapsed.ToString("hh\\:mm\\:ss")}: Success UnicastMetadata");
                        }
                    }

                    foreach (var multicastMetadata in multicastMetadataList.ToArray())
                    {
                        if (targetCoreManager.GetMulticastMetadatas(multicastMetadata.Tag, "Test").Contains(multicastMetadata))
                        {
                            multicastMetadataList.Remove(multicastMetadata);

                            Console.WriteLine($"{sw.Elapsed.ToString("hh\\:mm\\:ss")}: Success MulticastMetadata");
                        }
                    }

                    if (broadcastMetadataList.Count == 0 && unicastMetadataList.Count == 0 && multicastMetadataList.Count == 0) break;
                }
            }

            Console.WriteLine();
        }

        private void MessageUploadAndDownload(IEnumerable<CoreManager> coreManagers)
        {
            Console.WriteLine("----- CoreManager Message Send and Receive Test -----");
            Console.WriteLine();

            var sw = Stopwatch.StartNew();

            var coreManagerList = coreManagers.ToList();

            foreach (var coreManager in coreManagerList)
            {
                coreManager.Resize(1024 * 1024 * 1024);
            }

            var metadataList = new List<Metadata>();

            Parallel.For(1, coreManagerList.Count, i =>
            {
                var random = RandomProvider.GetThreadRandom();

                using (var stream = new BufferStream(_bufferManager))
                {
                    using (var safeBuffer = _bufferManager.CreateSafeBuffer(1024 * 4))
                    {
                        for (int remain = _random.Next(1, 1024 * 1024 * 4); remain > 0; remain = Math.Max(0, remain - safeBuffer.Value.Length))
                        {
                            int length = Math.Min(remain, safeBuffer.Value.Length);

                            random.NextBytes(safeBuffer.Value);
                            stream.Write(safeBuffer.Value, 0, safeBuffer.Value.Length);
                        }
                    }

                    //stream.Seek(0, SeekOrigin.Begin);
                    //Console.WriteLine($"{sw.Elapsed.ToString("hh\\:mm\\:ss")}: Upload {NetworkConverter.ToBase64UrlString(Sha256.ComputeHash(new WrapperStream(stream, true)))}");

                    stream.Seek(0, SeekOrigin.Begin);

                    using (var tokenSource = new CancellationTokenSource())
                    {
                        metadataList.Add(coreManagerList[i].Import(stream, new TimeSpan(1, 0, 0), tokenSource.Token).Result);
                    }
                }
            });

            Parallel.ForEach(metadataList, metadata =>
            {
                for (;;)
                {
                    Thread.Sleep(1000);

                    Stream stream = null;

                    try
                    {
                        stream = coreManagerList[0].GetStream(metadata, 1024 * 1024 * 32).Result;
                        if (stream == null) continue;

                        if (!Unsafe.Equals(metadata.Hash.Value, Sha256.ComputeHash(stream))) throw new ArgumentException("Broken");
                        Console.WriteLine($"{sw.Elapsed.ToString("hh\\:mm\\:ss")}: Success {NetworkConverter.ToBase64UrlString(metadata.Hash.Value)}");

                        return;
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Dispose();
                        }
                    }
                }
            });

            Console.WriteLine();
        }
    }
}
