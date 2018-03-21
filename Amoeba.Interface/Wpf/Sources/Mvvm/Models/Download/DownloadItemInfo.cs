using System;
using System.Runtime.Serialization;
using Amoeba.Messages;
using Amoeba.Rpc;
using Newtonsoft.Json;

namespace Amoeba.Interface
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    class DownloadItemInfo : IEquatable<DownloadItemInfo>
    {
        public DownloadItemInfo(Seed seed, string path)
        {
            this.Seed = seed;
            this.Path = path;
        }

        [JsonProperty]
        public Seed Seed { get; private set; }

        [JsonProperty]
        public string Path { get; private set; }

        public override int GetHashCode()
        {
            return this.Seed?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is DownloadItemInfo)) return false;

            return this.Equals((DownloadItemInfo)obj);
        }

        public bool Equals(DownloadItemInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Seed != other.Seed
                || this.Path != other.Path)
            {
                return false;
            }

            return true;
        }
    }
}
