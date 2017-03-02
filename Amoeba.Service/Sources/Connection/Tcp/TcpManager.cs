using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Amoeba.Core;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Net;
using Omnius.Utilities;

namespace Amoeba.Service
{
    class TcpManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CatharsisManager _catharsisManager;

        private Settings _settings;

        private ConnectionTcpConfig _config;

        private WatchTimer _watchTimer;

        private List<string> _locationUris;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public TcpManager(string configPath, CatharsisManager catharsisManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _catharsisManager = catharsisManager;

            _settings = new Settings(configPath);

            _watchTimer = new WatchTimer(this.WatchThread);
        }

        public ConnectionTcpConfig Config
        {
            get
            {
                lock (_lockObject)
                {
                    return _config;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _config = value;
                }

                _watchTimer.Run();
            }
        }

        public IEnumerable<string> LocationUris
        {
            get
            {
                lock (_lockObject)
                {
                    return _locationUris.ToArray();
                }
            }
        }

        public Cap ConnectCap(string uri)
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            if (!uri.StartsWith("tcp:")) return null;

            try
            {
                var config = this.Config;

                string scheme = null;
                string address = null;
                int port = 4050;
                if (!ConnectionUtils.ParseUri(uri, out scheme, out address, ref port)) return null;

                if (!string.IsNullOrWhiteSpace(config.Socks5ProxyUri))
                {
                    string proxyScheme = null;
                    string proxyaddress = null;
                    int proxyPort = 1080;
                    if (!ConnectionUtils.ParseUri(config.Socks5ProxyUri, out proxyScheme, out proxyaddress, ref proxyPort)) return null;

                }
            }
            catch (Exception)
            {

            }

            return null;
        }

        public Cap AcceptCap()
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            Socket socket = null;

            try
            {
                string base32Address;

                socket = _samManager.Accept(out base32Address);
            }
            catch (SamException)
            {
                if (socket != null) socket.Dispose();

                return null;
            }

            return new SocketCap(socket);
        }

        private void WatchThread()
        {
            var checkStopwatch = new Stopwatch();
            checkStopwatch.Start();

            string nowSamBridgeUri = null;

            for (;;)
            {
                Thread.Sleep(1000);
                if (this.State == ManagerState.Stop) return;

                if (!checkStopwatch.IsRunning || checkStopwatch.Elapsed.TotalSeconds >= 30)
                {
                    checkStopwatch.Restart();

                    var targetSamBridgeUri = this.SamBridgeUri;

                    if ((_samManager == null || !_samManager.IsConnected)
                        || nowSamBridgeUri != targetSamBridgeUri)
                    {
                        string i2pUri = null;

                        try
                        {
                            var match = _regex.Match(targetSamBridgeUri);
                            if (!match.Success) throw new Exception();

                            if (match.Groups[1].Value == "tcp")
                            {
                                {
                                    if (_samManager != null)
                                    {
                                        _samManager.Dispose();
                                        _samManager = null;
                                    }

                                    var host = match.Groups[2].Value;
                                    var port = int.Parse(match.Groups[3].Value);

                                    _samManager = new SamManager(host, port, "Amoeba");
                                }

                                var base32Address = _samManager.Start();

                                if (base32Address != null)
                                {
                                    i2pUri = string.Format("i2p:{0}", base32Address);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            if (_samManager != null)
                            {
                                _samManager.Dispose();
                                _samManager = null;
                            }
                        }

                        lock (_lockObject)
                        {
                            _locationUris.Clear();
                            if (i2pUri != null) _locationUris.Add(i2pUri);
                        }

                        nowSamBridgeUri = targetSamBridgeUri;
                    }
                }
            }
        }

        public string GetIpv4Uri(ushort port)
        {
            try
            {
                var myIpAddresses = new List<IPAddress>(TcpManager.GetIpAddresses());

                foreach (var myIpAddress in myIpAddresses.Where(n => n.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                {
                    if (IPAddress.Any.ToString() == myIpAddress.ToString()
                        || IPAddress.Loopback.ToString() == myIpAddress.ToString()
                        || IPAddress.Broadcast.ToString() == myIpAddress.ToString())
                    {
                        continue;
                    }
                    if (TcpManager.IpAddressCompare(myIpAddress, IPAddress.Parse("10.0.0.0")) >= 0
                        && TcpManager.IpAddressCompare(myIpAddress, IPAddress.Parse("10.255.255.255")) <= 0)
                    {
                        continue;
                    }
                    if (TcpManager.IpAddressCompare(myIpAddress, IPAddress.Parse("172.16.0.0")) >= 0
                        && TcpManager.IpAddressCompare(myIpAddress, IPAddress.Parse("172.31.255.255")) <= 0)
                    {
                        continue;
                    }
                    if (TcpManager.IpAddressCompare(myIpAddress, IPAddress.Parse("127.0.0.0")) >= 0
                        && TcpManager.IpAddressCompare(myIpAddress, IPAddress.Parse("127.255.255.255")) <= 0)
                    {
                        continue;
                    }
                    if (TcpManager.IpAddressCompare(myIpAddress, IPAddress.Parse("192.168.0.0")) >= 0
                        && TcpManager.IpAddressCompare(myIpAddress, IPAddress.Parse("192.168.255.255")) <= 0)
                    {
                        continue;
                    }

                    ipv4Uri = string.Format("tcp:{0}:{1}", myIpAddress.ToString(), port);

                    break;
                }

                using (UpnpClient client = new UpnpClient())
                {
                    client.Connect(new TimeSpan(0, 0, 10));

                    string ip = client.GetExternalIpAddress(new TimeSpan(0, 0, 10));
                    if (string.IsNullOrWhiteSpace(ip)) throw new Exception();

                    upnpUri = string.Format("tcp:{0}:{1}", ip, port);

                    if (upnpUri != _settings.UpnpUri)
                    {
                        if (_settings.UpnpUri != null)
                        {
                            try
                            {
                                var match2 = regex.Match(_settings.UpnpUri);
                                if (!match2.Success) throw new Exception();
                                int port2 = int.Parse(match2.Groups[3].Value);

                                client.ClosePort(UpnpProtocolType.Tcp, port2, new TimeSpan(0, 0, 10));
                                Log.Information(string.Format("UPnP Close port: {0}", port2));
                            }
                            catch (Exception)
                            {

                            }
                        }

                        client.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 10));

                        if (client.OpenPort(UpnpProtocolType.Tcp, port, port, "Amoeba", new TimeSpan(0, 0, 10)))
                        {
                            Log.Information(string.Format("UPnP Open port: {0}", port));
                        }
                    }
                }

            }
            catch (Exception)
            {

            }
        }

        public string GetIpv6Uri(ushort port)
        {
            string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:[{0}]:", IPAddress.IPv6Any.ToString())));

            var regex = new Regex(@"(.*?):(.*):(\d*)");
            var match = regex.Match(uri);
            if (!match.Success) throw new Exception();

            int port = int.Parse(match.Groups[3].Value);

            var myIpAddresses = new List<IPAddress>(TcpManager.GetIpAddresses());

            foreach (var myIpAddress in myIpAddresses.Where(n => n.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))
            {
                if (IPAddress.IPv6Any.ToString() == myIpAddress.ToString()
                    || IPAddress.IPv6Loopback.ToString() == myIpAddress.ToString()
                    || IPAddress.IPv6None.ToString() == myIpAddress.ToString())
                {
                    continue;
                }
                if (myIpAddress.ToString().StartsWith("fe80:"))
                {
                    continue;
                }
                if (myIpAddress.ToString().StartsWith("2001:"))
                {
                    continue;
                }
                if (myIpAddress.ToString().StartsWith("2002:"))
                {
                    continue;
                }

                ipv6Uri = string.Format("tcp:[{0}]:{1}", myIpAddress.ToString(), port);

                break;
            }
        }

        private static IEnumerable<IPAddress> GetIpAddresses()
        {
            var list = new HashSet<IPAddress>();

            try
            {
                list.UnionWith(Dns.GetHostAddresses(Dns.GetHostName()));
            }
            catch (Exception)
            {

            }

            return list;
        }

        private static int IpAddressCompare(IPAddress x, IPAddress y)
        {
            return CollectionUtils.Compare(x.GetAddressBytes(), y.GetAddressBytes());
        }

        public override ManagerState State
        {
            get
            {
                return _state;
            }
        }

        private readonly object _stateLock = new object();

        public override void Start()
        {
            lock (_stateLock)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _watchTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 3, 0));
                }
            }
        }

        public override void Stop()
        {
            lock (_stateLock)
            {
                lock (_lockObject)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                _watchTimer.Stop();

                if (_samManager != null)
                {
                    _samManager.Dispose();
                    _samManager = null;
                }
            }
        }

        #region ISettings

        public void Load()
        {
            lock (_lockObject)
            {
                int version = _settings.Load("Version", () => 0);

                _config = _settings.Load<ConnectionTcpConfig>("Config");
            }
        }

        public void Save()
        {
            lock (_lockObject)
            {
                _settings.Save("Version", 0);

                _settings.Save("Config", _config);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_samManager != null)
                {
                    _samManager.Dispose();
                    _samManager = null;
                }
            }
        }
    }
}
