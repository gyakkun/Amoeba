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
using Omnius.Net.Proxy;
using Omnius.Net.Upnp;
using Omnius.Utilities;

namespace Amoeba.Service
{
    class TcpManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CatharsisManager _catharsisManager;

        private Settings _settings;

        private ConnectionTcpConfig _config;

        private TcpListener _ipv4TcpListener;
        private TcpListener _ipv6TcpListener;

        private WatchTimer _watchTimer;

        private List<string> _locationUris = new List<string>();

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public TcpManager(string configPath, CatharsisManager catharsisManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _catharsisManager = catharsisManager;

            _settings = new Settings(configPath);

            _watchTimer = new WatchTimer(this.WatchListenerThread);
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

        private static bool CheckGlobalIpAddress(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                if (IPAddress.Any.ToString() == ipAddress.ToString()
                    || IPAddress.Loopback.ToString() == ipAddress.ToString()
                    || IPAddress.Broadcast.ToString() == ipAddress.ToString())
                {
                    return false;
                }
                if (CollectionUtils.Compare(ipAddress.GetAddressBytes(), IPAddress.Parse("10.0.0.0").GetAddressBytes()) >= 0
                    && CollectionUtils.Compare(ipAddress.GetAddressBytes(), IPAddress.Parse("10.255.255.255").GetAddressBytes()) <= 0)
                {
                    return false;
                }
                if (CollectionUtils.Compare(ipAddress.GetAddressBytes(), IPAddress.Parse("172.16.0.0").GetAddressBytes()) >= 0
                    && CollectionUtils.Compare(ipAddress.GetAddressBytes(), IPAddress.Parse("172.31.255.255").GetAddressBytes()) <= 0)
                {
                    return false;
                }
                if (CollectionUtils.Compare(ipAddress.GetAddressBytes(), IPAddress.Parse("127.0.0.0").GetAddressBytes()) >= 0
                    && CollectionUtils.Compare(ipAddress.GetAddressBytes(), IPAddress.Parse("127.255.255.255").GetAddressBytes()) <= 0)
                {
                    return false;
                }
                if (CollectionUtils.Compare(ipAddress.GetAddressBytes(), IPAddress.Parse("192.168.0.0").GetAddressBytes()) >= 0
                    && CollectionUtils.Compare(ipAddress.GetAddressBytes(), IPAddress.Parse("192.168.255.255").GetAddressBytes()) <= 0)
                {
                    return false;
                }
            }
            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (IPAddress.IPv6Any.ToString() == ipAddress.ToString()
                    || IPAddress.IPv6Loopback.ToString() == ipAddress.ToString()
                    || IPAddress.IPv6None.ToString() == ipAddress.ToString())
                {
                    return false;
                }
                if (ipAddress.ToString().StartsWith("fe80:"))
                {
                    return false;
                }
                if (ipAddress.ToString().StartsWith("2001:"))
                {
                    return false;
                }
                if (ipAddress.ToString().StartsWith("2002:"))
                {
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<IPAddress> GetMyGlobalIpAddresses()
        {
            var list = new HashSet<IPAddress>();

            try
            {
                list.UnionWith(Dns.GetHostAddresses(Dns.GetHostName()).Where(n => TcpManager.CheckGlobalIpAddress(n)));
            }
            catch (Exception)
            {

            }

            return list;
        }

        private static IPAddress GetIpAddress(string host)
        {
            if (!IPAddress.TryParse(host, out IPAddress remoteIp))
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(host);

                if (hostEntry.AddressList.Length > 0)
                {
                    remoteIp = hostEntry.AddressList[0];
                }
                else
                {
                    return null;
                }
            }

            return remoteIp;
        }

        private static Socket Connect(IPEndPoint remoteEndPoint, TimeSpan timeout)
        {
            Socket socket = null;

            try
            {
                socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.ReceiveTimeout = (int)timeout.TotalMilliseconds;
                socket.SendTimeout = (int)timeout.TotalMilliseconds;

                var asyncResult = socket.BeginConnect(remoteEndPoint, null, null);

                if (!asyncResult.IsCompleted)
                {
                    if (!asyncResult.AsyncWaitHandle.WaitOne(timeout, false))
                    {
                        throw new ConnectionException();
                    }
                }

                socket.EndConnect(asyncResult);

                return socket;
            }
            catch (Exception)
            {
                if (socket != null) socket.Dispose();
            }

            throw new Exception();
        }

        public Cap ConnectCap(string uri)
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            if (!uri.StartsWith("tcp:")) return null;

            var garbages = new List<IDisposable>();

            try
            {
                var config = this.Config;

                var result = UriUtils.Parse(uri);
                if (result == null) throw new Exception();

                var scheme = result.GetValue<string>("Scheme");
                if (scheme != "tcp") return null;

                var address = result.GetValue<string>("Address");
                var port = result.GetValueOrDefault<int>("Port", () => 4050);

                // Check
                {
                    if (!IPAddress.TryParse(address, out IPAddress ipAddress)) return null;

#if !DEBUG
                    if (!TcpManager.CheckGlobalIpAddress(ipAddress)) return null;
#endif

                    if (!_catharsisManager.Check(ipAddress)) return null;
                }

                if (string.IsNullOrWhiteSpace(config.ProxyUri))
                {
                    var result2 = UriUtils.Parse(config.ProxyUri);
                    if (result2 == null) throw new Exception();

                    var proxyScheme = result.GetValue<string>("Scheme");

                    if (proxyScheme == "socks5")
                    {
                        var proxyAddress = result.GetValue<string>("Address");
                        var proxyPort = result.GetValueOrDefault<int>("Port", () => 1080);

                        var socket = TcpManager.Connect(new IPEndPoint(TcpManager.GetIpAddress(proxyAddress), proxyPort), new TimeSpan(0, 0, 10));
                        garbages.Add(socket);

                        var proxy = new Socks5ProxyClient(null, null, address, port);
                        proxy.Create(socket, new TimeSpan(0, 0, 30));

                        var cap = new SocketCap(socket);
                        garbages.Add(cap);

                        return cap;
                    }
                }
                else
                {
                    var socket = TcpManager.Connect(new IPEndPoint(IPAddress.Parse(address), port), new TimeSpan(0, 0, 10));
                    garbages.Add(socket);

                    var cap = new SocketCap(socket);
                    garbages.Add(cap);

                    return cap;
                }
            }
            catch (Exception)
            {
                foreach (var item in garbages)
                {
                    item.Dispose();
                }
            }

            return null;
        }

        public Cap AcceptCap()
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            var garbages = new List<IDisposable>();


            try
            {
                var config = this.Config;

                foreach (var p in new int[] { 0, 1 }.Randomize())
                {
                    if (p == 0 && config.Type.HasFlag(ConnectionTcpType.Ipv4) && _ipv4TcpListener.Pending())
                    {
                        var socket = _ipv4TcpListener.AcceptSocket();
                        garbages.Add(socket);

                        // Check
                        {
                            var ipAddress = ((IPEndPoint)socket.RemoteEndPoint).Address;
                            if (!_catharsisManager.Check(ipAddress)) return null;
                        }

                        return new SocketCap(socket);
                    }

                    if (p == 1 && config.Type.HasFlag(ConnectionTcpType.Ipv6) && _ipv6TcpListener.Pending())
                    {
                        var socket = _ipv6TcpListener.AcceptSocket();
                        garbages.Add(socket);

                        // Check
                        {
                            var ipAddress = ((IPEndPoint)socket.RemoteEndPoint).Address;
                            if (!_catharsisManager.Check(ipAddress)) return null;
                        }

                        return new SocketCap(socket);
                    }
                }
            }
            catch (Exception)
            {
                foreach (var item in garbages)
                {
                    item.Dispose();
                }
            }

            return null;
        }

        private int _watchIpv4Port = -1;
        private int _watchIpv6Port = -1;

        private void WatchListenerThread()
        {
            for (;;)
            {
                ConnectionTcpConfig config = null;

                lock (_lockObject)
                {
                    config = this.Config;
                }

                string ipv4Uri = null;
                string ipv6Uri = null;

                if (config.Type.HasFlag(ConnectionTcpType.Ipv4))
                {
                    ipv4Uri = this.GetIpv4Uri(config.Ipv4Port);

                    if (_ipv4TcpListener == null || _watchIpv4Port != config.Ipv4Port)
                    {
                        if (_ipv4TcpListener != null)
                        {
                            _ipv4TcpListener.Server.Dispose();
                            _ipv4TcpListener.Stop();

                            _ipv4TcpListener = null;
                        }

                        _ipv4TcpListener = new TcpListener(IPAddress.Any, config.Ipv4Port);
                        _ipv4TcpListener.Start(3);

                        // Port forwarding
                        try
                        {
                            using (UpnpClient client = new UpnpClient())
                            {
                                client.Connect(new TimeSpan(0, 0, 10));

                                var ipAddress = IPAddress.Parse(client.GetExternalIpAddress(new TimeSpan(0, 0, 10)));
                                if (ipAddress == null || !TcpManager.CheckGlobalIpAddress(ipAddress)) throw new Exception();

                                client.ClosePort(UpnpProtocolType.Tcp, config.Ipv4Port, new TimeSpan(0, 0, 10));
                                client.OpenPort(UpnpProtocolType.Tcp, config.Ipv4Port, config.Ipv4Port, "Amoeba", new TimeSpan(0, 0, 10));
                            }
                        }
                        catch (Exception)
                        {

                        }

                        _watchIpv4Port = config.Ipv4Port;
                    }
                }
                else
                {
                    if (_ipv4TcpListener != null)
                    {
                        _ipv4TcpListener.Server.Dispose();
                        _ipv4TcpListener.Stop();

                        _ipv4TcpListener = null;
                    }
                }

                if (config.Type.HasFlag(ConnectionTcpType.Ipv6))
                {
                    ipv6Uri = this.GetIpv6Uri(config.Ipv6Port);

                    if (_ipv6TcpListener == null || _watchIpv6Port != config.Ipv6Port)
                    {
                        if (_ipv6TcpListener != null)
                        {
                            _ipv6TcpListener.Server.Dispose();
                            _ipv6TcpListener.Stop();

                            _ipv6TcpListener = null;
                        }

                        _ipv6TcpListener = new TcpListener(IPAddress.IPv6Any, config.Ipv6Port);
                        _ipv6TcpListener.Start(3);

                        _watchIpv6Port = config.Ipv6Port;
                    }
                }
                else
                {
                    if (_ipv6TcpListener != null)
                    {
                        _ipv6TcpListener.Server.Dispose();
                        _ipv6TcpListener.Stop();

                        _ipv6TcpListener = null;
                    }
                }

                lock (_lockObject)
                {
                    if (this.Config != config) continue;

                    _locationUris.Clear();
                    if (ipv4Uri != null) _locationUris.Add(ipv4Uri);
                    if (ipv6Uri != null) _locationUris.Add(ipv6Uri);
                }
            }
        }

        private string GetIpv4Uri(ushort port)
        {
            {
                var ipAddress = TcpManager.GetMyGlobalIpAddresses().FirstOrDefault(n => n.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                if (ipAddress != null)
                {
                    return string.Format("tcp:{0}:{1}", ipAddress.ToString(), port);
                }
            }

            try
            {
                using (UpnpClient client = new UpnpClient())
                {
                    client.Connect(new TimeSpan(0, 0, 10));

                    var ipAddress = IPAddress.Parse(client.GetExternalIpAddress(new TimeSpan(0, 0, 10)));
                    if (ipAddress == null || !TcpManager.CheckGlobalIpAddress(ipAddress)) throw new Exception();

                    return string.Format("tcp:{0}:{1}", ipAddress.ToString(), port);
                }
            }
            catch (Exception)
            {

            }

            return null;
        }

        private string GetIpv6Uri(ushort port)
        {
            var ipAddress = TcpManager.GetMyGlobalIpAddresses().FirstOrDefault(n => n.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);

            if (ipAddress != null)
            {
                return string.Format("tcp:[{0}]:{1}", ipAddress.ToString(), port);
            }

            return null;
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

                    _watchTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 30, 0));
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

                if (_ipv4TcpListener != null)
                {
                    _ipv4TcpListener.Server.Dispose();
                    _ipv4TcpListener.Stop();

                    _ipv4TcpListener = null;
                }

                if (_ipv6TcpListener != null)
                {
                    _ipv6TcpListener.Server.Dispose();
                    _ipv6TcpListener.Stop();

                    _ipv6TcpListener = null;
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
                if (_watchTimer != null)
                {
                    _watchTimer.Dispose();
                    _watchTimer = null;
                }

                if (_ipv4TcpListener != null)
                {
                    _ipv4TcpListener.Server.Dispose();
                    _ipv4TcpListener.Stop();

                    _ipv4TcpListener = null;
                }

                if (_ipv6TcpListener != null)
                {
                    _ipv6TcpListener.Server.Dispose();
                    _ipv6TcpListener.Stop();

                    _ipv6TcpListener = null;
                }
            }
        }
    }
}
