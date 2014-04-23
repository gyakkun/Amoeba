using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Library;
using Library.Io;
using Library.Net;
using Library.Net.Amoeba;
using Library.Net.Connections;
using Library.Net.I2p;

namespace Amoeba
{
    unsafe class CatharsisManager : ManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private AmoebaManager _amoebaManager;
        private BufferManager _bufferManager;

        private Settings _settings;

        private Regex _regex = new Regex(@"(.*?):(.*):(\d*)", RegexOptions.Compiled);
        private Regex _regex2 = new Regex(@"(.*?):(.*)", RegexOptions.Compiled);

        private System.Threading.Timer _watchTimer;

        private volatile bool _disposed;
        private readonly object _thisLock = new object();

        private const int _maxReceiveCount = 1024 * 1024 * 32;

        public CatharsisManager(AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _settings = new Settings(this.ThisLock);

#if DEBUG
            _watchTimer = new System.Threading.Timer(this.WatchTimer, null, new TimeSpan(0, 0, 0), new TimeSpan(1, 0, 0, 0));
#else
            _watchTimer = new System.Threading.Timer(this.WatchTimer, null, new TimeSpan(0, 3, 0), new TimeSpan(7, 0, 0, 0));
#endif

            _amoebaManager.CheckUriEvent = this.CheckUri;
        }

        private bool CheckUri(object sender, string uri)
        {
            string host = null;

            {
                var match = _regex.Match(uri);

                if (match.Success)
                {
                    host = match.Groups[2].Value;
                }
                else
                {
                    var match2 = _regex2.Match(uri);

                    if (match2.Success)
                    {
                        host = match2.Groups[2].Value;
                    }
                }
            }

            IPAddress ip;

            if (IPAddress.TryParse(host, out ip))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    uint uip = NetworkConverter.ToUInt32(ip.GetAddressBytes());

                    lock (this.ThisLock)
                    {
                        if (_settings.Ipv4AddressSet.Contains(uip)) return false;

                        foreach (var range in _settings.Ipv4AddressRangeSet)
                        {
                            if (range.Verify(uip)) return false;
                        }
                    }
                }
            }

            return true;
        }

        private void WatchTimer(object state)
        {
            var ipv4AddressSet = new HashSet<uint>();
            var ipv4AddressRangeSet = new HashSet<SearchRange<uint>>();

            foreach (var addressFilter in App.AddressFilters)
            {
                string proxyScheme = null;
                string proxyHost = null;
                int proxyPort = -1;

                {
                    var match = _regex.Match(addressFilter.ProxyUri);

                    if (match.Success)
                    {
                        proxyScheme = match.Groups[1].Value;
                        proxyHost = match.Groups[2].Value;
                        proxyPort = int.Parse(match.Groups[3].Value);
                    }
                    else
                    {
                        var match2 = _regex2.Match(addressFilter.ProxyUri);

                        if (match2.Success)
                        {
                            proxyScheme = match2.Groups[1].Value;
                            proxyHost = match2.Groups[2].Value;
                            proxyPort = 80;
                        }
                    }
                }

                if (proxyHost == null) continue;

                WebProxy proxy = new WebProxy(proxyHost, proxyPort);

                foreach (var url in addressFilter.Urls)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            using (var stream = CatharsisManager.GetStream(url, proxy))
                            using (var gzipStream = new Ionic.Zlib.GZipStream(stream, Ionic.Zlib.CompressionMode.Decompress))
                            using (var reader = new StreamReader(gzipStream))
                            {
                                string line;

                                while ((line = reader.ReadLine()) != null)
                                {
                                    var index = line.LastIndexOf(':');
                                    if (index == -1) continue;

                                    var ips = CatharsisManager.GetStringToIpv4(line.Substring(index + 1));
                                    if (ips == null) continue;

                                    if (ips[0] == ips[1])
                                    {
                                        ipv4AddressSet.Add(ips[0]);
                                    }
                                    else if (ips[0] < ips[1])
                                    {
                                        var range = new SearchRange<uint>(ips[0], ips[1]);
                                        ipv4AddressRangeSet.Add(range);
                                    }
                                    else
                                    {
                                        var range = new SearchRange<uint>(ips[1], ips[0]);
                                        ipv4AddressRangeSet.Add(range);
                                    }
                                }
                            }

                            break;
                        }
                        catch (Exception e)
                        {
                            Log.Warning(e);
                        }
                    }
                }
            }

            lock (this.ThisLock)
            {
                _settings.Ipv4AddressSet.Clear();
                _settings.Ipv4AddressSet.UnionWith(ipv4AddressSet);

                _settings.Ipv4AddressRangeSet.Clear();
                _settings.Ipv4AddressRangeSet.UnionWith(ipv4AddressRangeSet);
            }
        }

        private static Stream GetStream(string url, IWebProxy proxy)
        {
            BufferManager bufferManager = BufferManager.Instance;

            for (int i = 0; i < 10; i++)
            {
                BufferStream bufferStream = new BufferStream(bufferManager);

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.AllowAutoRedirect = true;
                    request.Proxy = proxy;
                    request.Headers.Add("Pragma", "no-cache");
                    request.Headers.Add("Cache-Control", "no-cache");
                    request.Timeout = 1000 * 60 * 30;
                    request.ReadWriteTimeout = 1000 * 60 * 30;

                    using (WebResponse response = request.GetResponse())
                    {
                        if (response.ContentLength > 1024 * 1024 * 32) throw new Exception("too large");

                        byte[] buffer = null;

                        try
                        {
                            buffer = bufferManager.TakeBuffer(1024 * 32);

                            using (Stream stream = response.GetResponseStream())
                            {
                                int length;

                                while ((length = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    bufferStream.Write(buffer, 0, length);

                                    if (bufferStream.Length > 1024 * 1024 * 32) throw new Exception("too large");
                                }
                            }
                        }
                        finally
                        {
                            if (buffer != null)
                            {
                                bufferManager.ReturnBuffer(buffer);
                            }
                        }

                        if (response.ContentLength != -1 && bufferStream.Length != response.ContentLength)
                        {
                            continue;
                        }

                        bufferStream.Seek(0, SeekOrigin.Begin);
                        return bufferStream;
                    }
                }
                catch (Exception)
                {
                    bufferStream.Dispose();
                }
            }

            throw new Exception(string.Format("not found: {0}", url));
        }

        private static uint[] GetStringToIpv4(string value)
        {
            var list = value.Split('.', '-');
            if (list.Length != 8) return null;

            uint[] ip = new uint[2];

            fixed (uint* p = ip)
            {
                byte* bp = (byte*)p;

                if (BitConverter.IsLittleEndian)
                {
                    *bp++ = byte.Parse(list[3]);
                    *bp++ = byte.Parse(list[2]);
                    *bp++ = byte.Parse(list[1]);
                    *bp++ = byte.Parse(list[0]);
                    *bp++ = byte.Parse(list[7]);
                    *bp++ = byte.Parse(list[6]);
                    *bp++ = byte.Parse(list[5]);
                    *bp = byte.Parse(list[4]);
                }
                else
                {
                    *bp++ = byte.Parse(list[0]);
                    *bp++ = byte.Parse(list[1]);
                    *bp++ = byte.Parse(list[2]);
                    *bp++ = byte.Parse(list[3]);
                    *bp++ = byte.Parse(list[4]);
                    *bp++ = byte.Parse(list[5]);
                    *bp++ = byte.Parse(list[6]);
                    *bp = byte.Parse(list[7]);
                }
            }

            return ip;
        }

        // http://blogs.msdn.com/b/feroze_daud/archive/2004/03/30/104440.aspx
        private static string Decode(WebResponse w)
        {
            BufferManager bufferManager = BufferManager.Instance;

            //
            // first see if content length header has charset = calue
            //
            string charset = null;

            {
                string content_type = w.Headers["content-type"];

                if (content_type != null)
                {
                    int index = content_type.IndexOf("charset=");

                    if (index != -1)
                    {
                        charset = content_type.Substring(index + 8);
                    }
                }
            }

            // save data to a rawStream
            BufferStream rawStream = new BufferStream(bufferManager);

            {
                byte[] buffer = null;

                try
                {
                    buffer = bufferManager.TakeBuffer(1024 * 32);

                    using (Stream stream = w.GetResponseStream())
                    {
                        int length;

                        while ((length = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            rawStream.Write(buffer, 0, length);

                            if (rawStream.Length > 1024 * 1024 * 32) throw new Exception("too large");
                        }
                    }
                }
                finally
                {
                    if (buffer != null)
                    {
                        bufferManager.ReturnBuffer(buffer);
                    }
                }
            }

            //
            // if ContentType is null, or did not contain charset, we search in body
            //
            if (charset == null)
            {
                rawStream.Seek(0, SeekOrigin.Begin);

                using (StreamReader reader = new StreamReader(new WrapperStream(rawStream, true), Encoding.ASCII))
                {
                    string meta = reader.ReadToEnd();

                    if (!string.IsNullOrWhiteSpace(meta))
                    {
                        int start_index = meta.IndexOf("charset=");

                        if (start_index != -1)
                        {
                            int end_index = meta.IndexOf("\"", start_index);

                            if (end_index != -1)
                            {
                                start_index += 8;

                                charset = meta.Substring(start_index, end_index - start_index);
                            }
                        }
                    }
                }
            }

            Encoding encoding = null;

            if (charset == null)
            {
                encoding = Encoding.UTF8; //default encoding
            }
            else
            {
                try
                {
                    encoding = Encoding.GetEncoding(charset);
                }
                catch (Exception)
                {
                    Debug.WriteLine("GetEncoding: " + charset);
                    encoding = Encoding.UTF8;
                }
            }

            rawStream.Seek(0, SeekOrigin.Begin);

            using (StreamReader reader = new StreamReader(rawStream, encoding))
            {
                return reader.ReadToEnd();
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
                    new Library.Configuration.SettingContent<HashSet<uint>>() { Name = "Ipv4AddressSet", Value = new HashSet<uint>() },
                    new Library.Configuration.SettingContent<HashSet<SearchRange<uint>>>() { Name = "Ipv4AddressRangeSet", Value = new HashSet<SearchRange<uint>>() },
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

            public HashSet<uint> Ipv4AddressSet
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (HashSet<uint>)this["Ipv4AddressSet"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["Ipv4AddressSet"] = value;
                    }
                }
            }

            public HashSet<SearchRange<uint>> Ipv4AddressRangeSet
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (HashSet<SearchRange<uint>>)this["Ipv4AddressRangeSet"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["Ipv4AddressRangeSet"] = value;
                    }
                }
            }
        }

        [DataContract(Name = "SearchRange", Namespace = "http://Amoeba/CatharsisManager")]
        struct SearchRange<T> : IEquatable<SearchRange<T>>
            where T : IComparable, IEquatable<T>
        {
            private T _min;
            private T _max;

            public SearchRange(T min, T max)
            {
                _min = min;
                _max = (max.CompareTo(_min) < 0) ? _min : max;
            }

            [DataMember(Name = "Min")]
            public T Min
            {
                get
                {
                    return _min;
                }
                private set
                {
                    _min = value;
                }
            }

            [DataMember(Name = "Max")]
            public T Max
            {
                get
                {
                    return _max;
                }
                private set
                {
                    _max = value;
                }
            }

            public bool Verify(T value)
            {
                if (value.CompareTo(this.Min) < 0 || value.CompareTo(this.Max) > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public override int GetHashCode()
            {
                return this.Min.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is SearchRange<T>)) return false;

                return this.Equals((SearchRange<T>)obj);
            }

            public bool Equals(SearchRange<T> other)
            {
                if ((object)other == null) return false;
                if (object.ReferenceEquals(this, other)) return true;

                if (!this.Min.Equals(other.Min) || !this.Max.Equals(other.Max))
                {
                    return false;
                }

                return true;
            }

            public override string ToString()
            {
                return string.Format("Min = {0}, Max = {1}", this.Min, this.Max);
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
