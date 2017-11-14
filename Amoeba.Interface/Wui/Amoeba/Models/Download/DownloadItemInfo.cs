using System;
using System.Runtime.Serialization;
using Amoeba.Messages;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(DownloadItemInfo))]
    public class DownloadItemInfo : IEquatable<DownloadItemInfo>
    {
        public DownloadItemInfo(Seed seed, string path)
        {
            this.Seed = seed;
            this.Path = path;
        }

        [DataMember(Name = nameof(Seed))]
        public Seed Seed { get; private set; }

        [DataMember(Name = nameof(Path))]
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
