using Amoeba.Service;
using Omnius.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(DownloadItemInfo))]
    class DownloadItemInfo
    {
        public DownloadItemInfo(Seed seed, string path)
        {
            this.Seed = seed;
            this.Path = path;
        }

        [DataMember(Name = nameof(Seed))]
        public Seed Seed { get; set; }

        [DataMember(Name = nameof(Path))]
        public string Path { get; set; }
    }
}
