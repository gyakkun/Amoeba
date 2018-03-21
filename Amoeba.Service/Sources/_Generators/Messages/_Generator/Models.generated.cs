using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amoeba.Messages;
using Newtonsoft.Json;
using Omnius.Base;
using Omnius.Security;
using Omnius.Serialization;
using Omnius.Utils;

namespace Amoeba.Service
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class Group : MessageBase<Group>
    {
        static Group()
        {
            Group.Formatter = new CustomFormatter();
        }
        public static readonly int MaxHashesCount = 1024 * 1024;
        [JsonConstructor]
        public Group(CorrectionAlgorithm correctionAlgorithm, long length, IList<Hash> hashes)
        {
            if (hashes == null) throw new ArgumentNullException("hashes");
            if (hashes.Count > MaxHashesCount) throw new ArgumentOutOfRangeException("hashes");
            for (int i = 0; i < hashes.Count; i++)
            {
                if (hashes[i] == null) throw new ArgumentNullException("hashes[i]");
            }
            this.CorrectionAlgorithm = correctionAlgorithm;
            this.Length = length;
            this.Hashes = new ReadOnlyCollection<Hash>(hashes);
            this.Initialize();
        }
        [JsonProperty]
        public CorrectionAlgorithm CorrectionAlgorithm { get; }
        [JsonProperty]
        public long Length { get; }
        [JsonProperty]
        public IReadOnlyList<Hash> Hashes { get; }
        public override bool Equals(Group target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.CorrectionAlgorithm != target.CorrectionAlgorithm) return false;
            if (this.Length != target.Length) return false;
            if (!CollectionUtils.Equals(this.Hashes, target.Hashes)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.CorrectionAlgorithm != default(CorrectionAlgorithm)) h ^= this.CorrectionAlgorithm.GetHashCode();
                if (this.Length != default(long)) h ^= this.Length.GetHashCode();
                for (int i = 0; i < Hashes.Count; i++)
                {
                    h ^= this.Hashes[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // CorrectionAlgorithm
            if (this.CorrectionAlgorithm != default(CorrectionAlgorithm))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.CorrectionAlgorithm);
            }
            // Length
            if (this.Length != default(long))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize((ulong)this.Length);
            }
            // Hashes
            if (this.Hashes.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                s += MessageSizeComputer.GetSize((ulong)this.Hashes.Count);
                for (int i = 0; i < this.Hashes.Count; i++)
                {
                    var size_1 = this.Hashes[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<Group>
        {
            public void Serialize(MessageStreamWriter w, Group value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // CorrectionAlgorithm
                if (value.CorrectionAlgorithm != default(CorrectionAlgorithm))
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.CorrectionAlgorithm);
                }
                // Length
                if (value.Length != default(long))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Length);
                }
                // Hashes
                if (value.Hashes.Count != 0)
                {
                    w.Write((ulong)2);
                    w.Write((ulong)value.Hashes.Count);
                    for (int i = 0; i < value.Hashes.Count; i++)
                    {
                        w.Write((ulong)value.Hashes[i].GetMessageSize());
                        Hash.Formatter.Serialize(w, value.Hashes[i], rank + 1);
                    }
                }
            }
            public Group Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                CorrectionAlgorithm p_correctionAlgorithm = default(CorrectionAlgorithm);
                long p_length = default(long);
                Hash[] p_hashes = Array.Empty<Hash>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //CorrectionAlgorithm
                            {
                                p_correctionAlgorithm = (CorrectionAlgorithm)r.GetUInt64();
                                break;
                            }
                        case 1: //Length
                            {
                                p_length = (long)r.GetUInt64();
                                break;
                            }
                        case 2: //Hashes
                            {
                                var length = (long)r.GetUInt64();
                                p_hashes = new Hash[Math.Min(length, 1024 * 1024)];
                                for (int i = 0; i < p_hashes.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_hashes[i] = Hash.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new Group(p_correctionAlgorithm, p_length, p_hashes);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class Index : MessageBase<Index>
    {
        static Index()
        {
            Index.Formatter = new CustomFormatter();
        }
        public static readonly int MaxGroupsCount = 1024 * 1024;
        [JsonConstructor]
        public Index(IList<Group> groups)
        {
            if (groups == null) throw new ArgumentNullException("groups");
            if (groups.Count > MaxGroupsCount) throw new ArgumentOutOfRangeException("groups");
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] == null) throw new ArgumentNullException("groups[i]");
            }
            this.Groups = new ReadOnlyCollection<Group>(groups);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<Group> Groups { get; }
        public override bool Equals(Index target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Groups, target.Groups)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Groups.Count; i++)
                {
                    h ^= this.Groups[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Groups
            if (this.Groups.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Groups.Count);
                for (int i = 0; i < this.Groups.Count; i++)
                {
                    var size_1 = this.Groups[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<Index>
        {
            public void Serialize(MessageStreamWriter w, Index value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Groups
                if (value.Groups.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Groups.Count);
                    for (int i = 0; i < value.Groups.Count; i++)
                    {
                        w.Write((ulong)value.Groups[i].GetMessageSize());
                        Group.Formatter.Serialize(w, value.Groups[i], rank + 1);
                    }
                }
            }
            public Index Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Group[] p_groups = Array.Empty<Group>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Groups
                            {
                                var length = (long)r.GetUInt64();
                                p_groups = new Group[Math.Min(length, 1024 * 1024)];
                                for (int i = 0; i < p_groups.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_groups[i] = Group.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new Index(p_groups);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class ProfilePacket : MessageBase<ProfilePacket>
    {
        static ProfilePacket()
        {
            ProfilePacket.Formatter = new CustomFormatter();
        }
        [JsonConstructor]
        public ProfilePacket(byte[] id, Location location)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (location == null) throw new ArgumentNullException("location");
            this.Id = id;
            this.Location = location;
            this.Initialize();
        }
        [JsonProperty]
        public byte[] Id { get; }
        [JsonProperty]
        public Location Location { get; }
        public override bool Equals(ProfilePacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if ((this.Id == null) != (target.Id == null)) return false;
            if ((this.Id != null && target.Id != null)
                && !Unsafe.Equals(this.Id, target.Id)) return false;
            if (this.Location != target.Location) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Id != default(byte[])) h ^= MessageUtils.GetHashCode(this.Id);
                if (this.Location != default(Location)) h ^= this.Location.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Id
            if (this.Id != default(byte[]))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Id);
            }
            // Location
            if (this.Location != default(Location))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                var size = this.Location.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<ProfilePacket>
        {
            public void Serialize(MessageStreamWriter w, ProfilePacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Id
                if (value.Id != default(byte[]))
                {
                    w.Write((ulong)0);
                    w.Write(value.Id);
                }
                // Location
                if (value.Location != default(Location))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Location.GetMessageSize());
                    Location.Formatter.Serialize(w, value.Location, rank + 1);
                }
            }
            public ProfilePacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                byte[] p_id = default(byte[]);
                Location p_location = default(Location);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Id
                            {
                                p_id = r.GetBytes();
                                break;
                            }
                        case 1: //Location
                            {
                                var size = (long)r.GetUInt64();
                                p_location = Location.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new ProfilePacket(p_id, p_location);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class LocationsPacket : MessageBase<LocationsPacket>
    {
        static LocationsPacket()
        {
            LocationsPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxLocationsCount = 1024 * 256;
        [JsonConstructor]
        public LocationsPacket(IList<Location> locations)
        {
            if (locations == null) throw new ArgumentNullException("locations");
            if (locations.Count > MaxLocationsCount) throw new ArgumentOutOfRangeException("locations");
            for (int i = 0; i < locations.Count; i++)
            {
                if (locations[i] == null) throw new ArgumentNullException("locations[i]");
            }
            this.Locations = new ReadOnlyCollection<Location>(locations);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<Location> Locations { get; }
        public override bool Equals(LocationsPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Locations, target.Locations)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Locations.Count; i++)
                {
                    h ^= this.Locations[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Locations
            if (this.Locations.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Locations.Count);
                for (int i = 0; i < this.Locations.Count; i++)
                {
                    var size_1 = this.Locations[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<LocationsPacket>
        {
            public void Serialize(MessageStreamWriter w, LocationsPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Locations
                if (value.Locations.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Locations.Count);
                    for (int i = 0; i < value.Locations.Count; i++)
                    {
                        w.Write((ulong)value.Locations[i].GetMessageSize());
                        Location.Formatter.Serialize(w, value.Locations[i], rank + 1);
                    }
                }
            }
            public LocationsPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Location[] p_locations = Array.Empty<Location>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Locations
                            {
                                var length = (long)r.GetUInt64();
                                p_locations = new Location[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_locations.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_locations[i] = Location.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new LocationsPacket(p_locations);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class BroadcastMetadata : MessageBase<BroadcastMetadata>
    {
        static BroadcastMetadata()
        {
            BroadcastMetadata.Formatter = new CustomFormatter();
        }
        public static readonly int MaxTypeLength = 256;
        [JsonConstructor]
        public BroadcastMetadata(string type, DateTime creationTime, Metadata metadata, Certificate certificate)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (metadata == null) throw new ArgumentNullException("metadata");
            if (type.Length > MaxTypeLength) throw new ArgumentOutOfRangeException("type");
            this.Type = type;
            this.CreationTime = creationTime;
            this.Metadata = metadata;
            this.Certificate = certificate;
            this.Initialize();
        }
        [JsonProperty]
        public string Type { get; }
        [JsonProperty]
        public DateTime CreationTime { get; }
        [JsonProperty]
        public Metadata Metadata { get; }
        [JsonProperty]
        public Certificate Certificate { get; }
        public override bool Equals(BroadcastMetadata target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Type != target.Type) return false;
            if (this.CreationTime != target.CreationTime) return false;
            if (this.Metadata != target.Metadata) return false;
            if (this.Certificate != target.Certificate) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Type != default(string)) h ^= this.Type.GetHashCode();
                if (this.CreationTime != default(DateTime)) h ^= this.CreationTime.GetHashCode();
                if (this.Metadata != default(Metadata)) h ^= this.Metadata.GetHashCode();
                if (this.Certificate != default(Certificate)) h ^= this.Certificate.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Type
            if (this.Type != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Type);
            }
            // CreationTime
            if (this.CreationTime != default(DateTime))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize(this.CreationTime);
            }
            // Metadata
            if (this.Metadata != default(Metadata))
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                var size = this.Metadata.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // Certificate
            if (this.Certificate != default(Certificate))
            {
                s += MessageSizeComputer.GetSize((ulong)3);
                var size = this.Certificate.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<BroadcastMetadata>
        {
            public void Serialize(MessageStreamWriter w, BroadcastMetadata value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Type
                if (value.Type != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Type);
                }
                // CreationTime
                if (value.CreationTime != default(DateTime))
                {
                    w.Write((ulong)1);
                    w.Write(value.CreationTime);
                }
                // Metadata
                if (value.Metadata != default(Metadata))
                {
                    w.Write((ulong)2);
                    w.Write((ulong)value.Metadata.GetMessageSize());
                    Metadata.Formatter.Serialize(w, value.Metadata, rank + 1);
                }
                // Certificate
                if (value.Certificate != default(Certificate))
                {
                    w.Write((ulong)3);
                    w.Write((ulong)value.Certificate.GetMessageSize());
                    Certificate.Formatter.Serialize(w, value.Certificate, rank + 1);
                }
            }
            public BroadcastMetadata Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_type = default(string);
                DateTime p_creationTime = default(DateTime);
                Metadata p_metadata = default(Metadata);
                Certificate p_certificate = default(Certificate);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Type
                            {
                                p_type = r.GetString();
                                break;
                            }
                        case 1: //CreationTime
                            {
                                p_creationTime = r.GetDateTime();
                                break;
                            }
                        case 2: //Metadata
                            {
                                var size = (long)r.GetUInt64();
                                p_metadata = Metadata.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 3: //Certificate
                            {
                                var size = (long)r.GetUInt64();
                                p_certificate = Certificate.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new BroadcastMetadata(p_type, p_creationTime, p_metadata, p_certificate);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class MulticastMetadata : MessageBase<MulticastMetadata>
    {
        static MulticastMetadata()
        {
            MulticastMetadata.Formatter = new CustomFormatter();
        }
        public static readonly int MaxTypeLength = 256;
        [JsonConstructor]
        public MulticastMetadata(string type, Tag tag, DateTime creationTime, Metadata metadata, Cash cash, Certificate certificate)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (tag == null) throw new ArgumentNullException("tag");
            if (metadata == null) throw new ArgumentNullException("metadata");
            if (type.Length > MaxTypeLength) throw new ArgumentOutOfRangeException("type");
            this.Type = type;
            this.Tag = tag;
            this.CreationTime = creationTime;
            this.Metadata = metadata;
            this.Cash = cash;
            this.Certificate = certificate;
            this.Initialize();
        }
        [JsonProperty]
        public string Type { get; }
        [JsonProperty]
        public Tag Tag { get; }
        [JsonProperty]
        public DateTime CreationTime { get; }
        [JsonProperty]
        public Metadata Metadata { get; }
        [JsonProperty]
        public Cash Cash { get; }
        [JsonProperty]
        public Certificate Certificate { get; }
        public override bool Equals(MulticastMetadata target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Type != target.Type) return false;
            if (this.Tag != target.Tag) return false;
            if (this.CreationTime != target.CreationTime) return false;
            if (this.Metadata != target.Metadata) return false;
            if (this.Cash != target.Cash) return false;
            if (this.Certificate != target.Certificate) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Type != default(string)) h ^= this.Type.GetHashCode();
                if (this.Tag != default(Tag)) h ^= this.Tag.GetHashCode();
                if (this.CreationTime != default(DateTime)) h ^= this.CreationTime.GetHashCode();
                if (this.Metadata != default(Metadata)) h ^= this.Metadata.GetHashCode();
                if (this.Cash != default(Cash)) h ^= this.Cash.GetHashCode();
                if (this.Certificate != default(Certificate)) h ^= this.Certificate.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Type
            if (this.Type != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Type);
            }
            // Tag
            if (this.Tag != default(Tag))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                var size = this.Tag.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // CreationTime
            if (this.CreationTime != default(DateTime))
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                s += MessageSizeComputer.GetSize(this.CreationTime);
            }
            // Metadata
            if (this.Metadata != default(Metadata))
            {
                s += MessageSizeComputer.GetSize((ulong)3);
                var size = this.Metadata.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // Cash
            if (this.Cash != default(Cash))
            {
                s += MessageSizeComputer.GetSize((ulong)4);
                var size = this.Cash.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // Certificate
            if (this.Certificate != default(Certificate))
            {
                s += MessageSizeComputer.GetSize((ulong)5);
                var size = this.Certificate.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<MulticastMetadata>
        {
            public void Serialize(MessageStreamWriter w, MulticastMetadata value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Type
                if (value.Type != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Type);
                }
                // Tag
                if (value.Tag != default(Tag))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Tag.GetMessageSize());
                    Tag.Formatter.Serialize(w, value.Tag, rank + 1);
                }
                // CreationTime
                if (value.CreationTime != default(DateTime))
                {
                    w.Write((ulong)2);
                    w.Write(value.CreationTime);
                }
                // Metadata
                if (value.Metadata != default(Metadata))
                {
                    w.Write((ulong)3);
                    w.Write((ulong)value.Metadata.GetMessageSize());
                    Metadata.Formatter.Serialize(w, value.Metadata, rank + 1);
                }
                // Cash
                if (value.Cash != default(Cash))
                {
                    w.Write((ulong)4);
                    w.Write((ulong)value.Cash.GetMessageSize());
                    Cash.Formatter.Serialize(w, value.Cash, rank + 1);
                }
                // Certificate
                if (value.Certificate != default(Certificate))
                {
                    w.Write((ulong)5);
                    w.Write((ulong)value.Certificate.GetMessageSize());
                    Certificate.Formatter.Serialize(w, value.Certificate, rank + 1);
                }
            }
            public MulticastMetadata Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_type = default(string);
                Tag p_tag = default(Tag);
                DateTime p_creationTime = default(DateTime);
                Metadata p_metadata = default(Metadata);
                Cash p_cash = default(Cash);
                Certificate p_certificate = default(Certificate);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Type
                            {
                                p_type = r.GetString();
                                break;
                            }
                        case 1: //Tag
                            {
                                var size = (long)r.GetUInt64();
                                p_tag = Tag.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 2: //CreationTime
                            {
                                p_creationTime = r.GetDateTime();
                                break;
                            }
                        case 3: //Metadata
                            {
                                var size = (long)r.GetUInt64();
                                p_metadata = Metadata.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 4: //Cash
                            {
                                var size = (long)r.GetUInt64();
                                p_cash = Cash.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 5: //Certificate
                            {
                                var size = (long)r.GetUInt64();
                                p_certificate = Certificate.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new MulticastMetadata(p_type, p_tag, p_creationTime, p_metadata, p_cash, p_certificate);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UnicastMetadata : MessageBase<UnicastMetadata>
    {
        static UnicastMetadata()
        {
            UnicastMetadata.Formatter = new CustomFormatter();
        }
        public static readonly int MaxTypeLength = 256;
        [JsonConstructor]
        public UnicastMetadata(string type, Signature signature, DateTime creationTime, Metadata metadata, Certificate certificate)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (signature == null) throw new ArgumentNullException("signature");
            if (metadata == null) throw new ArgumentNullException("metadata");
            if (type.Length > MaxTypeLength) throw new ArgumentOutOfRangeException("type");
            this.Type = type;
            this.Signature = signature;
            this.CreationTime = creationTime;
            this.Metadata = metadata;
            this.Certificate = certificate;
            this.Initialize();
        }
        [JsonProperty]
        public string Type { get; }
        [JsonProperty]
        public Signature Signature { get; }
        [JsonProperty]
        public DateTime CreationTime { get; }
        [JsonProperty]
        public Metadata Metadata { get; }
        [JsonProperty]
        public Certificate Certificate { get; }
        public override bool Equals(UnicastMetadata target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Type != target.Type) return false;
            if (this.Signature != target.Signature) return false;
            if (this.CreationTime != target.CreationTime) return false;
            if (this.Metadata != target.Metadata) return false;
            if (this.Certificate != target.Certificate) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Type != default(string)) h ^= this.Type.GetHashCode();
                if (this.Signature != default(Signature)) h ^= this.Signature.GetHashCode();
                if (this.CreationTime != default(DateTime)) h ^= this.CreationTime.GetHashCode();
                if (this.Metadata != default(Metadata)) h ^= this.Metadata.GetHashCode();
                if (this.Certificate != default(Certificate)) h ^= this.Certificate.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Type
            if (this.Type != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Type);
            }
            // Signature
            if (this.Signature != default(Signature))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                var size = this.Signature.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // CreationTime
            if (this.CreationTime != default(DateTime))
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                s += MessageSizeComputer.GetSize(this.CreationTime);
            }
            // Metadata
            if (this.Metadata != default(Metadata))
            {
                s += MessageSizeComputer.GetSize((ulong)3);
                var size = this.Metadata.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // Certificate
            if (this.Certificate != default(Certificate))
            {
                s += MessageSizeComputer.GetSize((ulong)4);
                var size = this.Certificate.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<UnicastMetadata>
        {
            public void Serialize(MessageStreamWriter w, UnicastMetadata value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Type
                if (value.Type != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Type);
                }
                // Signature
                if (value.Signature != default(Signature))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Signature.GetMessageSize());
                    Signature.Formatter.Serialize(w, value.Signature, rank + 1);
                }
                // CreationTime
                if (value.CreationTime != default(DateTime))
                {
                    w.Write((ulong)2);
                    w.Write(value.CreationTime);
                }
                // Metadata
                if (value.Metadata != default(Metadata))
                {
                    w.Write((ulong)3);
                    w.Write((ulong)value.Metadata.GetMessageSize());
                    Metadata.Formatter.Serialize(w, value.Metadata, rank + 1);
                }
                // Certificate
                if (value.Certificate != default(Certificate))
                {
                    w.Write((ulong)4);
                    w.Write((ulong)value.Certificate.GetMessageSize());
                    Certificate.Formatter.Serialize(w, value.Certificate, rank + 1);
                }
            }
            public UnicastMetadata Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_type = default(string);
                Signature p_signature = default(Signature);
                DateTime p_creationTime = default(DateTime);
                Metadata p_metadata = default(Metadata);
                Certificate p_certificate = default(Certificate);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Type
                            {
                                p_type = r.GetString();
                                break;
                            }
                        case 1: //Signature
                            {
                                var size = (long)r.GetUInt64();
                                p_signature = Signature.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 2: //CreationTime
                            {
                                p_creationTime = r.GetDateTime();
                                break;
                            }
                        case 3: //Metadata
                            {
                                var size = (long)r.GetUInt64();
                                p_metadata = Metadata.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 4: //Certificate
                            {
                                var size = (long)r.GetUInt64();
                                p_certificate = Certificate.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new UnicastMetadata(p_type, p_signature, p_creationTime, p_metadata, p_certificate);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class BlockResultPacket : MessageBase<BlockResultPacket>
    {
        static BlockResultPacket()
        {
            BlockResultPacket.Formatter = new CustomFormatter();
        }
        [JsonConstructor]
        public BlockResultPacket(Hash hash, ArraySegment<byte> value)
        {
            if (hash == null) throw new ArgumentNullException("hash");
            if (value == null) throw new ArgumentNullException("value");
            this.Hash = hash;
            this.Value = value;
            this.Initialize();
        }
        [JsonProperty]
        public Hash Hash { get; }
        [JsonProperty]
        public ArraySegment<byte> Value { get; }
        public override bool Equals(BlockResultPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Hash != target.Hash) return false;
            if ((this.Value.Array == null) != (target.Value.Array == null)) return false;
            if ((this.Value.Array != null && target.Value.Array != null)
                && (this.Value.Count != target.Value.Count
                && !Unsafe.Equals(this.Value.Array, this.Value.Offset, target.Value.Array, target.Value.Offset, this.Value.Count))) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Hash != default(Hash)) h ^= this.Hash.GetHashCode();
                if (this.Value != default(ArraySegment<byte>)) h ^= MessageUtils.GetHashCode(this.Value);
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Hash
            if (this.Hash != default(Hash))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                var size = this.Hash.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // Value
            if (this.Value != default(ArraySegment<byte>))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize(this.Value);
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<BlockResultPacket>
        {
            public void Serialize(MessageStreamWriter w, BlockResultPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Hash
                if (value.Hash != default(Hash))
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Hash.GetMessageSize());
                    Hash.Formatter.Serialize(w, value.Hash, rank + 1);
                }
                // Value
                if (value.Value != default(ArraySegment<byte>))
                {
                    w.Write((ulong)1);
                    w.Write(value.Value.Array, value.Value.Offset, value.Value.Count);
                }
            }
            public BlockResultPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Hash p_hash = default(Hash);
                ArraySegment<byte> p_value = default(ArraySegment<byte>);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Hash
                            {
                                var size = (long)r.GetUInt64();
                                p_hash = Hash.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 1: //Value
                            {
                                p_value = r.GetRecycleBytesSegment();
                                break;
                            }
                    }
                }
                return new BlockResultPacket(p_hash, p_value);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class BlocksLinkPacket : MessageBase<BlocksLinkPacket>
    {
        static BlocksLinkPacket()
        {
            BlocksLinkPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxHashesCount = 1024 * 256;
        [JsonConstructor]
        public BlocksLinkPacket(IList<Hash> hashes)
        {
            if (hashes == null) throw new ArgumentNullException("hashes");
            if (hashes.Count > MaxHashesCount) throw new ArgumentOutOfRangeException("hashes");
            for (int i = 0; i < hashes.Count; i++)
            {
                if (hashes[i] == null) throw new ArgumentNullException("hashes[i]");
            }
            this.Hashes = new ReadOnlyCollection<Hash>(hashes);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<Hash> Hashes { get; }
        public override bool Equals(BlocksLinkPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Hashes, target.Hashes)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Hashes.Count; i++)
                {
                    h ^= this.Hashes[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Hashes
            if (this.Hashes.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Hashes.Count);
                for (int i = 0; i < this.Hashes.Count; i++)
                {
                    var size_1 = this.Hashes[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<BlocksLinkPacket>
        {
            public void Serialize(MessageStreamWriter w, BlocksLinkPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Hashes
                if (value.Hashes.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Hashes.Count);
                    for (int i = 0; i < value.Hashes.Count; i++)
                    {
                        w.Write((ulong)value.Hashes[i].GetMessageSize());
                        Hash.Formatter.Serialize(w, value.Hashes[i], rank + 1);
                    }
                }
            }
            public BlocksLinkPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Hash[] p_hashes = Array.Empty<Hash>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Hashes
                            {
                                var length = (long)r.GetUInt64();
                                p_hashes = new Hash[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_hashes.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_hashes[i] = Hash.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new BlocksLinkPacket(p_hashes);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class BlocksRequestPacket : MessageBase<BlocksRequestPacket>
    {
        static BlocksRequestPacket()
        {
            BlocksRequestPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxHashesCount = 1024 * 256;
        [JsonConstructor]
        public BlocksRequestPacket(IList<Hash> hashes)
        {
            if (hashes == null) throw new ArgumentNullException("hashes");
            if (hashes.Count > MaxHashesCount) throw new ArgumentOutOfRangeException("hashes");
            for (int i = 0; i < hashes.Count; i++)
            {
                if (hashes[i] == null) throw new ArgumentNullException("hashes[i]");
            }
            this.Hashes = new ReadOnlyCollection<Hash>(hashes);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<Hash> Hashes { get; }
        public override bool Equals(BlocksRequestPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Hashes, target.Hashes)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Hashes.Count; i++)
                {
                    h ^= this.Hashes[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Hashes
            if (this.Hashes.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Hashes.Count);
                for (int i = 0; i < this.Hashes.Count; i++)
                {
                    var size_1 = this.Hashes[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<BlocksRequestPacket>
        {
            public void Serialize(MessageStreamWriter w, BlocksRequestPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Hashes
                if (value.Hashes.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Hashes.Count);
                    for (int i = 0; i < value.Hashes.Count; i++)
                    {
                        w.Write((ulong)value.Hashes[i].GetMessageSize());
                        Hash.Formatter.Serialize(w, value.Hashes[i], rank + 1);
                    }
                }
            }
            public BlocksRequestPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Hash[] p_hashes = Array.Empty<Hash>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Hashes
                            {
                                var length = (long)r.GetUInt64();
                                p_hashes = new Hash[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_hashes.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_hashes[i] = Hash.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new BlocksRequestPacket(p_hashes);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class BroadcastMetadatasRequestPacket : MessageBase<BroadcastMetadatasRequestPacket>
    {
        static BroadcastMetadatasRequestPacket()
        {
            BroadcastMetadatasRequestPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxSignaturesCount = 1024 * 256;
        [JsonConstructor]
        public BroadcastMetadatasRequestPacket(IList<Signature> signatures)
        {
            if (signatures == null) throw new ArgumentNullException("signatures");
            if (signatures.Count > MaxSignaturesCount) throw new ArgumentOutOfRangeException("signatures");
            for (int i = 0; i < signatures.Count; i++)
            {
                if (signatures[i] == null) throw new ArgumentNullException("signatures[i]");
            }
            this.Signatures = new ReadOnlyCollection<Signature>(signatures);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<Signature> Signatures { get; }
        public override bool Equals(BroadcastMetadatasRequestPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Signatures, target.Signatures)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Signatures.Count; i++)
                {
                    h ^= this.Signatures[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Signatures
            if (this.Signatures.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Signatures.Count);
                for (int i = 0; i < this.Signatures.Count; i++)
                {
                    var size_1 = this.Signatures[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<BroadcastMetadatasRequestPacket>
        {
            public void Serialize(MessageStreamWriter w, BroadcastMetadatasRequestPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Signatures
                if (value.Signatures.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Signatures.Count);
                    for (int i = 0; i < value.Signatures.Count; i++)
                    {
                        w.Write((ulong)value.Signatures[i].GetMessageSize());
                        Signature.Formatter.Serialize(w, value.Signatures[i], rank + 1);
                    }
                }
            }
            public BroadcastMetadatasRequestPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Signature[] p_signatures = Array.Empty<Signature>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Signatures
                            {
                                var length = (long)r.GetUInt64();
                                p_signatures = new Signature[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_signatures.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_signatures[i] = Signature.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new BroadcastMetadatasRequestPacket(p_signatures);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class BroadcastMetadatasResultPacket : MessageBase<BroadcastMetadatasResultPacket>
    {
        static BroadcastMetadatasResultPacket()
        {
            BroadcastMetadatasResultPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxBroadcastMetadatasCount = 1024 * 256;
        [JsonConstructor]
        public BroadcastMetadatasResultPacket(IList<BroadcastMetadata> broadcastMetadatas)
        {
            if (broadcastMetadatas == null) throw new ArgumentNullException("broadcastMetadatas");
            if (broadcastMetadatas.Count > MaxBroadcastMetadatasCount) throw new ArgumentOutOfRangeException("broadcastMetadatas");
            for (int i = 0; i < broadcastMetadatas.Count; i++)
            {
                if (broadcastMetadatas[i] == null) throw new ArgumentNullException("broadcastMetadatas[i]");
            }
            this.BroadcastMetadatas = new ReadOnlyCollection<BroadcastMetadata>(broadcastMetadatas);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<BroadcastMetadata> BroadcastMetadatas { get; }
        public override bool Equals(BroadcastMetadatasResultPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.BroadcastMetadatas, target.BroadcastMetadatas)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < BroadcastMetadatas.Count; i++)
                {
                    h ^= this.BroadcastMetadatas[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // BroadcastMetadatas
            if (this.BroadcastMetadatas.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.BroadcastMetadatas.Count);
                for (int i = 0; i < this.BroadcastMetadatas.Count; i++)
                {
                    var size_1 = this.BroadcastMetadatas[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<BroadcastMetadatasResultPacket>
        {
            public void Serialize(MessageStreamWriter w, BroadcastMetadatasResultPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // BroadcastMetadatas
                if (value.BroadcastMetadatas.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.BroadcastMetadatas.Count);
                    for (int i = 0; i < value.BroadcastMetadatas.Count; i++)
                    {
                        w.Write((ulong)value.BroadcastMetadatas[i].GetMessageSize());
                        BroadcastMetadata.Formatter.Serialize(w, value.BroadcastMetadatas[i], rank + 1);
                    }
                }
            }
            public BroadcastMetadatasResultPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                BroadcastMetadata[] p_broadcastMetadatas = Array.Empty<BroadcastMetadata>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //BroadcastMetadatas
                            {
                                var length = (long)r.GetUInt64();
                                p_broadcastMetadatas = new BroadcastMetadata[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_broadcastMetadatas.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_broadcastMetadatas[i] = BroadcastMetadata.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new BroadcastMetadatasResultPacket(p_broadcastMetadatas);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class MulticastMetadatasRequestPacket : MessageBase<MulticastMetadatasRequestPacket>
    {
        static MulticastMetadatasRequestPacket()
        {
            MulticastMetadatasRequestPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxTagsCount = 1024 * 256;
        [JsonConstructor]
        public MulticastMetadatasRequestPacket(IList<Tag> tags)
        {
            if (tags == null) throw new ArgumentNullException("tags");
            if (tags.Count > MaxTagsCount) throw new ArgumentOutOfRangeException("tags");
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == null) throw new ArgumentNullException("tags[i]");
            }
            this.Tags = new ReadOnlyCollection<Tag>(tags);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<Tag> Tags { get; }
        public override bool Equals(MulticastMetadatasRequestPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Tags, target.Tags)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Tags.Count; i++)
                {
                    h ^= this.Tags[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Tags
            if (this.Tags.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Tags.Count);
                for (int i = 0; i < this.Tags.Count; i++)
                {
                    var size_1 = this.Tags[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<MulticastMetadatasRequestPacket>
        {
            public void Serialize(MessageStreamWriter w, MulticastMetadatasRequestPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Tags
                if (value.Tags.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Tags.Count);
                    for (int i = 0; i < value.Tags.Count; i++)
                    {
                        w.Write((ulong)value.Tags[i].GetMessageSize());
                        Tag.Formatter.Serialize(w, value.Tags[i], rank + 1);
                    }
                }
            }
            public MulticastMetadatasRequestPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Tag[] p_tags = Array.Empty<Tag>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Tags
                            {
                                var length = (long)r.GetUInt64();
                                p_tags = new Tag[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_tags.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_tags[i] = Tag.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new MulticastMetadatasRequestPacket(p_tags);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class MulticastMetadatasResultPacket : MessageBase<MulticastMetadatasResultPacket>
    {
        static MulticastMetadatasResultPacket()
        {
            MulticastMetadatasResultPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxMulticastMetadatasCount = 1024 * 256;
        [JsonConstructor]
        public MulticastMetadatasResultPacket(IList<MulticastMetadata> multicastMetadatas)
        {
            if (multicastMetadatas == null) throw new ArgumentNullException("multicastMetadatas");
            if (multicastMetadatas.Count > MaxMulticastMetadatasCount) throw new ArgumentOutOfRangeException("multicastMetadatas");
            for (int i = 0; i < multicastMetadatas.Count; i++)
            {
                if (multicastMetadatas[i] == null) throw new ArgumentNullException("multicastMetadatas[i]");
            }
            this.MulticastMetadatas = new ReadOnlyCollection<MulticastMetadata>(multicastMetadatas);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<MulticastMetadata> MulticastMetadatas { get; }
        public override bool Equals(MulticastMetadatasResultPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.MulticastMetadatas, target.MulticastMetadatas)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < MulticastMetadatas.Count; i++)
                {
                    h ^= this.MulticastMetadatas[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // MulticastMetadatas
            if (this.MulticastMetadatas.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.MulticastMetadatas.Count);
                for (int i = 0; i < this.MulticastMetadatas.Count; i++)
                {
                    var size_1 = this.MulticastMetadatas[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<MulticastMetadatasResultPacket>
        {
            public void Serialize(MessageStreamWriter w, MulticastMetadatasResultPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // MulticastMetadatas
                if (value.MulticastMetadatas.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.MulticastMetadatas.Count);
                    for (int i = 0; i < value.MulticastMetadatas.Count; i++)
                    {
                        w.Write((ulong)value.MulticastMetadatas[i].GetMessageSize());
                        MulticastMetadata.Formatter.Serialize(w, value.MulticastMetadatas[i], rank + 1);
                    }
                }
            }
            public MulticastMetadatasResultPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                MulticastMetadata[] p_multicastMetadatas = Array.Empty<MulticastMetadata>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //MulticastMetadatas
                            {
                                var length = (long)r.GetUInt64();
                                p_multicastMetadatas = new MulticastMetadata[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_multicastMetadatas.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_multicastMetadatas[i] = MulticastMetadata.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new MulticastMetadatasResultPacket(p_multicastMetadatas);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UnicastMetadatasRequestPacket : MessageBase<UnicastMetadatasRequestPacket>
    {
        static UnicastMetadatasRequestPacket()
        {
            UnicastMetadatasRequestPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxSignaturesCount = 1024 * 256;
        [JsonConstructor]
        public UnicastMetadatasRequestPacket(IList<Signature> signatures)
        {
            if (signatures == null) throw new ArgumentNullException("signatures");
            if (signatures.Count > MaxSignaturesCount) throw new ArgumentOutOfRangeException("signatures");
            for (int i = 0; i < signatures.Count; i++)
            {
                if (signatures[i] == null) throw new ArgumentNullException("signatures[i]");
            }
            this.Signatures = new ReadOnlyCollection<Signature>(signatures);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<Signature> Signatures { get; }
        public override bool Equals(UnicastMetadatasRequestPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Signatures, target.Signatures)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Signatures.Count; i++)
                {
                    h ^= this.Signatures[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Signatures
            if (this.Signatures.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Signatures.Count);
                for (int i = 0; i < this.Signatures.Count; i++)
                {
                    var size_1 = this.Signatures[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<UnicastMetadatasRequestPacket>
        {
            public void Serialize(MessageStreamWriter w, UnicastMetadatasRequestPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Signatures
                if (value.Signatures.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Signatures.Count);
                    for (int i = 0; i < value.Signatures.Count; i++)
                    {
                        w.Write((ulong)value.Signatures[i].GetMessageSize());
                        Signature.Formatter.Serialize(w, value.Signatures[i], rank + 1);
                    }
                }
            }
            public UnicastMetadatasRequestPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Signature[] p_signatures = Array.Empty<Signature>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Signatures
                            {
                                var length = (long)r.GetUInt64();
                                p_signatures = new Signature[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_signatures.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_signatures[i] = Signature.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new UnicastMetadatasRequestPacket(p_signatures);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal sealed partial class UnicastMetadatasResultPacket : MessageBase<UnicastMetadatasResultPacket>
    {
        static UnicastMetadatasResultPacket()
        {
            UnicastMetadatasResultPacket.Formatter = new CustomFormatter();
        }
        public static readonly int MaxUnicastMetadatasCount = 1024 * 256;
        [JsonConstructor]
        public UnicastMetadatasResultPacket(IList<UnicastMetadata> unicastMetadatas)
        {
            if (unicastMetadatas == null) throw new ArgumentNullException("unicastMetadatas");
            if (unicastMetadatas.Count > MaxUnicastMetadatasCount) throw new ArgumentOutOfRangeException("unicastMetadatas");
            for (int i = 0; i < unicastMetadatas.Count; i++)
            {
                if (unicastMetadatas[i] == null) throw new ArgumentNullException("unicastMetadatas[i]");
            }
            this.UnicastMetadatas = new ReadOnlyCollection<UnicastMetadata>(unicastMetadatas);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<UnicastMetadata> UnicastMetadatas { get; }
        public override bool Equals(UnicastMetadatasResultPacket target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.UnicastMetadatas, target.UnicastMetadatas)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < UnicastMetadatas.Count; i++)
                {
                    h ^= this.UnicastMetadatas[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // UnicastMetadatas
            if (this.UnicastMetadatas.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.UnicastMetadatas.Count);
                for (int i = 0; i < this.UnicastMetadatas.Count; i++)
                {
                    var size_1 = this.UnicastMetadatas[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)size_1);
                    s += size_1;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<UnicastMetadatasResultPacket>
        {
            public void Serialize(MessageStreamWriter w, UnicastMetadatasResultPacket value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // UnicastMetadatas
                if (value.UnicastMetadatas.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.UnicastMetadatas.Count);
                    for (int i = 0; i < value.UnicastMetadatas.Count; i++)
                    {
                        w.Write((ulong)value.UnicastMetadatas[i].GetMessageSize());
                        UnicastMetadata.Formatter.Serialize(w, value.UnicastMetadatas[i], rank + 1);
                    }
                }
            }
            public UnicastMetadatasResultPacket Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                UnicastMetadata[] p_unicastMetadatas = Array.Empty<UnicastMetadata>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //UnicastMetadatas
                            {
                                var length = (long)r.GetUInt64();
                                p_unicastMetadatas = new UnicastMetadata[Math.Min(length, 1024 * 256)];
                                for (int i = 0; i < p_unicastMetadatas.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_unicastMetadatas[i] = UnicastMetadata.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new UnicastMetadatasResultPacket(p_unicastMetadatas);
            }
        }
    }

}
