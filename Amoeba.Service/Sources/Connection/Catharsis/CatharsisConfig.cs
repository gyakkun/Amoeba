using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Amoeba
{
    [DataContract(Name = nameof(CatharsisConfig))]
    public class CatharsisConfig
    {
        private CatharsisIpv4Config _ipv4Config;

        public CatharsisConfig(CatharsisIpv4Config ipv4Config)
        {
            this.Ipv4Config = ipv4Config;
        }

        [DataMember(Name = nameof(Ipv4Config))]
        public CatharsisIpv4Config Ipv4Config
        {
            get
            {
                return _ipv4Config;
            }
            private set
            {
                _ipv4Config = value;
            }
        }
    }

    [DataContract(Name = nameof(CatharsisIpv4Config))]
    public class CatharsisIpv4Config
    {
        private List<string> _urls;
        private List<string> _paths;

        public CatharsisIpv4Config(IEnumerable<string> urls, IEnumerable<string> paths)
        {
            if (urls != null) this.ProtectedUrls.AddRange(urls);
            if (paths != null) this.ProtectedPaths.AddRange(paths);
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

        [DataMember(Name = nameof(Urls))]
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
                    _readOnlyPaths = new ReadOnlyCollection<string>(this.ProtectedPaths);

                return _readOnlyPaths;
            }
        }

        [DataMember(Name = nameof(Paths))]
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
