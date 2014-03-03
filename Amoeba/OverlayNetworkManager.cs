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
using Library;
using Library.Net.I2p;
using Library.Net.Amoeba;
using Library.Net.Caps;
using Library.Net.Connections;

namespace Amoeba
{
    class OverlayNetworkManager : StateManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private AmoebaManager _amoebaManager;
        private BufferManager _bufferManager;

        private Settings _settings;

        private SamV3Session _samSession;
        private object _samClientLock = new object();

        private SamListener _samListener;
        private string _samHistory;
        private object _samServerLock = new object();

        private Regex _regex = new Regex(@"(.*?):(.*):(\d*)");

        private volatile Thread _watchThread;

        private ManagerState _state = ManagerState.Stop;

        private volatile bool _disposed;
        private readonly object _thisLock = new object();

        private const int _maxReceiveCount = 1024 * 1024 * 32;

        public OverlayNetworkManager(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _settings = new Settings(this.ThisLock);

            _amoebaManager.CreateCapEvent = this.CreateCap;
            _amoebaManager.AcceptCapEvent = this.AcceptCap;
        }

        private CapBase CreateCap(object sender, string uri)
        {
            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            if (!uri.StartsWith("i2p:")) return null;

            List<IDisposable> garbages = new List<IDisposable>();

            try
            {
                string scheme = null;
                string host = null;

                {
                    Regex regex = new Regex(@"(.*?):(.*)");
                    var match = regex.Match(uri);

                    if (match.Success)
                    {
                        scheme = match.Groups[1].Value;
                        host = match.Groups[2].Value;
                    }
                }

                if (host == null) return null;

                {
                    string proxyScheme = null;
                    string proxyHost = null;
                    int proxyPort = -1;

                    {
                        Regex regex = new Regex(@"(.*?):(.*):(\d*)");
                        var match = regex.Match(this.SamBridgeUri);

                        if (match.Success)
                        {
                            proxyScheme = match.Groups[1].Value;
                            proxyHost = match.Groups[2].Value;
                            proxyPort = int.Parse(match.Groups[3].Value);
                        }
                    }

                    if (proxyHost == null) return null;

                    if (scheme == "i2p")
                    {
                        SamV3Connector connector = null;

                        try
                        {
                            SamV3Session session;

                            lock (_samClientLock)
                            {
                                session = _samSession;

                                if (session == null)
                                {
                                    Socket proxySocket = null;

                                    try
                                    {
                                        proxySocket = OverlayNetworkManager.Connect(new IPEndPoint(OverlayNetworkManager.GetIpAddress(proxyHost), proxyPort), new TimeSpan(0, 0, 10));

                                        string caption = "Amoeba_Client";
                                        string[] options = new string[]
                                        {
                                            "inbound.nickname=" + caption,
                                            "outbound.nickname=" + caption
                                        };
                                        string optionsString = string.Join(" ", options);

                                        session = new SamV3Session(proxyHost, proxyPort, null, proxySocket);

                                        session.Handshake();
                                        session.Create(null, optionsString);

                                        _samSession = session;
                                    }
                                    catch (Exception)
                                    {
                                        if (session != null) session.Dispose();
                                        if (proxySocket != null) proxySocket.Dispose();

                                        throw;
                                    }
                                }
                            }

                            connector = new SamV3Connector(session);
                            connector.Handshake();
                            connector.Connect(host);
                        }
                        catch (SamBridgeErrorException ex)
                        {
                            bool isLog = true;
                            if (connector != null) connector.Dispose();

                            string result = ex.Reply.UniqueValue("RESULT", string.Empty).Value;

                            if (result == SamBridgeErrorMessage.INVALID_ID)
                            {
                                lock (_samClientLock)
                                {
                                    if (_samSession != null)
                                    {
                                        _samSession.Dispose();
                                        _samSession = null;
                                    }
                                }

                                isLog = false;
                            }
                            else if (result == SamBridgeErrorMessage.CANT_REACH_PEER
                                || result == SamBridgeErrorMessage.PEER_NOT_FOUND)
                            {
                                isLog = false;
                            }

                            if (isLog) Debug.WriteLine(ex);

                            throw;
                        }
                        catch (SamException ex)
                        {
                            Debug.WriteLine(ex);
                            if (connector != null) connector.Dispose();

                            throw;
                        }

                        return new SocketCap(connector.BridgeSocket);
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

        private CapBase AcceptCap(object sender, out string uri)
        {
            uri = null;

            if (_disposed) return null;
            if (this.State == ManagerState.Stop) return null;

            lock (_samServerLock)
            {
                if (_samListener == null) return null;

                try
                {
                    _samListener.Update();
                }
                catch (SamException)
                {
                    return null;
                }

                if (_samListener.Pending())
                {
                    SamV3StatefulAcceptor acceptor = _samListener.Dequeue();

                    try
                    {
                        acceptor.AcceptComplete();
                    }
                    catch (SamException ex)
                    {
                        Debug.WriteLine(ex);
                        acceptor.Dispose();

                        return null;
                    }

                    Socket socket = acceptor.BridgeSocket;
                    string base64Address = acceptor.DestinationBase64;
                    string base32Address = I2PEncoding.Base32Address.FromDestinationBase64(base64Address);
                    uri = "i2p:" + base32Address;

                    return new SocketCap(socket);
                }
            }

            return null;
        }

        public string SamBridgeUri
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _settings.SamBridgeUri;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _settings.SamBridgeUri = value;
                }
            }
        }

        private static IPAddress GetIpAddress(string host)
        {
            IPAddress remoteIP = null;

            if (!IPAddress.TryParse(host, out remoteIP))
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(host);

                if (hostEntry.AddressList.Length > 0)
                {
                    remoteIP = hostEntry.AddressList[0];
                }
                else
                {
                    return null;
                }
            }

            return remoteIP;
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

                if (!asyncResult.IsCompleted && !asyncResult.CompletedSynchronously)
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

            throw new OverlayNetworkManagerException();
        }

        private bool AddUri(string uri)
        {
            lock (this.ThisLock)
            {
                lock (_amoebaManager.ThisLock)
                {
                    var baseNode = _amoebaManager.BaseNode;
                    var random = RandomNumberGenerator.Create();

                    byte[] id = new byte[64];
                    random.GetBytes(id);

                    var uris = new List<string>(baseNode.Uris);
                    if (uris.Contains(uri)) return false;

                    uris.Add(uri);

                    _amoebaManager.SetBaseNode(new Node(id, uris));
                }
            }

            return true;
        }

        private bool RemoveUri(string uri)
        {
            lock (this.ThisLock)
            {
                lock (_amoebaManager.ThisLock)
                {
                    var baseNode = _amoebaManager.BaseNode;
                    var random = RandomNumberGenerator.Create();

                    byte[] id = new byte[64];
                    random.GetBytes(id);

                    var uris = new List<string>(baseNode.Uris);
                    if (!uris.Remove(uri)) return false;

                    _amoebaManager.SetBaseNode(new Node(id, uris));
                }
            }

            return true;
        }

        private void WatchThread()
        {
            Stopwatch checkSamStopwatch = new Stopwatch();
            checkSamStopwatch.Start();

            for (; ; )
            {
                Thread.Sleep(1000);
                if (this.State == ManagerState.Stop) return;

                if (!checkSamStopwatch.IsRunning || checkSamStopwatch.Elapsed.TotalSeconds >= 30)
                {
                    checkSamStopwatch.Restart();

                    if (_samHistory != this.SamBridgeUri)
                    {
                        string i2pUri = null;

                        lock (_samServerLock)
                        {
                            if (_samListener != null)
                            {
                                _samListener.Dispose();
                            }

                            try
                            {
                                var match = _regex.Match(this.SamBridgeUri);
                                if (!match.Success) throw new Exception();

                                if (match.Groups[1].Value == "tcp")
                                {
                                    SamListener listener = null;

                                    try
                                    {
                                        string caption = "Amoeba_Server";
                                        string[] options = new string[]
                                        {
                                            "inbound.nickname=" + caption,
                                            "outbound.nickname=" + caption
                                        };

                                        string optionsString = string.Join(" ", options);

                                        listener = new SamListener(match.Groups[2].Value, int.Parse(match.Groups[3].Value), optionsString);

                                        string base64Address = listener.Session.DestinationBase64;
                                        string base32Address = I2PEncoding.Base32Address.FromDestinationBase64(base64Address);

                                        Debug.WriteLine("New I2P BaseNode generated." + "\n" +
                                            "i2p:" + base64Address + "\n" +
                                            "i2p:" + base32Address);

                                        i2pUri = string.Format("i2p:{0}", base32Address);

                                        _samListener = listener;
                                    }
                                    catch (SamException ex)
                                    {
                                        Debug.WriteLine(ex);

                                        if (listener != null) listener.Dispose();
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }

                        lock (this.ThisLock)
                        {
                            if (i2pUri != _settings.I2pUri)
                            {
                                if (this.RemoveUri(_settings.I2pUri))
                                    Log.Information(string.Format("Remove Node Uri: {0}", _settings.I2pUri));
                            }

                            _settings.I2pUri = i2pUri;

                            if (_settings.I2pUri != null)
                            {
                                if (this.AddUri(_settings.I2pUri))
                                    Log.Information(string.Format("Add Node Uri: {0}", _settings.I2pUri));
                            }

                            _samHistory = this.SamBridgeUri;
                        }
                    }
                }
            }
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

        private readonly object _stateLock = new object();

        public override void Start()
        {
            lock (_stateLock)
            {
                lock (this.ThisLock)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _watchThread = new Thread(this.WatchThread);
                    _watchThread.Priority = ThreadPriority.Lowest;
                    _watchThread.Name = "OverlayNetworkManager_WatchThread";
                    _watchThread.Start();
                }
            }
        }

        public override void Stop()
        {
            lock (_stateLock)
            {
                lock (this.ThisLock)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                _watchThread.Join();
                _watchThread = null;

                lock (_samClientLock)
                {
                    if (_samSession != null)
                        _samSession.Dispose();

                    _samSession = null;
                }

                lock (_samServerLock)
                {
                    if (_samListener != null)
                        _samListener.Dispose();

                    _samListener = null;

                    _samHistory = null;
                }

                lock (this.ThisLock)
                {
                    if (_settings.I2pUri != null)
                    {
                        if (this.RemoveUri(_settings.I2pUri))
                            Log.Information(string.Format("Remove Node Uri: {0}", _settings.I2pUri));
                    }
                    _settings.I2pUri = null;
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

        private class Settings : Library.Configuration.SettingsBase
        {
            private object _thisLock;

            public Settings(object lockObject)
                : base(new List<Library.Configuration.ISettingContent>() { 
                    new Library.Configuration.SettingContent<string>() { Name = "SamBridgeUri", Value = "tcp:127.0.0.1:7656" },
                    new Library.Configuration.SettingContent<string>() { Name = "I2pUri", Value = "" },
                })
            {
                _thisLock = lockObject;
            }

            public override void Load(string directoryPath)
            {
                lock (_thisLock)
                {
                    base.Load(directoryPath);
                }
            }

            public override void Save(string directoryPath)
            {
                lock (_thisLock)
                {
                    base.Save(directoryPath);
                }
            }

            public string SamBridgeUri
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (string)this["SamBridgeUri"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["SamBridgeUri"] = value;
                    }
                }
            }

            public string I2pUri
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (string)this["I2pUri"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["I2pUri"] = value;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_samSession != null) _samSession.Dispose();
                if (_samListener != null) _samListener.Dispose();
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

    [Serializable]
    class OverlayNetworkManagerException : ManagerException
    {
        public OverlayNetworkManagerException() : base() { }
        public OverlayNetworkManagerException(string message) : base(message) { }
        public OverlayNetworkManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
