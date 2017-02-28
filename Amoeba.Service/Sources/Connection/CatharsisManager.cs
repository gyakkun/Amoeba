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
using System.Windows;
using System.Xml;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Utilities;

namespace Amoeba.Service
{
    class CatharsisManager : ManagerBase, ISettings
    {
        private BufferManager _bufferManager;

        private Settings _settings;

        private HashSet<Ipv4> _ipv4Set;
        private HashSet<SearchRange<Ipv4>> _ipv4RangeSet;

        private WatchTimer _watchTimer;

        private VolatileHashSet<Ipv4> _succeededIpv4Set;
        private VolatileHashSet<Ipv4> _failedIpv4Set;

        private readonly Regex _regex = new Regex(@"(.*?):(.*):(\d*)", RegexOptions.Compiled);
        private readonly Regex _regex2 = new Regex(@"(.*?):(.*)", RegexOptions.Compiled);

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public CatharsisManager(string configPath, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            _settings = new Settings(configPath);

            _watchTimer = new WatchTimer(this.WatchThread, new TimeSpan(0, 0, 0), new TimeSpan(1, 0, 0, 0));

            _succeededIpv4Set = new VolatileHashSet<Ipv4>(new TimeSpan(0, 30, 0));
            _failedIpv4Set = new VolatileHashSet<Ipv4>(new TimeSpan(0, 30, 0));
        }

        public bool Check(IPAddress ip)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork) return false;

            var ipv4 = new Ipv4(ip);

            lock (_thisLock)
            {
                if (_settings.Ipv4AddressSet.Contains(uip)) return false;

                foreach (var range in _settings.Ipv4AddressRangeSet)
                {
                    if (range.Verify(uip)) return false;
                }
            }

            return true;
        }

        private void WatchTimer(object state)
        {
            if (_isWatching) return;
            _isWatching = true;

            try
            {
                var ipv4AddressSet = new HashSet<uint>();
                var ipv4AddressRangeSet = new HashSet<SearchRange<uint>>();

                foreach (var ipv4AddressFilter in _serviceManager.Config.Catharsis.Ipv4AddressFilters)
                {
                    // path
                    {
                        foreach (var path in ipv4AddressFilter.Paths)
                        {
                            using (var stream = new FileStream(Path.Combine(_serviceManager.Paths["Configuration"], path), FileMode.OpenOrCreate))
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
            finally
            {
                _isWatching = false;
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

        [DataContract(Name = "Ipv4")]
        struct Ipv4 : IComparable<Ipv4>, IEquatable<Ipv4>
        {
            private uint _value;

            public Ipv4(IPAddress ipAddress)
            {
                if (ipAddress.AddressFamily != AddressFamily.InterNetwork) throw new ArgumentException(nameof(ipAddress));
                _value = NetworkConverter.ToUInt32(ipAddress.GetAddressBytes());
            }

            [DataMember(Name = "Value")]
            private uint Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                }
            }

            public override int GetHashCode()
            {
                return (int)this.Value;
            }

            public override bool Equals(object obj)
            {
                if ((object)obj == null || !(obj is Ipv4)) return false;

                return this.Equals((Ipv4)obj);
            }

            public bool Equals(Ipv4 other)
            {
                if (this.Value != other.Value)
                {
                    return false;
                }

                return true;
            }

            public int CompareTo(Ipv4 other)
            {
                return this.Value.CompareTo(other.Value);
            }

            public override string ToString()
            {
                return new IPAddress(NetworkConverter.GetBytes(this.Value)).ToString();
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
