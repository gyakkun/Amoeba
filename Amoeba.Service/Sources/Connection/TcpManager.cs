using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;

namespace Amoeba.Service
{
    class TcpManager : StateManagerBase, Library.Configuration.ISettings
    {
        private AmoebaManager _amoebaManager;

        private Settings _settings;

        private volatile ManagerState _state = ManagerState.Stop;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public TcpManager(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            _settings = new Settings();
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
                        string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));

                        var regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(uri);
                        if (!match.Success) throw new Exception();

                        int port = int.Parse(match.Groups[3].Value);

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
                    }
                    catch (Exception)
                    {

                    }

                    if (ipv4Uri != _settings.Ipv4Uri)
                    {
                        if (this.RemoveUri(_settings.Ipv4Uri))
                            Log.Information(string.Format("Remove Node uri: {0}", _settings.Ipv4Uri));
                    }

                    _settings.Ipv4Uri = ipv4Uri;

                    if (_settings.Ipv4Uri != null)
                    {
                        if (this.AddUri(_settings.Ipv4Uri))
                            Log.Information(string.Format("Add Node uri: {0}", _settings.Ipv4Uri));
                    }
                }

                {
                    string ipv6Uri = null;

                    try
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
                    catch (Exception)
                    {

                    }

                    if (ipv6Uri != _settings.Ipv6Uri)
                    {
                        if (this.RemoveUri(_settings.Ipv6Uri))
                            Log.Information(string.Format("Remove Node uri: {0}", _settings.Ipv6Uri));
                    }

                    _settings.Ipv6Uri = ipv6Uri;

                    if (_settings.Ipv6Uri != null)
                    {
                        if (this.AddUri(_settings.Ipv6Uri))
                            Log.Information(string.Format("Add Node uri: {0}", _settings.Ipv6Uri));
                    }
                }

                {
                    string upnpUri = null;

                    try
                    {
                        string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));

                        var regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(uri);
                        if (!match.Success) throw new Exception();

                        int port = int.Parse(match.Groups[3].Value);

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

                    if (upnpUri != _settings.UpnpUri)
                    {
                        if (this.RemoveUri(_settings.UpnpUri))
                            Log.Information(string.Format("Remove Node uri: {0}", _settings.UpnpUri));
                    }

                    _settings.UpnpUri = upnpUri;

                    if (_settings.UpnpUri != null)
                    {
                        if (this.AddUri(_settings.UpnpUri))
                            Log.Information(string.Format("Add Node uri: {0}", _settings.UpnpUri));
                    }
                }
            }
        }

        private void Shutdown()
        {
            lock (_thisLock)
            {
                if (_settings.Ipv4Uri != null)
                {
                    if (this.RemoveUri(_settings.Ipv4Uri))
                        Log.Information(string.Format("Remove Node uri: {0}", _settings.Ipv4Uri));
                }
                _settings.Ipv4Uri = null;

                if (_settings.Ipv6Uri != null)
                {
                    if (this.RemoveUri(_settings.Ipv6Uri))
                        Log.Information(string.Format("Remove Node uri: {0}", _settings.Ipv6Uri));
                }
                _settings.Ipv6Uri = null;

                if (_settings.UpnpUri != null)
                {
                    if (this.RemoveUri(_settings.UpnpUri))
                        Log.Information(string.Format("Remove Node uri: {0}", _settings.UpnpUri));

                    try
                    {
                        using (UpnpClient client = new UpnpClient())
                        {
                            client.Connect(new TimeSpan(0, 0, 10));

                            var regex = new Regex(@"(.*?):(.*):(\d*)");
                            var match = regex.Match(_settings.UpnpUri);
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
                _settings.UpnpUri = null;
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

        #region ISettings

        public void Load(string directoryPath)
        {
            lock (_thisLock)
            {
                _settings.Load(directoryPath);

                this.Shutdown();
            }
        }

        public void Save(string directoryPath)
        {
            lock (_thisLock)
            {
                _settings.Save(directoryPath);
            }
        }

        #endregion

        private class Settings : Library.Configuration.SettingsBase
        {
            public Settings()
                : base(new List<Library.Configuration.ISettingContent>() {
                    new Library.Configuration.SettingContent<string>() { Name = "Ipv4Uri", Value = null },
                    new Library.Configuration.SettingContent<string>() { Name = "Ipv6Uri", Value = null },
                    new Library.Configuration.SettingContent<string>() { Name = "UpnpUri", Value = null },
                })
            {

            }

            public string Ipv4Uri
            {
                get
                {
                    return (string)this["Ipv4Uri"];
                }
                set
                {
                    this["Ipv4Uri"] = value;
                }
            }

            public string Ipv6Uri
            {
                get
                {
                    return (string)this["Ipv6Uri"];
                }
                set
                {
                    this["Ipv6Uri"] = value;
                }
            }

            public string UpnpUri
            {
                get
                {
                    return (string)this["UpnpUri"];
                }
                set
                {
                    this["UpnpUri"] = value;
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
