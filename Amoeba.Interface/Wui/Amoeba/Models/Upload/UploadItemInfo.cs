using System;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(UploadItemInfo))]
    public class UploadItemInfo : IEquatable<UploadItemInfo>
    {
        public UploadItemInfo(string path)
        {
            this.Path = path;
        }

        [DataMember(Name = nameof(Path))]
        public string Path { get; private set; }

        public override int GetHashCode()
        {
            return this.Path?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is DownloadItemInfo)) return false;

            return this.Equals((DownloadItemInfo)obj);
        }

        public bool Equals(UploadItemInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Path != other.Path)
            {
                return false;
            }

            return true;
        }
    }
}
