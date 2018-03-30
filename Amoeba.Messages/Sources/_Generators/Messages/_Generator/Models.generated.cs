using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using Omnius.Serialization;
using Omnius.Utils;
using Omnius.Base;
using System.IO;
using System.Runtime.Serialization;
using Omnius.Security;
using System.Collections.ObjectModel;

namespace Amoeba.Messages
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class ConnectionFilter : MessageBase<ConnectionFilter>
    {
        static ConnectionFilter()
        {
            ConnectionFilter.Formatter = new CustomFormatter();
        }
        public static readonly int MaxSchemeLength = 256;
        public static readonly int MaxProxyUriLength = 256;
        [JsonConstructor]
        public ConnectionFilter(string scheme, ConnectionType type, string proxyUri)
        {
            if (scheme == null) throw new ArgumentNullException("scheme");
            if (scheme.Length > MaxSchemeLength) throw new ArgumentOutOfRangeException("scheme");
            if (proxyUri != null && proxyUri.Length > MaxProxyUriLength) throw new ArgumentOutOfRangeException("proxyUri");
            this.Scheme = scheme;
            this.Type = type;
            this.ProxyUri = proxyUri;
            this.Initialize();
        }
        [JsonProperty]
        public string Scheme { get; }
        [JsonProperty]
        public ConnectionType Type { get; }
        [JsonProperty]
        public string ProxyUri { get; }
        public override bool Equals(ConnectionFilter target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Scheme != target.Scheme) return false;
            if (this.Type != target.Type) return false;
            if (this.ProxyUri != target.ProxyUri) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Scheme != default(string)) h ^= this.Scheme.GetHashCode();
                if (this.Type != default(ConnectionType)) h ^= this.Type.GetHashCode();
                if (this.ProxyUri != default(string)) h ^= this.ProxyUri.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Scheme
            if (this.Scheme != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Scheme);
            }
            // Type
            if (this.Type != default(ConnectionType))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize((ulong)this.Type);
            }
            // ProxyUri
            if (this.ProxyUri != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                s += MessageSizeComputer.GetSize(this.ProxyUri);
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<ConnectionFilter>
        {
            public void Serialize(MessageStreamWriter w, ConnectionFilter value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Scheme
                if (value.Scheme != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Scheme);
                }
                // Type
                if (value.Type != default(ConnectionType))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Type);
                }
                // ProxyUri
                if (value.ProxyUri != default(string))
                {
                    w.Write((ulong)2);
                    w.Write(value.ProxyUri);
                }
            }
            public ConnectionFilter Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_scheme = default(string);
                ConnectionType p_type = default(ConnectionType);
                string p_proxyUri = default(string);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Scheme
                            {
                                p_scheme = r.GetString(MaxSchemeLength);
                                break;
                            }
                        case 1: //Type
                            {
                                p_type = (ConnectionType)r.GetUInt64();
                                break;
                            }
                        case 2: //ProxyUri
                            {
                                p_proxyUri = r.GetString(MaxProxyUriLength);
                                break;
                            }
                    }
                }
                return new ConnectionFilter(p_scheme, p_type, p_proxyUri);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class Location : MessageBase<Location>
    {
        static Location()
        {
            Location.Formatter = new CustomFormatter();
        }
        public static readonly int MaxUrisCount = 32;
        [JsonConstructor]
        public Location(IList<string> uris)
        {
            if (uris == null) throw new ArgumentNullException("uris");
            if (uris.Count > MaxUrisCount) throw new ArgumentOutOfRangeException("uris");
            for (int i = 0; i < uris.Count; i++)
            {
                if (uris[i] == null) throw new ArgumentNullException("uris[i]");
                if (uris[i].Length > 256) throw new ArgumentOutOfRangeException("uris[i]");
            }
            this.Uris = new ReadOnlyCollection<string>(uris);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<string> Uris { get; }
        public override bool Equals(Location target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Uris, target.Uris)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Uris.Count; i++)
                {
                    h ^= this.Uris[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Uris
            if (this.Uris.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Uris.Count);
                for (int i = 0; i < this.Uris.Count; i++)
                {
                    s += MessageSizeComputer.GetSize(this.Uris[i]);
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<Location>
        {
            public void Serialize(MessageStreamWriter w, Location value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Uris
                if (value.Uris.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Uris.Count);
                    for (int i = 0; i < value.Uris.Count; i++)
                    {
                        w.Write(value.Uris[i]);
                    }
                }
            }
            public Location Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string[] p_uris = Array.Empty<string>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Uris
                            {
                                var length = (long)r.GetUInt64();
                                p_uris = new string[Math.Min(length, MaxUrisCount)];
                                for (int i = 0; i < p_uris.Length; i++)
                                {
                                    p_uris[i] = r.GetString(256);
                                }
                                break;
                            }
                    }
                }
                return new Location(p_uris);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class Metadata : MessageBase<Metadata>
    {
        static Metadata()
        {
            Metadata.Formatter = new CustomFormatter();
        }
        [JsonConstructor]
        public Metadata(int depth, Hash hash)
        {
            if (hash == null) throw new ArgumentNullException("hash");
            this.Depth = depth;
            this.Hash = hash;
            this.Initialize();
        }
        [JsonProperty]
        public int Depth { get; }
        [JsonProperty]
        public Hash Hash { get; }
        public override bool Equals(Metadata target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Depth != target.Depth) return false;
            if (this.Hash != target.Hash) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Depth != default(int)) h ^= this.Depth.GetHashCode();
                if (this.Hash != default(Hash)) h ^= this.Hash.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Depth
            if (this.Depth != default(int))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Depth);
            }
            // Hash
            if (this.Hash != default(Hash))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                var size = this.Hash.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<Metadata>
        {
            public void Serialize(MessageStreamWriter w, Metadata value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Depth
                if (value.Depth != default(int))
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Depth);
                }
                // Hash
                if (value.Hash != default(Hash))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Hash.GetMessageSize());
                    Hash.Formatter.Serialize(w, value.Hash, rank + 1);
                }
            }
            public Metadata Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                int p_depth = default(int);
                Hash p_hash = default(Hash);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Depth
                            {
                                p_depth = (int)r.GetUInt64();
                                break;
                            }
                        case 1: //Hash
                            {
                                var size = (long)r.GetUInt64();
                                p_hash = Hash.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new Metadata(p_depth, p_hash);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class Tag : MessageBase<Tag>
    {
        static Tag()
        {
            Tag.Formatter = new CustomFormatter();
        }
        public static readonly int MaxNameLength = 256;
        public static readonly int MaxIdLength = 32;
        [JsonConstructor]
        public Tag(string name, byte[] id)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (id == null) throw new ArgumentNullException("id");
            if (name.Length > MaxNameLength) throw new ArgumentOutOfRangeException("name");
            if (id.Length > MaxIdLength) throw new ArgumentOutOfRangeException("id");
            this.Name = name;
            this.Id = id;
            this.Initialize();
        }
        [JsonProperty]
        public string Name { get; }
        [JsonProperty]
        public byte[] Id { get; }
        public override bool Equals(Tag target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Name != target.Name) return false;
            if ((this.Id == null) != (target.Id == null)) return false;
            if ((this.Id != null && target.Id != null)
                && !Unsafe.Equals(this.Id, target.Id)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Name != default(string)) h ^= this.Name.GetHashCode();
                if (this.Id != default(byte[])) h ^= MessageUtils.GetHashCode(this.Id);
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Name
            if (this.Name != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Name);
            }
            // Id
            if (this.Id != default(byte[]))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize(this.Id);
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<Tag>
        {
            public void Serialize(MessageStreamWriter w, Tag value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Name
                if (value.Name != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Name);
                }
                // Id
                if (value.Id != default(byte[]))
                {
                    w.Write((ulong)1);
                    w.Write(value.Id);
                }
            }
            public Tag Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_name = default(string);
                byte[] p_id = default(byte[]);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Name
                            {
                                p_name = r.GetString(MaxNameLength);
                                break;
                            }
                        case 1: //Id
                            {
                                p_id = r.GetBytes(MaxIdLength);
                                break;
                            }
                    }
                }
                return new Tag(p_name, p_id);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class ProfileContent : MessageBase<ProfileContent>
    {
        static ProfileContent()
        {
            ProfileContent.Formatter = new CustomFormatter();
        }
        public static readonly int MaxCommentLength = 8192;
        public static readonly int MaxTrustSignaturesCount = 1024;
        public static readonly int MaxDeleteSignaturesCount = 1024;
        public static readonly int MaxTagsCount = 1024;
        [JsonConstructor]
        public ProfileContent(string comment, ExchangePublicKey exchangePublicKey, IList<Signature> trustSignatures, IList<Signature> deleteSignatures, IList<Tag> tags, AgreementPublicKey agreementPublicKey)
        {
            if (trustSignatures == null) throw new ArgumentNullException("trustSignatures");
            if (deleteSignatures == null) throw new ArgumentNullException("deleteSignatures");
            if (tags == null) throw new ArgumentNullException("tags");
            if (comment != null && comment.Length > MaxCommentLength) throw new ArgumentOutOfRangeException("comment");
            if (trustSignatures.Count > MaxTrustSignaturesCount) throw new ArgumentOutOfRangeException("trustSignatures");
            if (deleteSignatures.Count > MaxDeleteSignaturesCount) throw new ArgumentOutOfRangeException("deleteSignatures");
            if (tags.Count > MaxTagsCount) throw new ArgumentOutOfRangeException("tags");
            for (int i = 0; i < trustSignatures.Count; i++)
            {
                if (trustSignatures[i] == null) throw new ArgumentNullException("trustSignatures[i]");
            }
            for (int i = 0; i < deleteSignatures.Count; i++)
            {
                if (deleteSignatures[i] == null) throw new ArgumentNullException("deleteSignatures[i]");
            }
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == null) throw new ArgumentNullException("tags[i]");
            }
            this.Comment = comment;
            this.ExchangePublicKey = exchangePublicKey;
            this.TrustSignatures = new ReadOnlyCollection<Signature>(trustSignatures);
            this.DeleteSignatures = new ReadOnlyCollection<Signature>(deleteSignatures);
            this.Tags = new ReadOnlyCollection<Tag>(tags);
            this.AgreementPublicKey = agreementPublicKey;
            this.Initialize();
        }
        [JsonProperty]
        public string Comment { get; }
        [Obsolete]
        [JsonProperty]
        public ExchangePublicKey ExchangePublicKey { get; }
        [JsonProperty]
        public IReadOnlyList<Signature> TrustSignatures { get; }
        [JsonProperty]
        public IReadOnlyList<Signature> DeleteSignatures { get; }
        [JsonProperty]
        public IReadOnlyList<Tag> Tags { get; }
        [JsonProperty]
        public AgreementPublicKey AgreementPublicKey { get; }
        public override bool Equals(ProfileContent target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Comment != target.Comment) return false;
            if (this.ExchangePublicKey != target.ExchangePublicKey) return false;
            if (!CollectionUtils.Equals(this.TrustSignatures, target.TrustSignatures)) return false;
            if (!CollectionUtils.Equals(this.DeleteSignatures, target.DeleteSignatures)) return false;
            if (!CollectionUtils.Equals(this.Tags, target.Tags)) return false;
            if (this.AgreementPublicKey != target.AgreementPublicKey) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Comment != default(string)) h ^= this.Comment.GetHashCode();
                if (this.ExchangePublicKey != default(ExchangePublicKey)) h ^= this.ExchangePublicKey.GetHashCode();
                for (int i = 0; i < TrustSignatures.Count; i++)
                {
                    h ^= this.TrustSignatures[i].GetHashCode();
                }
                for (int i = 0; i < DeleteSignatures.Count; i++)
                {
                    h ^= this.DeleteSignatures[i].GetHashCode();
                }
                for (int i = 0; i < Tags.Count; i++)
                {
                    h ^= this.Tags[i].GetHashCode();
                }
                if (this.AgreementPublicKey != default(AgreementPublicKey)) h ^= this.AgreementPublicKey.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Comment
            if (this.Comment != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Comment);
            }
            // ExchangePublicKey
            if (this.ExchangePublicKey != default(ExchangePublicKey))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                var size = this.ExchangePublicKey.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // TrustSignatures
            if (this.TrustSignatures.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                s += MessageSizeComputer.GetSize((ulong)this.TrustSignatures.Count);
                for (int i = 0; i < this.TrustSignatures.Count; i++)
                {
                    var element_size = this.TrustSignatures[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)element_size);
                    s += element_size;
                }
            }
            // DeleteSignatures
            if (this.DeleteSignatures.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)3);
                s += MessageSizeComputer.GetSize((ulong)this.DeleteSignatures.Count);
                for (int i = 0; i < this.DeleteSignatures.Count; i++)
                {
                    var element_size = this.DeleteSignatures[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)element_size);
                    s += element_size;
                }
            }
            // Tags
            if (this.Tags.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)4);
                s += MessageSizeComputer.GetSize((ulong)this.Tags.Count);
                for (int i = 0; i < this.Tags.Count; i++)
                {
                    var element_size = this.Tags[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)element_size);
                    s += element_size;
                }
            }
            // AgreementPublicKey
            if (this.AgreementPublicKey != default(AgreementPublicKey))
            {
                s += MessageSizeComputer.GetSize((ulong)5);
                var size = this.AgreementPublicKey.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<ProfileContent>
        {
            public void Serialize(MessageStreamWriter w, ProfileContent value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Comment
                if (value.Comment != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Comment);
                }
                // ExchangePublicKey
                if (value.ExchangePublicKey != default(ExchangePublicKey))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.ExchangePublicKey.GetMessageSize());
                    ExchangePublicKey.Formatter.Serialize(w, value.ExchangePublicKey, rank + 1);
                }
                // TrustSignatures
                if (value.TrustSignatures.Count != 0)
                {
                    w.Write((ulong)2);
                    w.Write((ulong)value.TrustSignatures.Count);
                    for (int i = 0; i < value.TrustSignatures.Count; i++)
                    {
                        w.Write((ulong)value.TrustSignatures[i].GetMessageSize());
                        Signature.Formatter.Serialize(w, value.TrustSignatures[i], rank + 1);
                    }
                }
                // DeleteSignatures
                if (value.DeleteSignatures.Count != 0)
                {
                    w.Write((ulong)3);
                    w.Write((ulong)value.DeleteSignatures.Count);
                    for (int i = 0; i < value.DeleteSignatures.Count; i++)
                    {
                        w.Write((ulong)value.DeleteSignatures[i].GetMessageSize());
                        Signature.Formatter.Serialize(w, value.DeleteSignatures[i], rank + 1);
                    }
                }
                // Tags
                if (value.Tags.Count != 0)
                {
                    w.Write((ulong)4);
                    w.Write((ulong)value.Tags.Count);
                    for (int i = 0; i < value.Tags.Count; i++)
                    {
                        w.Write((ulong)value.Tags[i].GetMessageSize());
                        Tag.Formatter.Serialize(w, value.Tags[i], rank + 1);
                    }
                }
                // AgreementPublicKey
                if (value.AgreementPublicKey != default(AgreementPublicKey))
                {
                    w.Write((ulong)5);
                    w.Write((ulong)value.AgreementPublicKey.GetMessageSize());
                    AgreementPublicKey.Formatter.Serialize(w, value.AgreementPublicKey, rank + 1);
                }
            }
            public ProfileContent Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_comment = default(string);
                ExchangePublicKey p_exchangePublicKey = default(ExchangePublicKey);
                Signature[] p_trustSignatures = Array.Empty<Signature>();
                Signature[] p_deleteSignatures = Array.Empty<Signature>();
                Tag[] p_tags = Array.Empty<Tag>();
                AgreementPublicKey p_agreementPublicKey = default(AgreementPublicKey);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Comment
                            {
                                p_comment = r.GetString(MaxCommentLength);
                                break;
                            }
                        case 1: //ExchangePublicKey
                            {
                                var size = (long)r.GetUInt64();
                                p_exchangePublicKey = ExchangePublicKey.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 2: //TrustSignatures
                            {
                                var length = (long)r.GetUInt64();
                                p_trustSignatures = new Signature[Math.Min(length, MaxTrustSignaturesCount)];
                                for (int i = 0; i < p_trustSignatures.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_trustSignatures[i] = Signature.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                        case 3: //DeleteSignatures
                            {
                                var length = (long)r.GetUInt64();
                                p_deleteSignatures = new Signature[Math.Min(length, MaxDeleteSignaturesCount)];
                                for (int i = 0; i < p_deleteSignatures.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_deleteSignatures[i] = Signature.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                        case 4: //Tags
                            {
                                var length = (long)r.GetUInt64();
                                p_tags = new Tag[Math.Min(length, MaxTagsCount)];
                                for (int i = 0; i < p_tags.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_tags[i] = Tag.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                        case 5: //AgreementPublicKey
                            {
                                var size = (long)r.GetUInt64();
                                p_agreementPublicKey = AgreementPublicKey.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new ProfileContent(p_comment, p_exchangePublicKey, p_trustSignatures, p_deleteSignatures, p_tags, p_agreementPublicKey);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class CommentContent : MessageBase<CommentContent>
    {
        static CommentContent()
        {
            CommentContent.Formatter = new CustomFormatter();
        }
        public static readonly int MaxCommentLength = 8192;
        [JsonConstructor]
        public CommentContent(string comment)
        {
            if (comment == null) throw new ArgumentNullException("comment");
            if (comment.Length > MaxCommentLength) throw new ArgumentOutOfRangeException("comment");
            this.Comment = comment;
            this.Initialize();
        }
        [JsonProperty]
        public string Comment { get; }
        public override bool Equals(CommentContent target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Comment != target.Comment) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Comment != default(string)) h ^= this.Comment.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Comment
            if (this.Comment != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Comment);
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<CommentContent>
        {
            public void Serialize(MessageStreamWriter w, CommentContent value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Comment
                if (value.Comment != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Comment);
                }
            }
            public CommentContent Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_comment = default(string);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Comment
                            {
                                p_comment = r.GetString(MaxCommentLength);
                                break;
                            }
                    }
                }
                return new CommentContent(p_comment);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class StoreContent : MessageBase<StoreContent>
    {
        static StoreContent()
        {
            StoreContent.Formatter = new CustomFormatter();
        }
        public static readonly int MaxBoxesCount = 1024 * 8;
        [JsonConstructor]
        public StoreContent(IList<Box> boxes)
        {
            if (boxes == null) throw new ArgumentNullException("boxes");
            if (boxes.Count > MaxBoxesCount) throw new ArgumentOutOfRangeException("boxes");
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] == null) throw new ArgumentNullException("boxes[i]");
            }
            this.Boxes = new ReadOnlyCollection<Box>(boxes);
            this.Initialize();
        }
        [JsonProperty]
        public IReadOnlyList<Box> Boxes { get; }
        public override bool Equals(StoreContent target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (!CollectionUtils.Equals(this.Boxes, target.Boxes)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                for (int i = 0; i < Boxes.Count; i++)
                {
                    h ^= this.Boxes[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Boxes
            if (this.Boxes.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize((ulong)this.Boxes.Count);
                for (int i = 0; i < this.Boxes.Count; i++)
                {
                    var element_size = this.Boxes[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)element_size);
                    s += element_size;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<StoreContent>
        {
            public void Serialize(MessageStreamWriter w, StoreContent value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Boxes
                if (value.Boxes.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Boxes.Count);
                    for (int i = 0; i < value.Boxes.Count; i++)
                    {
                        w.Write((ulong)value.Boxes[i].GetMessageSize());
                        Box.Formatter.Serialize(w, value.Boxes[i], rank + 1);
                    }
                }
            }
            public StoreContent Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Box[] p_boxes = Array.Empty<Box>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Boxes
                            {
                                var length = (long)r.GetUInt64();
                                p_boxes = new Box[Math.Min(length, MaxBoxesCount)];
                                for (int i = 0; i < p_boxes.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_boxes[i] = Box.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new StoreContent(p_boxes);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class Box : MessageBase<Box>
    {
        static Box()
        {
            Box.Formatter = new CustomFormatter();
        }
        public static readonly int MaxNameLength = 256;
        public static readonly int MaxSeedsCount = 1024 * 64;
        public static readonly int MaxBoxesCount = 1024 * 8;
        [JsonConstructor]
        public Box(string name, IList<Seed> seeds, IList<Box> boxes)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (seeds == null) throw new ArgumentNullException("seeds");
            if (boxes == null) throw new ArgumentNullException("boxes");
            if (name.Length > MaxNameLength) throw new ArgumentOutOfRangeException("name");
            if (seeds.Count > MaxSeedsCount) throw new ArgumentOutOfRangeException("seeds");
            if (boxes.Count > MaxBoxesCount) throw new ArgumentOutOfRangeException("boxes");
            for (int i = 0; i < seeds.Count; i++)
            {
                if (seeds[i] == null) throw new ArgumentNullException("seeds[i]");
            }
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] == null) throw new ArgumentNullException("boxes[i]");
            }
            this.Name = name;
            this.Seeds = new ReadOnlyCollection<Seed>(seeds);
            this.Boxes = new ReadOnlyCollection<Box>(boxes);
            this.Initialize();
        }
        [JsonProperty]
        public string Name { get; }
        [JsonProperty]
        public IReadOnlyList<Seed> Seeds { get; }
        [JsonProperty]
        public IReadOnlyList<Box> Boxes { get; }
        public override bool Equals(Box target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Name != target.Name) return false;
            if (!CollectionUtils.Equals(this.Seeds, target.Seeds)) return false;
            if (!CollectionUtils.Equals(this.Boxes, target.Boxes)) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Name != default(string)) h ^= this.Name.GetHashCode();
                for (int i = 0; i < Seeds.Count; i++)
                {
                    h ^= this.Seeds[i].GetHashCode();
                }
                for (int i = 0; i < Boxes.Count; i++)
                {
                    h ^= this.Boxes[i].GetHashCode();
                }
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Name
            if (this.Name != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Name);
            }
            // Seeds
            if (this.Seeds.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize((ulong)this.Seeds.Count);
                for (int i = 0; i < this.Seeds.Count; i++)
                {
                    var element_size = this.Seeds[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)element_size);
                    s += element_size;
                }
            }
            // Boxes
            if (this.Boxes.Count != 0)
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                s += MessageSizeComputer.GetSize((ulong)this.Boxes.Count);
                for (int i = 0; i < this.Boxes.Count; i++)
                {
                    var element_size = this.Boxes[i].GetMessageSize();
                    s += MessageSizeComputer.GetSize((ulong)element_size);
                    s += element_size;
                }
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<Box>
        {
            public void Serialize(MessageStreamWriter w, Box value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Name
                if (value.Name != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Name);
                }
                // Seeds
                if (value.Seeds.Count != 0)
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Seeds.Count);
                    for (int i = 0; i < value.Seeds.Count; i++)
                    {
                        w.Write((ulong)value.Seeds[i].GetMessageSize());
                        Seed.Formatter.Serialize(w, value.Seeds[i], rank + 1);
                    }
                }
                // Boxes
                if (value.Boxes.Count != 0)
                {
                    w.Write((ulong)2);
                    w.Write((ulong)value.Boxes.Count);
                    for (int i = 0; i < value.Boxes.Count; i++)
                    {
                        w.Write((ulong)value.Boxes[i].GetMessageSize());
                        Box.Formatter.Serialize(w, value.Boxes[i], rank + 1);
                    }
                }
            }
            public Box Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_name = default(string);
                Seed[] p_seeds = Array.Empty<Seed>();
                Box[] p_boxes = Array.Empty<Box>();
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Name
                            {
                                p_name = r.GetString(MaxNameLength);
                                break;
                            }
                        case 1: //Seeds
                            {
                                var length = (long)r.GetUInt64();
                                p_seeds = new Seed[Math.Min(length, MaxSeedsCount)];
                                for (int i = 0; i < p_seeds.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_seeds[i] = Seed.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                        case 2: //Boxes
                            {
                                var length = (long)r.GetUInt64();
                                p_boxes = new Box[Math.Min(length, MaxBoxesCount)];
                                for (int i = 0; i < p_boxes.Length; i++)
                                {
                                    var element_size = (long)r.GetUInt64();
                                    p_boxes[i] = Box.Formatter.Deserialize(r.GetRange(element_size), rank + 1);
                                }
                                break;
                            }
                    }
                }
                return new Box(p_name, p_seeds, p_boxes);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class Seed : MessageBase<Seed>
    {
        static Seed()
        {
            Seed.Formatter = new CustomFormatter();
        }
        public static readonly int MaxNameLength = 256;
        [JsonConstructor]
        public Seed(string name, long length, DateTime creationTime, Metadata metadata)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (metadata == null) throw new ArgumentNullException("metadata");
            if (name.Length > MaxNameLength) throw new ArgumentOutOfRangeException("name");
            this.Name = name;
            this.Length = length;
            this.CreationTime = creationTime.Trim();
            this.Metadata = metadata;
            this.Initialize();
        }
        [JsonProperty]
        public string Name { get; }
        [JsonProperty]
        public long Length { get; }
        [JsonProperty]
        public DateTime CreationTime { get; }
        [JsonProperty]
        public Metadata Metadata { get; }
        public override bool Equals(Seed target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Name != target.Name) return false;
            if (this.Length != target.Length) return false;
            if (this.CreationTime != target.CreationTime) return false;
            if (this.Metadata != target.Metadata) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Name != default(string)) h ^= this.Name.GetHashCode();
                if (this.Length != default(long)) h ^= this.Length.GetHashCode();
                if (this.CreationTime != default(DateTime)) h ^= this.CreationTime.GetHashCode();
                if (this.Metadata != default(Metadata)) h ^= this.Metadata.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Name
            if (this.Name != default(string))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                s += MessageSizeComputer.GetSize(this.Name);
            }
            // Length
            if (this.Length != default(long))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize((ulong)this.Length);
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
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<Seed>
        {
            public void Serialize(MessageStreamWriter w, Seed value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Name
                if (value.Name != default(string))
                {
                    w.Write((ulong)0);
                    w.Write(value.Name);
                }
                // Length
                if (value.Length != default(long))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Length);
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
            }
            public Seed Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                string p_name = default(string);
                long p_length = default(long);
                DateTime p_creationTime = default(DateTime);
                Metadata p_metadata = default(Metadata);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Name
                            {
                                p_name = r.GetString(MaxNameLength);
                                break;
                            }
                        case 1: //Length
                            {
                                p_length = (long)r.GetUInt64();
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
                    }
                }
                return new Seed(p_name, p_length, p_creationTime, p_metadata);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class BroadcastProfileMessage : MessageBase<BroadcastProfileMessage>
    {
        static BroadcastProfileMessage()
        {
            BroadcastProfileMessage.Formatter = new CustomFormatter();
        }
        [JsonConstructor]
        public BroadcastProfileMessage(Signature authorSignature, DateTime creationTime, ProfileContent value)
        {
            if (authorSignature == null) throw new ArgumentNullException("authorSignature");
            if (value == null) throw new ArgumentNullException("value");
            this.AuthorSignature = authorSignature;
            this.CreationTime = creationTime.Trim();
            this.Value = value;
            this.Initialize();
        }
        [JsonProperty]
        public Signature AuthorSignature { get; }
        [JsonProperty]
        public DateTime CreationTime { get; }
        [JsonProperty]
        public ProfileContent Value { get; }
        public override bool Equals(BroadcastProfileMessage target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.AuthorSignature != target.AuthorSignature) return false;
            if (this.CreationTime != target.CreationTime) return false;
            if (this.Value != target.Value) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.AuthorSignature != default(Signature)) h ^= this.AuthorSignature.GetHashCode();
                if (this.CreationTime != default(DateTime)) h ^= this.CreationTime.GetHashCode();
                if (this.Value != default(ProfileContent)) h ^= this.Value.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // AuthorSignature
            if (this.AuthorSignature != default(Signature))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                var size = this.AuthorSignature.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // CreationTime
            if (this.CreationTime != default(DateTime))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize(this.CreationTime);
            }
            // Value
            if (this.Value != default(ProfileContent))
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                var size = this.Value.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<BroadcastProfileMessage>
        {
            public void Serialize(MessageStreamWriter w, BroadcastProfileMessage value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // AuthorSignature
                if (value.AuthorSignature != default(Signature))
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.AuthorSignature.GetMessageSize());
                    Signature.Formatter.Serialize(w, value.AuthorSignature, rank + 1);
                }
                // CreationTime
                if (value.CreationTime != default(DateTime))
                {
                    w.Write((ulong)1);
                    w.Write(value.CreationTime);
                }
                // Value
                if (value.Value != default(ProfileContent))
                {
                    w.Write((ulong)2);
                    w.Write((ulong)value.Value.GetMessageSize());
                    ProfileContent.Formatter.Serialize(w, value.Value, rank + 1);
                }
            }
            public BroadcastProfileMessage Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Signature p_authorSignature = default(Signature);
                DateTime p_creationTime = default(DateTime);
                ProfileContent p_value = default(ProfileContent);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //AuthorSignature
                            {
                                var size = (long)r.GetUInt64();
                                p_authorSignature = Signature.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 1: //CreationTime
                            {
                                p_creationTime = r.GetDateTime();
                                break;
                            }
                        case 2: //Value
                            {
                                var size = (long)r.GetUInt64();
                                p_value = ProfileContent.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new BroadcastProfileMessage(p_authorSignature, p_creationTime, p_value);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class BroadcastStoreMessage : MessageBase<BroadcastStoreMessage>
    {
        static BroadcastStoreMessage()
        {
            BroadcastStoreMessage.Formatter = new CustomFormatter();
        }
        [JsonConstructor]
        public BroadcastStoreMessage(Signature authorSignature, DateTime creationTime, StoreContent value)
        {
            if (authorSignature == null) throw new ArgumentNullException("authorSignature");
            if (value == null) throw new ArgumentNullException("value");
            this.AuthorSignature = authorSignature;
            this.CreationTime = creationTime.Trim();
            this.Value = value;
            this.Initialize();
        }
        [JsonProperty]
        public Signature AuthorSignature { get; }
        [JsonProperty]
        public DateTime CreationTime { get; }
        [JsonProperty]
        public StoreContent Value { get; }
        public override bool Equals(BroadcastStoreMessage target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.AuthorSignature != target.AuthorSignature) return false;
            if (this.CreationTime != target.CreationTime) return false;
            if (this.Value != target.Value) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.AuthorSignature != default(Signature)) h ^= this.AuthorSignature.GetHashCode();
                if (this.CreationTime != default(DateTime)) h ^= this.CreationTime.GetHashCode();
                if (this.Value != default(StoreContent)) h ^= this.Value.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // AuthorSignature
            if (this.AuthorSignature != default(Signature))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                var size = this.AuthorSignature.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // CreationTime
            if (this.CreationTime != default(DateTime))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                s += MessageSizeComputer.GetSize(this.CreationTime);
            }
            // Value
            if (this.Value != default(StoreContent))
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                var size = this.Value.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<BroadcastStoreMessage>
        {
            public void Serialize(MessageStreamWriter w, BroadcastStoreMessage value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // AuthorSignature
                if (value.AuthorSignature != default(Signature))
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.AuthorSignature.GetMessageSize());
                    Signature.Formatter.Serialize(w, value.AuthorSignature, rank + 1);
                }
                // CreationTime
                if (value.CreationTime != default(DateTime))
                {
                    w.Write((ulong)1);
                    w.Write(value.CreationTime);
                }
                // Value
                if (value.Value != default(StoreContent))
                {
                    w.Write((ulong)2);
                    w.Write((ulong)value.Value.GetMessageSize());
                    StoreContent.Formatter.Serialize(w, value.Value, rank + 1);
                }
            }
            public BroadcastStoreMessage Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Signature p_authorSignature = default(Signature);
                DateTime p_creationTime = default(DateTime);
                StoreContent p_value = default(StoreContent);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //AuthorSignature
                            {
                                var size = (long)r.GetUInt64();
                                p_authorSignature = Signature.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 1: //CreationTime
                            {
                                p_creationTime = r.GetDateTime();
                                break;
                            }
                        case 2: //Value
                            {
                                var size = (long)r.GetUInt64();
                                p_value = StoreContent.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new BroadcastStoreMessage(p_authorSignature, p_creationTime, p_value);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class MulticastCommentMessage : MessageBase<MulticastCommentMessage>
    {
        static MulticastCommentMessage()
        {
            MulticastCommentMessage.Formatter = new CustomFormatter();
        }
        [JsonConstructor]
        public MulticastCommentMessage(Tag tag, Signature authorSignature, DateTime creationTime, Cost cost, CommentContent value)
        {
            if (tag == null) throw new ArgumentNullException("tag");
            if (authorSignature == null) throw new ArgumentNullException("authorSignature");
            if (cost == null) throw new ArgumentNullException("cost");
            if (value == null) throw new ArgumentNullException("value");
            this.Tag = tag;
            this.AuthorSignature = authorSignature;
            this.CreationTime = creationTime.Trim();
            this.Cost = cost;
            this.Value = value;
            this.Initialize();
        }
        [JsonProperty]
        public Tag Tag { get; }
        [JsonProperty]
        public Signature AuthorSignature { get; }
        [JsonProperty]
        public DateTime CreationTime { get; }
        [JsonProperty]
        public Cost Cost { get; }
        [JsonProperty]
        public CommentContent Value { get; }
        public override bool Equals(MulticastCommentMessage target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Tag != target.Tag) return false;
            if (this.AuthorSignature != target.AuthorSignature) return false;
            if (this.CreationTime != target.CreationTime) return false;
            if (this.Cost != target.Cost) return false;
            if (this.Value != target.Value) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.Tag != default(Tag)) h ^= this.Tag.GetHashCode();
                if (this.AuthorSignature != default(Signature)) h ^= this.AuthorSignature.GetHashCode();
                if (this.CreationTime != default(DateTime)) h ^= this.CreationTime.GetHashCode();
                if (this.Cost != default(Cost)) h ^= this.Cost.GetHashCode();
                if (this.Value != default(CommentContent)) h ^= this.Value.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // Tag
            if (this.Tag != default(Tag))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                var size = this.Tag.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // AuthorSignature
            if (this.AuthorSignature != default(Signature))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                var size = this.AuthorSignature.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // CreationTime
            if (this.CreationTime != default(DateTime))
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                s += MessageSizeComputer.GetSize(this.CreationTime);
            }
            // Cost
            if (this.Cost != default(Cost))
            {
                s += MessageSizeComputer.GetSize((ulong)3);
                var size = this.Cost.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // Value
            if (this.Value != default(CommentContent))
            {
                s += MessageSizeComputer.GetSize((ulong)4);
                var size = this.Value.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<MulticastCommentMessage>
        {
            public void Serialize(MessageStreamWriter w, MulticastCommentMessage value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // Tag
                if (value.Tag != default(Tag))
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Tag.GetMessageSize());
                    Tag.Formatter.Serialize(w, value.Tag, rank + 1);
                }
                // AuthorSignature
                if (value.AuthorSignature != default(Signature))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.AuthorSignature.GetMessageSize());
                    Signature.Formatter.Serialize(w, value.AuthorSignature, rank + 1);
                }
                // CreationTime
                if (value.CreationTime != default(DateTime))
                {
                    w.Write((ulong)2);
                    w.Write(value.CreationTime);
                }
                // Cost
                if (value.Cost != default(Cost))
                {
                    w.Write((ulong)3);
                    w.Write((ulong)value.Cost.GetMessageSize());
                    Cost.Formatter.Serialize(w, value.Cost, rank + 1);
                }
                // Value
                if (value.Value != default(CommentContent))
                {
                    w.Write((ulong)4);
                    w.Write((ulong)value.Value.GetMessageSize());
                    CommentContent.Formatter.Serialize(w, value.Value, rank + 1);
                }
            }
            public MulticastCommentMessage Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Tag p_tag = default(Tag);
                Signature p_authorSignature = default(Signature);
                DateTime p_creationTime = default(DateTime);
                Cost p_cost = default(Cost);
                CommentContent p_value = default(CommentContent);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //Tag
                            {
                                var size = (long)r.GetUInt64();
                                p_tag = Tag.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 1: //AuthorSignature
                            {
                                var size = (long)r.GetUInt64();
                                p_authorSignature = Signature.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 2: //CreationTime
                            {
                                p_creationTime = r.GetDateTime();
                                break;
                            }
                        case 3: //Cost
                            {
                                var size = (long)r.GetUInt64();
                                p_cost = Cost.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 4: //Value
                            {
                                var size = (long)r.GetUInt64();
                                p_value = CommentContent.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new MulticastCommentMessage(p_tag, p_authorSignature, p_creationTime, p_cost, p_value);
            }
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed partial class UnicastCommentMessage : MessageBase<UnicastCommentMessage>
    {
        static UnicastCommentMessage()
        {
            UnicastCommentMessage.Formatter = new CustomFormatter();
        }
        [JsonConstructor]
        public UnicastCommentMessage(Signature targetSignature, Signature authorSignature, DateTime creationTime, CommentContent value)
        {
            if (targetSignature == null) throw new ArgumentNullException("targetSignature");
            if (authorSignature == null) throw new ArgumentNullException("authorSignature");
            if (value == null) throw new ArgumentNullException("value");
            this.TargetSignature = targetSignature;
            this.AuthorSignature = authorSignature;
            this.CreationTime = creationTime.Trim();
            this.Value = value;
            this.Initialize();
        }
        [JsonProperty]
        public Signature TargetSignature { get; }
        [JsonProperty]
        public Signature AuthorSignature { get; }
        [JsonProperty]
        public DateTime CreationTime { get; }
        [JsonProperty]
        public CommentContent Value { get; }
        public override bool Equals(UnicastCommentMessage target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.TargetSignature != target.TargetSignature) return false;
            if (this.AuthorSignature != target.AuthorSignature) return false;
            if (this.CreationTime != target.CreationTime) return false;
            if (this.Value != target.Value) return false;
            return true;
        }
        private int? _hashCode;
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                int h = 0;
                if (this.TargetSignature != default(Signature)) h ^= this.TargetSignature.GetHashCode();
                if (this.AuthorSignature != default(Signature)) h ^= this.AuthorSignature.GetHashCode();
                if (this.CreationTime != default(DateTime)) h ^= this.CreationTime.GetHashCode();
                if (this.Value != default(CommentContent)) h ^= this.Value.GetHashCode();
                _hashCode = h;
            }
            return _hashCode.Value;
        }
        public override long GetMessageSize()
        {
            long s = 0;
            // TargetSignature
            if (this.TargetSignature != default(Signature))
            {
                s += MessageSizeComputer.GetSize((ulong)0);
                var size = this.TargetSignature.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // AuthorSignature
            if (this.AuthorSignature != default(Signature))
            {
                s += MessageSizeComputer.GetSize((ulong)1);
                var size = this.AuthorSignature.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            // CreationTime
            if (this.CreationTime != default(DateTime))
            {
                s += MessageSizeComputer.GetSize((ulong)2);
                s += MessageSizeComputer.GetSize(this.CreationTime);
            }
            // Value
            if (this.Value != default(CommentContent))
            {
                s += MessageSizeComputer.GetSize((ulong)3);
                var size = this.Value.GetMessageSize();
                s += MessageSizeComputer.GetSize((ulong)size);
                s += size;
            }
            return s;
        }
        private sealed class CustomFormatter : IMessageFormatter<UnicastCommentMessage>
        {
            public void Serialize(MessageStreamWriter w, UnicastCommentMessage value, int rank)
            {
                if (rank > 256) throw new FormatException();
                // TargetSignature
                if (value.TargetSignature != default(Signature))
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.TargetSignature.GetMessageSize());
                    Signature.Formatter.Serialize(w, value.TargetSignature, rank + 1);
                }
                // AuthorSignature
                if (value.AuthorSignature != default(Signature))
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.AuthorSignature.GetMessageSize());
                    Signature.Formatter.Serialize(w, value.AuthorSignature, rank + 1);
                }
                // CreationTime
                if (value.CreationTime != default(DateTime))
                {
                    w.Write((ulong)2);
                    w.Write(value.CreationTime);
                }
                // Value
                if (value.Value != default(CommentContent))
                {
                    w.Write((ulong)3);
                    w.Write((ulong)value.Value.GetMessageSize());
                    CommentContent.Formatter.Serialize(w, value.Value, rank + 1);
                }
            }
            public UnicastCommentMessage Deserialize(MessageStreamReader r, int rank)
            {
                if (rank > 256) throw new FormatException();
                Signature p_targetSignature = default(Signature);
                Signature p_authorSignature = default(Signature);
                DateTime p_creationTime = default(DateTime);
                CommentContent p_value = default(CommentContent);
                while (r.Available > 0)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: //TargetSignature
                            {
                                var size = (long)r.GetUInt64();
                                p_targetSignature = Signature.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 1: //AuthorSignature
                            {
                                var size = (long)r.GetUInt64();
                                p_authorSignature = Signature.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                        case 2: //CreationTime
                            {
                                p_creationTime = r.GetDateTime();
                                break;
                            }
                        case 3: //Value
                            {
                                var size = (long)r.GetUInt64();
                                p_value = CommentContent.Formatter.Deserialize(r.GetRange(size), rank + 1);
                                break;
                            }
                    }
                }
                return new UnicastCommentMessage(p_targetSignature, p_authorSignature, p_creationTime, p_value);
            }
        }
    }

}
