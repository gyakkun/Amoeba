using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Library;
using Library.Net.Amoeba;
using Library.Net.Proxy;

namespace Amoeba
{
    class Ipv6Manager : StateManagerBase, IThisLock
    {
        private AmoebaManager _amoebaManager;

        private string _ipv6Uri = null;
        private ConnectionFilter _ipv6ConnectionFilter = null;

        private ManagerState _state = ManagerState.Stop;
        private object _thisLock = new object();
        private bool _disposed = false;

        public Ipv6Manager(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;
        }

        public override ManagerState State
        {
            get
            {
                using (DeadlockMonitor.Lock(this.ThisLock))
                {
                    return _state;
                }
            }
        }

        private static int GetPortv6()
        {
            Random random = new Random();

            for (; ; )
            {
                Socket socket = null;

                try
                {
                    int port = random.Next(1024, 65536);

                    socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));

                    return port;
                }
                catch (Exception)
                {

                }
                finally
                {
                    if (socket != null) socket.Close();
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
            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                try
                {
                    if (!_amoebaManager.ListenUris.Any(n => n.StartsWith(string.Format("tcp:[{0}]:", IPAddress.IPv6Any.ToString()))))
                    {
                        int tempPort = Ipv6Manager.GetPortv6();
                        _amoebaManager.ListenUris.Add(string.Format("tcp:[{0}]:{1}", IPAddress.IPv6Any.ToString(), tempPort));
                    }

                    string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:[{0}]:", IPAddress.IPv6Any.ToString())));
                    Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                    var match = regex.Match(uri);
                    if (!match.Success) return;
                    int port = int.Parse(match.Groups[3].Value);

                    List<IPAddress> myIpAddresses = new List<IPAddress>(Ipv6Manager.GetIpAddresses());

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

                        _ipv6Uri = string.Format("tcp:[{0}]:{1}", myIpAddress.ToString(), port);

                        if (!_amoebaManager.BaseNode.Uris.Any(n => n == _ipv6Uri))
                            _amoebaManager.BaseNode.Uris.Add(_ipv6Uri);

                        break;
                    }

                    _ipv6ConnectionFilter = new ConnectionFilter()
                    {
                        ConnectionType = ConnectionType.Tcp,
                        UriCondition = new UriCondition()
                        {
                            Value = @"tcp:\[(\d|:)*\].*",
                        },
                    };

                    if (!_amoebaManager.Filters.Any(n => n == _ipv6ConnectionFilter))
                        _amoebaManager.Filters.Add(_ipv6ConnectionFilter);
                }
                catch (Exception)
                {

                }
            }
        }

        public override void Stop()
        {
            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                if (this.State == ManagerState.Stop) return;
                _state = ManagerState.Stop;

                if (_ipv6Uri != null) _amoebaManager.BaseNode.Uris.Remove(_ipv6Uri);
                if (_ipv6ConnectionFilter != null) _amoebaManager.Filters.Remove(_ipv6ConnectionFilter);

                _ipv6Uri = null;
                _ipv6ConnectionFilter = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                }

                _disposed = true;
            }
        }

        #region IThisLock メンバ

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
