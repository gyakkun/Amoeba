using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Library;
using Library.Net.Amoeba;
using Library.Net.Upnp;

namespace Amoeba
{
    class AutoBaseNodeSettingManager : StateManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private AmoebaManager _amoebaManager;

        private Settings _settings;

        private ManagerState _state = ManagerState.Stop;

        private object _thisLock = new object();
        private bool _disposed = false;

        public AutoBaseNodeSettingManager(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            _settings = new Settings();
        }

        public override ManagerState State
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _state;
                }
            }
        }

        private static IEnumerable<IPAddress> GetIpAddresses()
        {
            List<IPAddress> list = new List<IPAddress>();
            list.AddRange(Dns.GetHostAddresses(Dns.GetHostName()));

            string query = "SELECT * FROM Win32_NetworkAdapterConfiguration";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                ManagementObjectCollection queryCollection = searcher.Get();

                foreach (ManagementObject mo in queryCollection)
                {
                    if ((bool)mo["IPEnabled"])
                    {
                        foreach (string ip in (string[])mo["IPAddress"])
                        {
                            if (ip != null) continue;

                            var tempIp = IPAddress.Parse(ip);

                            if (!list.Contains(tempIp))
                                list.Add(tempIp);
                        }
                    }
                }
            }

            return list;
        }

        private static int IpAddressCompare(IPAddress x, IPAddress y)
        {
            return Collection.Compare(x.GetAddressBytes(), y.GetAddressBytes());
        }

        public void Update()
        {
            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Stop) return;

                try
                {
                    string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));

                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(uri);
                    if (!match.Success) goto End;

                    int port = int.Parse(match.Groups[3].Value);

                    List<IPAddress> myIpAddresses = new List<IPAddress>(AutoBaseNodeSettingManager.GetIpAddresses());

                    foreach (var myIpAddress in myIpAddresses.Where(n => n.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                    {
                        if (IPAddress.Any.ToString() == myIpAddress.ToString()
                            || IPAddress.Loopback.ToString() == myIpAddress.ToString()
                            || IPAddress.Broadcast.ToString() == myIpAddress.ToString())
                        {
                            continue;
                        }
                        if (AutoBaseNodeSettingManager.IpAddressCompare(myIpAddress, IPAddress.Parse("10.0.0.0")) >= 0
                            && AutoBaseNodeSettingManager.IpAddressCompare(myIpAddress, IPAddress.Parse("10.255.255.255")) <= 0)
                        {
                            continue;
                        }
                        if (AutoBaseNodeSettingManager.IpAddressCompare(myIpAddress, IPAddress.Parse("172.16.0.0")) >= 0
                            && AutoBaseNodeSettingManager.IpAddressCompare(myIpAddress, IPAddress.Parse("172.31.255.255")) <= 0)
                        {
                            continue;
                        }
                        if (AutoBaseNodeSettingManager.IpAddressCompare(myIpAddress, IPAddress.Parse("127.0.0.0")) >= 0
                            && AutoBaseNodeSettingManager.IpAddressCompare(myIpAddress, IPAddress.Parse("127.255.255.255")) <= 0)
                        {
                            continue;
                        }
                        if (AutoBaseNodeSettingManager.IpAddressCompare(myIpAddress, IPAddress.Parse("192.168.0.0")) >= 0
                            && AutoBaseNodeSettingManager.IpAddressCompare(myIpAddress, IPAddress.Parse("192.168.255.255")) <= 0)
                        {
                            continue;
                        }

                        var ipv4Uri = string.Format("tcp:{0}:{1}", myIpAddress.ToString(), port);

                        if (_settings.Ipv4Uri != ipv4Uri)
                        {
                            if (_settings.Ipv4Uri != null)
                            {
                                _amoebaManager.BaseNode.Uris.Remove(_settings.Ipv4Uri);

                                Log.Information(string.Format("Remove Node Uri: {0}", _settings.Ipv4Uri));
                            }
                            _settings.Ipv4Uri = ipv4Uri;

                            if (!_amoebaManager.BaseNode.Uris.Any(n => n == _settings.Ipv4Uri))
                            {
                                _amoebaManager.BaseNode.Uris.Add(_settings.Ipv4Uri);

                                Log.Information(string.Format("Add Node Uri: {0}", _settings.Ipv4Uri));
                            }
                        }

                        break;
                    }

                End: ;
                }
                catch (Exception)
                {

                }

                try
                {
                    string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:[{0}]:", IPAddress.IPv6Any.ToString())));

                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(uri);
                    if (!match.Success) goto End;

                    int port = int.Parse(match.Groups[3].Value);

                    List<IPAddress> myIpAddresses = new List<IPAddress>(AutoBaseNodeSettingManager.GetIpAddresses());

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

                        var ipv6Uri = string.Format("tcp:[{0}]:{1}", myIpAddress.ToString(), port);

                        if (_settings.Ipv6Uri != ipv6Uri)
                        {
                            if (_settings.Ipv6Uri != null)
                            {
                                _amoebaManager.BaseNode.Uris.Remove(_settings.Ipv6Uri);

                                Log.Information(string.Format("Remove Node Uri: {0}", _settings.Ipv6Uri));
                            }
                            _settings.Ipv6Uri = ipv6Uri;

                            if (!_amoebaManager.BaseNode.Uris.Any(n => n == _settings.Ipv6Uri))
                            {
                                _amoebaManager.BaseNode.Uris.Add(_settings.Ipv6Uri);

                                Log.Information(string.Format("Add Node Uri: {0}", _settings.Ipv6Uri));
                            }
                        }

                        break;
                    }

                End: ;
                }
                catch (Exception)
                {

                }

                try
                {
                    string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));

                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(uri);
                    if (!match.Success) goto End;

                    using (UpnpClient upnpClient = new UpnpClient())
                    {
                        upnpClient.Connect(new TimeSpan(0, 0, 5));

                        int port = int.Parse(match.Groups[3].Value);

                        string ip = upnpClient.GetExternalIpAddress(new TimeSpan(0, 0, 5));
                        if (string.IsNullOrWhiteSpace(ip)) goto End;

                        var upnpUri = string.Format("tcp:{0}:{1}", ip, port);

                        if (_settings.UpnpUri != upnpUri)
                        {
                            if (_settings.UpnpUri != null)
                            {
                                _amoebaManager.BaseNode.Uris.Remove(_settings.UpnpUri);

                                Log.Information(string.Format("Remove Node Uri: {0}", _settings.UpnpUri));

                                try
                                {
                                    var match2 = regex.Match(_settings.UpnpUri);
                                    if (!match2.Success) goto End;
                                    int port2 = int.Parse(match2.Groups[3].Value);

                                    if (port != port2)
                                    {
                                        upnpClient.ClosePort(UpnpProtocolType.Tcp, port2, new TimeSpan(0, 0, 5));

                                        Log.Information(string.Format("UPnP Close Port: {0}", port2));

                                        upnpClient.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 5));
                                        if (!upnpClient.OpenPort(UpnpProtocolType.Tcp, port, port, "Amoeba", new TimeSpan(0, 0, 5))) goto End;

                                        Log.Information(string.Format("UPnP Open Port: {0}", port));
                                    }
                                }
                                catch (Exception)
                                {

                                }
                            }
                            else
                            {
                                upnpClient.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 5));
                                if (!upnpClient.OpenPort(UpnpProtocolType.Tcp, port, port, "Amoeba", new TimeSpan(0, 0, 5))) goto End;

                                Log.Information(string.Format("UPnP Open Port: {0}", port));
                            }

                            _settings.UpnpUri = upnpUri;

                            if (!_amoebaManager.BaseNode.Uris.Any(n => n == _settings.UpnpUri))
                            {
                                _amoebaManager.BaseNode.Uris.Add(_settings.UpnpUri);

                                Log.Information(string.Format("Add Node Uri: {0}", _settings.UpnpUri));
                            }
                        }
                    }

                End: ;
                }
                catch (Exception)
                {
                    if (_settings.UpnpUri != null)
                    {
                        _amoebaManager.BaseNode.Uris.Remove(_settings.UpnpUri);

                        Log.Information(string.Format("Remove Node Uri: {0}", _settings.UpnpUri));
                    }
                    _settings.UpnpUri = null;
                }
            }
        }

        public override void Start()
        {
            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                this.Update();
            }
        }

        public override void Stop()
        {
            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Stop) return;
                _state = ManagerState.Stop;

                if (_settings.Ipv4Uri != null)
                {
                    _amoebaManager.BaseNode.Uris.Remove(_settings.Ipv4Uri);

                    Log.Information(string.Format("Remove Node Uri: {0}", _settings.Ipv4Uri));
                }
                _settings.Ipv4Uri = null;

                if (_settings.Ipv6Uri != null)
                {
                    _amoebaManager.BaseNode.Uris.Remove(_settings.Ipv6Uri);

                    Log.Information(string.Format("Remove Node Uri: {0}", _settings.Ipv6Uri));
                }
                _settings.Ipv6Uri = null;

                if (_settings.UpnpUri != null)
                {
                    _amoebaManager.BaseNode.Uris.Remove(_settings.UpnpUri);

                    Log.Information(string.Format("Remove Node Uri: {0}", _settings.UpnpUri));

                    try
                    {
                        using (UpnpClient upnpClient = new UpnpClient())
                        {
                            upnpClient.Connect(new TimeSpan(0, 0, 5));

                            Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                            var match = regex.Match(_settings.UpnpUri);
                            if (!match.Success) goto End;

                            int port = int.Parse(match.Groups[3].Value);

                            upnpClient.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 5));

                            Log.Information(string.Format("UPnP Close Port: {0}", port));
                        }

                    End: ;
                    }
                    catch (Exception)
                    {

                    }
                }
                _settings.UpnpUri = null;
            }
        }

        #region ISettings

        public void Load(string directoryPath)
        {
            lock (this.ThisLock)
            {
                _settings.Load(directoryPath);
            }
        }

        public void Save(string directoryPath)
        {
            lock (this.ThisLock)
            {
                _settings.Save(directoryPath);
            }
        }

        #endregion

        private class Settings : Library.Configuration.SettingsBase, IThisLock
        {
            private object _thisLock = new object();

            public Settings()
                : base(new List<Library.Configuration.ISettingsContext>() { 
                    new Library.Configuration.SettingsContext<string>() { Name = "Ipv4Uri", Value = null },
                    new Library.Configuration.SettingsContext<string>() { Name = "Ipv6Uri", Value = null },
                    new Library.Configuration.SettingsContext<string>() { Name = "UpnpUri", Value = null },
                })
            {

            }

            public override void Load(string directoryPath)
            {
                lock (this.ThisLock)
                {
                    base.Load(directoryPath);
                }
            }

            public override void Save(string directoryPath)
            {
                lock (this.ThisLock)
                {
                    base.Save(directoryPath);
                }
            }

            public string Ipv4Uri
            {
                get
                {
                    lock (this.ThisLock)
                    {
                        return (string)this["Ipv4Uri"];
                    }
                }
                set
                {
                    lock (this.ThisLock)
                    {
                        this["Ipv4Uri"] = value;
                    }
                }
            }

            public string Ipv6Uri
            {
                get
                {
                    lock (this.ThisLock)
                    {
                        return (string)this["Ipv6Uri"];
                    }
                }
                set
                {
                    lock (this.ThisLock)
                    {
                        this["Ipv6Uri"] = value;
                    }
                }
            }

            public string UpnpUri
            {
                get
                {
                    lock (this.ThisLock)
                    {
                        return (string)this["UpnpUri"];
                    }
                }
                set
                {
                    lock (this.ThisLock)
                    {
                        this["UpnpUri"] = value;
                    }
                }
            }

            #region IThisLock

            public object ThisLock
            {
                get
                {
                    return _thisLock;
                }
            }

            #endregion
        }

        protected override void Dispose(bool disposing)
        {
            lock (this.ThisLock)
            {
                if (_disposed) return;

                if (disposing)
                {
                    this.Stop();
                }

                _disposed = true;
            }
        }

        #region IThisLock

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion
    }
}
