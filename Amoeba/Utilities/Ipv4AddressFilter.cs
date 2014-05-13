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

        public Ipv4AddressFilter(string proxyUri, IEnumerable<string> urls)
        {
            this.ProxyUri = proxyUri;
            this.ProtectedUrls.AddRange(urls);
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
                    _readOnlyUrls = new ReadOnlyCollection<string>(this.ProtectedUrls);

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
    }
}
