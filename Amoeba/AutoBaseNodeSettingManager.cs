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

        public override void Start()
        {
            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                try
                {
                    string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));
                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(uri);
                    if (!match.Success) return;
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

                        _settings.Ipv4Uri = string.Format("tcp:{0}:{1}", myIpAddress.ToString(), port);

                        if (!_amoebaManager.BaseNode.Uris.Any(n => n == _settings.Ipv4Uri))
                            _amoebaManager.BaseNode.Uris.Add(_settings.Ipv4Uri);

                        break;
                    }
                }
                catch (Exception)
                {

                }

                try
                {
                    string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:[{0}]:", IPAddress.IPv6Any.ToString())));
                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(uri);
                    if (!match.Success) return;
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

                        _settings.Ipv6Uri = string.Format("tcp:[{0}]:{1}", myIpAddress.ToString(), port);

                        if (!_amoebaManager.BaseNode.Uris.Any(n => n == _settings.Ipv6Uri))
                            _amoebaManager.BaseNode.Uris.Add(_settings.Ipv6Uri);

                        break;
                    }
                }
                catch (Exception)
                {

                }

                try
                {
                    string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));
                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(uri);
                    if (!match.Success) return;
                    int port = int.Parse(match.Groups[3].Value);

                    using (UpnpClient upnpClient = new UpnpClient())
                    {
                        upnpClient.Connect(new TimeSpan(0, 0, 10));

                        string ip = upnpClient.GetExternalIpAddress(new TimeSpan(0, 0, 10));

                        if (!string.IsNullOrWhiteSpace(ip))
                        {
                            upnpClient.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 10));
                            
                            if (upnpClient.OpenPort(UpnpProtocolType.Tcp, port, port, "Amoeba", new TimeSpan(0, 0, 10)))
                            {
                                _settings.UpnpUri = string.Format("tcp:{0}:{1}", ip, port);

                                if (!_amoebaManager.BaseNode.Uris.Any(n => n == _settings.UpnpUri))
                                    _amoebaManager.BaseNode.Uris.Add(_settings.UpnpUri);
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public override void Stop()
        {
            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Stop) return;
                _state = ManagerState.Stop;

                if (_settings.Ipv4Uri != null) _amoebaManager.BaseNode.Uris.Remove(_settings.Ipv4Uri);
                _settings.Ipv4Uri = null;

                if (_settings.Ipv6Uri != null) _amoebaManager.BaseNode.Uris.Remove(_settings.Ipv6Uri);
                _settings.Ipv6Uri = null;

                try
                {
                    if (_settings.UpnpUri != null)
                    {
                        using (UpnpClient client = new UpnpClient())
                        {
                            client.Connect(new TimeSpan(0, 0, 10));

                            Regex regex = new Regex(@"(.*?)\:(.*)\:(\d*)");
                            var match = regex.Match(_settings.UpnpUri);
                            if (!match.Success) return;
                            int port = int.Parse(match.Groups[2].Value);

                            client.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 10));
                        }
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    if (_settings.UpnpUri != null) _amoebaManager.BaseNode.Uris.Remove(_settings.UpnpUri);
                    _settings.UpnpUri = null;
                }
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
