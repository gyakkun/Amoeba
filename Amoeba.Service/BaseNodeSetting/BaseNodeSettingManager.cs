using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
using Library;
using Library.Net.Amoeba;
using Library.Net.Upnp;

namespace Amoeba
{
    class BaseNodeSettingManager : StateManagerBase
    {
        private AmoebaManager _amoebaManager;

        private string _ipv4Uri;
        private string _ipv6Uri;
        private string _upnpUri;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public BaseNodeSettingManager(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;
        }

        public override ManagerState State
        {
            get
            {
                return _state;
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

        private static bool Check(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                if (IPAddress.Any.ToString() == ipAddress.ToString()
                    || IPAddress.Loopback.ToString() == ipAddress.ToString()
                    || IPAddress.Broadcast.ToString() == ipAddress.ToString())
                {
                    return false;
                }
                if (BaseNodeSettingManager.IpAddressCompare(ipAddress, IPAddress.Parse("10.0.0.0")) >= 0
                    && BaseNodeSettingManager.IpAddressCompare(ipAddress, IPAddress.Parse("10.255.255.255")) <= 0)
                {
                    return false;
                }
                if (BaseNodeSettingManager.IpAddressCompare(ipAddress, IPAddress.Parse("172.16.0.0")) >= 0
                    && BaseNodeSettingManager.IpAddressCompare(ipAddress, IPAddress.Parse("172.31.255.255")) <= 0)
                {
                    return false;
                }
                if (BaseNodeSettingManager.IpAddressCompare(ipAddress, IPAddress.Parse("127.0.0.0")) >= 0
                    && BaseNodeSettingManager.IpAddressCompare(ipAddress, IPAddress.Parse("127.255.255.255")) <= 0)
                {
                    return false;
                }
                if (BaseNodeSettingManager.IpAddressCompare(ipAddress, IPAddress.Parse("192.168.0.0")) >= 0
                    && BaseNodeSettingManager.IpAddressCompare(ipAddress, IPAddress.Parse("192.168.255.255")) <= 0)
                {
                    return false;
                }
            }

            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
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

        private bool AddUri(string uri)
        {
            lock (_thisLock)
            {
                lock (_amoebaManager.ThisLock)
                {
                    var baseNode = _amoebaManager.BaseNode;

                    var uris = new List<string>(baseNode.Uris);
                    if (uris.Contains(uri) || uris.Count >= Node.MaxUriCount) return false;

                    uris.Add(uri);

                    _amoebaManager.SetBaseNode(new Node(baseNode.Id, uris));
                }
            }

            return true;
        }

        private bool RemoveUri(string uri)
        {
            lock (_thisLock)
            {
                lock (_amoebaManager.ThisLock)
                {
                    var baseNode = _amoebaManager.BaseNode;

                    var uris = new List<string>(baseNode.Uris);
                    if (!uris.Remove(uri)) return false;

                    _amoebaManager.SetBaseNode(new Node(baseNode.Id, uris));
                }
            }

            return true;
        }

        public void Update()
        {
            lock (_thisLock)
            {
                if (this.State == ManagerState.Stop) return;

                {
                    string ipv4Uri = null;

                    try
                    {
                        string listenUri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));

                        var regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(listenUri);
                        if (!match.Success) throw new Exception();

                        int port = int.Parse(match.Groups[3].Value);

                        var ipAddress = BaseNodeSettingManager.GetIpAddresses()
                            .Where(n => n.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .Where(n => BaseNodeSettingManager.Check(n))
                            .FirstOrDefault();

                        if (ipAddress != null) ipv4Uri = string.Format("tcp:{0}:{1}", ipAddress.ToString(), port);
                    }
                    catch (Exception)
                    {

                    }

                    if (ipv4Uri != _ipv4Uri)
                    {
                        if (this.RemoveUri(_ipv4Uri))
                        {
                            Log.Information(string.Format("Remove Node uri: {0}", _ipv4Uri));
                        }
                    }

                    _ipv4Uri = ipv4Uri;

                    if (_ipv4Uri != null)
                    {
                        if (this.AddUri(_ipv4Uri))
                        {
                            Log.Information(string.Format("Add Node uri: {0}", _ipv4Uri));
                        }
                    }
                }

                {
                    string ipv6Uri = null;

                    try
                    {
                        string listenUri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:[{0}]:", IPAddress.IPv6Any.ToString())));

                        var regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(listenUri);
                        if (!match.Success) throw new Exception();

                        int port = int.Parse(match.Groups[3].Value);

                        var ipAddress = BaseNodeSettingManager.GetIpAddresses()
                            .Where(n => n.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            .Where(n => BaseNodeSettingManager.Check(n))
                            .FirstOrDefault();

                        if (ipAddress != null) ipv6Uri = string.Format("tcp:[{0}]:{1}", ipAddress.ToString(), port);
                    }
                    catch (Exception)
                    {

                    }

                    if (ipv6Uri != _ipv6Uri)
                    {
                        if (this.RemoveUri(_ipv6Uri))
                        {
                            Log.Information(string.Format("Remove Node uri: {0}", _ipv6Uri));
                        }
                    }

                    _ipv6Uri = ipv6Uri;

                    if (_ipv6Uri != null)
                    {
                        if (this.AddUri(_ipv6Uri))
                        {
                            Log.Information(string.Format("Add Node uri: {0}", _ipv6Uri));
                        }
                    }
                }

                {
                    string upnpUri = null;

                    try
                    {
                        string listenUri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));

                        var regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(listenUri);
                        if (!match.Success) throw new Exception();

                        int port = int.Parse(match.Groups[3].Value);

                        using (UpnpClient client = new UpnpClient())
                        {
                            client.Connect(new TimeSpan(0, 0, 10));

                            IPAddress ipAddress;
                            {
                                string ip = client.GetExternalIpAddress(new TimeSpan(0, 0, 10));
                                if (string.IsNullOrWhiteSpace(ip)) throw new Exception();

                                ipAddress = IPAddress.Parse(ip);

                                if (!BaseNodeSettingManager.Check(ipAddress)) throw new Exception();
                            }

                            upnpUri = string.Format("tcp:{0}:{1}", ipAddress.ToString(), port);

                            if (upnpUri != _upnpUri)
                            {
                                if (_upnpUri != null)
                                {
                                    try
                                    {
                                        var match2 = regex.Match(_upnpUri);
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

                    if (upnpUri != _upnpUri)
                    {
                        if (this.RemoveUri(_upnpUri))
                        {
                            Log.Information(string.Format("Remove Node uri: {0}", _upnpUri));
                        }
                    }

                    _upnpUri = upnpUri;

                    if (_upnpUri != null)
                    {
                        if (this.AddUri(_upnpUri))
                        {
                            Log.Information(string.Format("Add Node uri: {0}", _upnpUri));
                        }
                    }
                }
            }
        }

        private void Shutdown()
        {
            lock (_thisLock)
            {
                if (_ipv4Uri != null)
                {
                    if (this.RemoveUri(_ipv4Uri))
                    {
                        Log.Information(string.Format("Remove Node uri: {0}", _ipv4Uri));
                    }
                }
                _ipv4Uri = null;

                if (_ipv6Uri != null)
                {
                    if (this.RemoveUri(_ipv6Uri))
                    {
                        Log.Information(string.Format("Remove Node uri: {0}", _ipv6Uri));
                    }
                }
                _ipv6Uri = null;

                if (_upnpUri != null)
                {
                    if (this.RemoveUri(_upnpUri))
                    {
                        Log.Information(string.Format("Remove Node uri: {0}", _upnpUri));
                    }

                    try
                    {
                        using (UpnpClient client = new UpnpClient())
                        {
                            client.Connect(new TimeSpan(0, 0, 10));

                            var regex = new Regex(@"(.*?):(.*):(\d*)");
                            var match = regex.Match(_upnpUri);
                            if (!match.Success) throw new Exception();
                            int port = int.Parse(match.Groups[3].Value);

                            client.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 10));

                            Log.Information(string.Format("UPnP Close Port: {0}", port));
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                _upnpUri = null;
            }
        }

        private readonly object _stateLock = new object();

        public override void Start()
        {
            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    this.Update();
                }
            }
        }

        public override void Stop()
        {
            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;

                    this.Shutdown();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {

            }
        }
    }
}
