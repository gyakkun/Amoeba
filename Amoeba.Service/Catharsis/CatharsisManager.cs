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
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net;
using Library.Net.Amoeba;
using Library.Utilities;

namespace Amoeba
{
    class CatharsisManager : ManagerBase, Library.Configuration.ISettings
    {
        private string _basePath;
        private AmoebaManager _amoebaManager;
        private BufferManager _bufferManager;

        private Settings _settings;

        private Regex _regex = new Regex(@"(.*?):(.*):(\d*)", RegexOptions.Compiled);
        private Regex _regex2 = new Regex(@"(.*?):(.*)", RegexOptions.Compiled);

        private WatchTimer _watchTimer;

        private VolatileHashSet<string> _succeededUris;
        private VolatileHashSet<string> _failedUris;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public CatharsisManager(string basePath, AmoebaManager amoebaManager, BufferManager bufferManager)
        {
            _basePath = basePath;
            _amoebaManager = amoebaManager;
            _bufferManager = bufferManager;

            _settings = new Settings();

            _watchTimer = new WatchTimer(this.WatchTimer, Timeout.Infinite);

            _succeededUris = new VolatileHashSet<string>(new TimeSpan(1, 0, 0));
            _failedUris = new VolatileHashSet<string>(new TimeSpan(1, 0, 0));

            _amoebaManager.CheckUriEvent = this.ResultCache_CheckUri;
        }

        private bool ResultCache_CheckUri(object sender, string uri)
        {
            _succeededUris.Update();
            _failedUris.Update();

            if (_succeededUris.Contains(uri)) return true;
            if (_failedUris.Contains(uri)) return false;

            if (this.CheckUri(uri))
            {
                _succeededUris.Add(uri);

                return true;
            }
            else
            {
                _failedUris.Add(uri);

                return false;
            }
        }

        private bool CheckUri(string uri)
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

            if (host == null) return false;

            IPAddress ip;

            if (IPAddress.TryParse(host, out ip))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    uint uip = NetworkConverter.ToUInt32(ip.GetAddressBytes());

                    lock (_thisLock)
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

        public IEnumerable<Ipv4AddressFilter> Ipv4AddressFilters
        {
            get
            {
                lock (_thisLock)
                {
                    return _settings.Ipv4AddressFilters.ToArray();
                }
            }
        }

        public void SetIpv4AddressFilters(IEnumerable<Ipv4AddressFilter> collection)
        {
            lock (_thisLock)
            {
                lock (_settings.Ipv4AddressFilters.ThisLock)
                {
                    _settings.Ipv4AddressFilters.Clear();
                    _settings.Ipv4AddressFilters.AddRange(collection);
                }

                Task.Run(() => this.Update());
            }
        }

        private void WatchTimer()
        {
            this.Update();
        }

        private object _updateLock = new object();

        private void Update()
        {
            try
            {
                lock (_updateLock)
                {
                    var ipv4AddressSet = new HashSet<uint>();
                    var ipv4AddressRangeSet = new HashSet<SearchRange<uint>>();

                    foreach (var ipv4AddressFilter in _settings.Ipv4AddressFilters.ToArray())
                    {
                        // path
                        {
                            foreach (var path in ipv4AddressFilter.Paths)
                            {
                                using (var stream = new FileStream(Path.Combine(_basePath, path), FileMode.OpenOrCreate))
                                using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
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
                            }
                        }

                        // Url
                        if (!string.IsNullOrWhiteSpace(ipv4AddressFilter.ProxyUri))
                        {
                            string proxyScheme = null;
                            string proxyHost = null;
                            int proxyPort = -1;

                            {
                                var match = _regex.Match(ipv4AddressFilter.ProxyUri);

                                if (match.Success)
                                {
                                    proxyScheme = match.Groups[1].Value;
                                    proxyHost = match.Groups[2].Value;
                                    proxyPort = int.Parse(match.Groups[3].Value);
                                }
                                else
                                {
                                    var match2 = _regex2.Match(ipv4AddressFilter.ProxyUri);

                                    if (match2.Success)
                                    {
                                        proxyScheme = match2.Groups[1].Value;
                                        proxyHost = match2.Groups[2].Value;
                                        proxyPort = 80;
                                    }
                                }
                            }

                            if (proxyHost == null) goto End;

                            var proxy = new WebProxy(proxyHost, proxyPort);

                            foreach (var url in ipv4AddressFilter.Urls)
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

                            End:;
                        }
                    }

                    lock (_thisLock)
                    {
                        _settings.Ipv4AddressSet.Clear();
                        _settings.Ipv4AddressSet.UnionWith(ipv4AddressSet);

                        _settings.Ipv4AddressRangeSet.Clear();
                        _settings.Ipv4AddressRangeSet.UnionWith(ipv4AddressRangeSet);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private static Stream GetStream(string url, IWebProxy proxy)
        {
            BufferManager bufferManager = BufferManager.Instance;

            for (int i = 0; i < 10; i++)
            {
                var bufferStream = new BufferStream(bufferManager);

                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.AllowAutoRedirect = true;
                    request.Proxy = proxy;
                    request.Headers.Add("Pragma", "no-cache");
                    request.Headers.Add("Cache-Control", "no-cache");
                    request.Timeout = 1000 * 60 * 5;
                    request.ReadWriteTimeout = 1000 * 60 * 5;

                    using (WebResponse response = request.GetResponse())
                    {
                        if (response.ContentLength > 1024 * 1024 * 32) throw new Exception("too large");

                        using (Stream stream = response.GetResponseStream())
                        using (var safeBuffer = bufferManager.CreateSafeBuffer(1024 * 4))
                        {
                            int length;

                            while ((length = stream.Read(safeBuffer.Value, 0, safeBuffer.Value.Length)) > 0)
                            {
                                bufferStream.Write(safeBuffer.Value, 0, length);

                                if (bufferStream.Length > 1024 * 1024 * 32) throw new Exception("too large");
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

        private unsafe static uint[] GetStringToIpv4(string value)
        {
            var list = value.Split('.', '-');
            if (list.Length != 8) return null;

            uint[] ip = new uint[2];

            fixed (uint* p = ip)
            {
                var bp = (byte*)p;

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

        #region ISettings

        public void Load(string directoryPath)
        {
            lock (_thisLock)
            {
                _settings.Load(directoryPath);

                _watchTimer.Change(new TimeSpan(0, 0, 0), new TimeSpan(1, 0, 0, 0));
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
                    new Library.Configuration.SettingContent<LockedList<Ipv4AddressFilter>>() { Name = "Ipv4AddressFilters", Value = new LockedList<Ipv4AddressFilter>() },
                    new Library.Configuration.SettingContent<HashSet<uint>>() { Name = "Ipv4AddressSet", Value = new HashSet<uint>() },
                    new Library.Configuration.SettingContent<HashSet<SearchRange<uint>>>() { Name = "Ipv4AddressRangeSet", Value = new HashSet<SearchRange<uint>>() },
                })
            {

            }

            public LockedList<Ipv4AddressFilter> Ipv4AddressFilters
            {
                get
                {
                    return (LockedList<Ipv4AddressFilter>)this["Ipv4AddressFilters"];
                }
            }

            public HashSet<uint> Ipv4AddressSet
            {
                get
                {
                    return (HashSet<uint>)this["Ipv4AddressSet"];
                }
            }

            public HashSet<SearchRange<uint>> Ipv4AddressRangeSet
            {
                get
                {
                    return (HashSet<SearchRange<uint>>)this["Ipv4AddressRangeSet"];
                }
            }
        }

        [DataContract(Name = "SearchRange")]
        struct SearchRange<T> : IEquatable<SearchRange<T>>
            where T : IComparable<T>, IEquatable<T>
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
                return this.Min.GetHashCode() ^ this.Max.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is SearchRange<T>)) return false;

                return this.Equals((SearchRange<T>)obj);
            }

            public bool Equals(SearchRange<T> other)
            {
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
    }
}
