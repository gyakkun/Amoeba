using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Amoeba;

namespace Amoeba
{
    [DataContract(Name = "Ipv4AddressFilter", Namespace = "http://Amoeba")]
    class Ipv4AddressFilter
    {
        private string _proxyUri;
        private List<string> _urls;
        private List<string> _paths;

        public Ipv4AddressFilter(string proxyUri, IEnumerable<string> urls, IEnumerable<string> paths)
        {
            this.ProxyUri = proxyUri;
            if (urls != null) this.ProtectedUrls.AddRange(urls);
            if (paths != null) this.ProtectedPaths.AddRange(paths);
        }

        [DataMember(Name = "ProxyUri")]
        public string ProxyUri
        {
            get
            {
                return _proxyUri;
            }
            private set
            {
                _proxyUri = value;
            }
        }

        private volatile ReadOnlyCollection<string> _readOnlyUrls;

        public IEnumerable<string> Urls
        {
            get
            {
                if (_readOnlyUrls == null)
                    _readOnlyUrls = new ReadOnlyCollection<string>(this.ProtectedUrls.ToArray());

                return _readOnlyUrls;
            }
        }

        [DataMember(Name = "Urls")]
        private List<string> ProtectedUrls
        {
            get
            {
                if (_urls == null)
                    _urls = new List<string>();

                return _urls;
            }
        }

        private volatile ReadOnlyCollection<string> _readOnlyPaths;

        public IEnumerable<string> Paths
        {
            get
            {
                if (_readOnlyPaths == null)
                    _readOnlyPaths = new ReadOnlyCollection<string>(this.ProtectedPaths.ToArray());

                return _readOnlyPaths;
            }
        }

        [DataMember(Name = "Paths")]
        private List<string> ProtectedPaths
        {
            get
            {
                if (_paths == null)
                    _paths = new List<string>();

                return _paths;
            }
        }
    }
}
