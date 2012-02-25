using System;
using System.Collections.Generic;
using System.Linq;
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
    class UpnpManager : StateManagerBase, IThisLock
    {
        private AmoebaManager _amoebaManager;
       
        private string _upnpUri = null;
        private ConnectionFilter _upnpConnectionFilter = null;

        private ManagerState _state = ManagerState.Stop;
        private object _thisLock = new object();
        private bool _disposed = false;

        public UpnpManager(AmoebaManager amoebaManager)
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

        private static int GetPortv4()
        {
            Random random = new Random();

            for (; ; )
            {
                Socket socket = null;

                try
                {
                    int port = random.Next(1024, 65536);

                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Bind(new IPEndPoint(IPAddress.Any, port));

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

        public override void Start()
        {
            using (DeadlockMonitor.Lock(this.ThisLock))
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                try
                {
                    using (UpnpClient upnpClient = new UpnpClient())
                    {
                        upnpClient.Connect(new TimeSpan(0, 0, 10));

                        if (!_amoebaManager.ListenUris.Any(n => n.StartsWith(string.Format("tcp:{0}", IPAddress.Any.ToString()))))
                        {
                            int tempPort = UpnpManager.GetPortv4();
                            _amoebaManager.ListenUris.Add(string.Format("tcp:{0}:{1}", IPAddress.Any.ToString(), tempPort));
                        }

                        string uri = _amoebaManager.ListenUris.FirstOrDefault(n => n.StartsWith(string.Format("tcp:{0}:", IPAddress.Any.ToString())));
                        Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(uri);
                        if (!match.Success) return;
                        int port = int.Parse(match.Groups[3].Value);

                        string ip = upnpClient.GetExternalIpAddress(new TimeSpan(0, 0, 20));

                        upnpClient.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 20));
                        upnpClient.OpenPort(UpnpProtocolType.Tcp, port, port, "Amoeba", new TimeSpan(0, 0, 20));

                        _upnpUri = string.Format("tcp:{0}:{1}", ip, port);

                        if (!_amoebaManager.BaseNode.Uris.Any(n => n == _upnpUri))
                            _amoebaManager.BaseNode.Uris.Add(_upnpUri);

                        _upnpConnectionFilter = new ConnectionFilter()
                        {
                            ConnectionType = ConnectionType.Tcp,
                            UriCondition = new UriCondition()
                            {
                                Value = @"tcp:([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3}).*",
                            },
                        };

                        if (!_amoebaManager.Filters.Any(n => n == _upnpConnectionFilter))
                            _amoebaManager.Filters.Add(_upnpConnectionFilter);
                    }
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

                if (_upnpUri != null) _amoebaManager.BaseNode.Uris.Remove(_upnpUri);
                if (_upnpConnectionFilter != null) _amoebaManager.Filters.Remove(_upnpConnectionFilter);

                _upnpUri = null;
                _upnpConnectionFilter = null;

                try
                {
                    using (UpnpClient client = new UpnpClient())
                    {
                        client.Connect(new TimeSpan(0, 0, 10));

                        Regex regex = new Regex(@"(.*?)\:(.*)\:(\d*)");
                        var match = regex.Match(_upnpUri);
                        if (!match.Success) return;
                        int port = int.Parse(match.Groups[2].Value);

                        client.ClosePort(UpnpProtocolType.Tcp, port, new TimeSpan(0, 0, 20));
                    }
                }
                catch (Exception)
                {

                }
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
