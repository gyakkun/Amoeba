using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Library;
using Library.Collections;
using Library.Io;
using Library.Net.Amoeba;

namespace Amoeba.Windows
{
    [DataContract(Name = "SearchItem", Namespace = "http://Amoeba/Windows")]
    class SearchItem : ICloneable<SearchItem>, IThisLock
    {
        private string _name = "default";
        private LockedList<SearchContains<string>> _searchNameCollection;
        private LockedList<SearchContains<SearchRegex>> _searchNameRegexCollection;
        private LockedList<SearchContains<SearchRegex>> _searchSignatureCollection;
        private LockedList<SearchContains<string>> _searchKeywordCollection;
        private LockedList<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;
        private LockedList<SearchContains<SearchRange<long>>> _searchLengthRangeCollection;
        private LockedList<SearchContains<Seed>> _searchSeedCollection;
        private LockedList<SearchContains<SearchState>> _searchStateCollection;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _name;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _name = value;
                }
            }
        }

        [DataMember(Name = "SearchNameCollection")]
        public LockedList<SearchContains<string>> SearchNameCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchNameCollection == null)
                        _searchNameCollection = new LockedList<SearchContains<string>>();

                    return _searchNameCollection;
                }
            }
        }

        [DataMember(Name = "SearchNameRegexCollection")]
        public LockedList<SearchContains<SearchRegex>> SearchNameRegexCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchNameRegexCollection == null)
                        _searchNameRegexCollection = new LockedList<SearchContains<SearchRegex>>();

                    return _searchNameRegexCollection;
                }
            }
        }

        [DataMember(Name = "SearchSignatureCollection 2")]
        public LockedList<SearchContains<SearchRegex>> SearchSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSignatureCollection == null)
                        _searchSignatureCollection = new LockedList<SearchContains<SearchRegex>>();

                    return _searchSignatureCollection;
                }
            }
        }

        [DataMember(Name = "SearchKeywordCollection")]
        public LockedList<SearchContains<string>> SearchKeywordCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchKeywordCollection == null)
                        _searchKeywordCollection = new LockedList<SearchContains<string>>();

                    return _searchKeywordCollection;
                }
            }
        }

        [DataMember(Name = "SearchCreationTimeRangeCollection")]
        public LockedList<SearchContains<SearchRange<DateTime>>> SearchCreationTimeRangeCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchCreationTimeRangeCollection == null)
                        _searchCreationTimeRangeCollection = new LockedList<SearchContains<SearchRange<DateTime>>>();

                    return _searchCreationTimeRangeCollection;
                }
            }
        }

        [DataMember(Name = "SearchLengthRangeCollection")]
        public LockedList<SearchContains<SearchRange<long>>> SearchLengthRangeCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchLengthRangeCollection == null)
                        _searchLengthRangeCollection = new LockedList<SearchContains<SearchRange<long>>>();

                    return _searchLengthRangeCollection;
                }
            }
        }

        [DataMember(Name = "SearchSeedCollection")]
        public LockedList<SearchContains<Seed>> SearchSeedCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSeedCollection == null)
                        _searchSeedCollection = new LockedList<SearchContains<Seed>>();

                    return _searchSeedCollection;
                }
            }
        }

        [DataMember(Name = "SearchStateCollection")]
        public LockedList<SearchContains<SearchState>> SearchStateCollection
        {
            get
            {
                lock (this.ThisLock)
                {

                    if (_searchStateCollection == null)
                        _searchStateCollection = new LockedList<SearchContains<SearchState>>();

                    return _searchStateCollection;
                }
            }
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("Name = {0}", this.Name);
            }
        }

        #region ICloneable<SearchItem>

        public SearchItem Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchItem));

                using (BufferStream stream = new BufferStream(BufferManager.Instance))
                {
                    using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    stream.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchItem)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                if (_thisLock == null)
                {
                    lock (_initializeLock)
                    {
                        if (_thisLock == null)
                        {
                            _thisLock = new object();
                        }
                    }
                }

                return _thisLock;
            }
        }

        #endregion
    }

    [Flags]
    [DataContract(Name = "SearchState", Namespace = "http://Amoeba/Windows")]
    enum SearchState
    {
        [EnumMember(Value = "Cache")]
        Cache = 0x1,

        [EnumMember(Value = "Share")]
        Share = 0x2,

        [EnumMember(Value = "Uploading")]
        Uploading = 0x4,

        [EnumMember(Value = "Uploaded")]
        Uploaded = 0x8,

        [EnumMember(Value = "Downloading")]
        Downloading = 0x10,

        [EnumMember(Value = "Downloaded")]
        Downloaded = 0x20,

        [EnumMember(Value = "Box")]
        Box = 0x40,

        [EnumMember(Value = "Link")]
        Link = 0x80,
    }

    [DataContract(Name = "SearchContains", Namespace = "http://Amoeba/Windows")]
    class SearchContains<T> : IEquatable<SearchContains<T>>, ICloneable<SearchContains<T>>
    {
        private bool _contains;
        private T _value;

        [DataMember(Name = "Contains")]
        public bool Contains
        {
            get
            {
                return _contains;
            }
            set
            {
                _contains = value;
            }
        }

        [DataMember(Name = "Value")]
        public T Value
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
            return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchContains<T>)) return false;

            return this.Equals((SearchContains<T>)obj);
        }

        public bool Equals(SearchContains<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((this.Contains != other.Contains)
                || (!this.Value.Equals(other.Value)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.Contains, this.Value);
        }

        #region ICloneable<SearchContains<T>>

        public SearchContains<T> Clone()
        {
            var ds = new DataContractSerializer(typeof(SearchContains<T>));

            using (BufferStream stream = new BufferStream(BufferManager.Instance))
            {
                using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                stream.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchContains<T>)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchRegex", Namespace = "http://Amoeba/Windows")]
    class SearchRegex : IEquatable<SearchRegex>, ICloneable<SearchRegex>
    {
        private string _value;
        private bool _isIgnoreCase;

        private Regex _regex;

        [DataMember(Name = "Value")]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.RegexUpdate();
            }
        }

        [DataMember(Name = "IsIgnoreCase")]
        public bool IsIgnoreCase
        {
            get
            {
                return _isIgnoreCase;
            }
            set
            {
                _isIgnoreCase = value;

                this.RegexUpdate();
            }
        }

        private void RegexUpdate()
        {
            var o = RegexOptions.Compiled | RegexOptions.Singleline;
            if (_isIgnoreCase) o |= RegexOptions.IgnoreCase;

            if (_value != null) _regex = new Regex(_value, o);
            else _regex = null;
        }

        public bool IsMatch(string value)
        {
            if (_regex == null) return false;

            return _regex.IsMatch(value);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRegex)) return false;

            return this.Equals((SearchRegex)obj);
        }

        public bool Equals(SearchRegex other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((this.IsIgnoreCase != other.IsIgnoreCase)
                || (this.Value != other.Value))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.IsIgnoreCase, this.Value);
        }

        #region ICloneable<SearchRegex>

        public SearchRegex Clone()
        {
            var ds = new DataContractSerializer(typeof(SearchRegex));

            using (BufferStream stream = new BufferStream(BufferManager.Instance))
            {
                using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                stream.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchRegex)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchRange", Namespace = "http://Amoeba/Windows")]
    class SearchRange<T> : IEquatable<SearchRange<T>>, ICloneable<SearchRange<T>>
        where T : IComparable
    {
        T _max;
        T _min;

        [DataMember(Name = "Max")]
        public T Max
        {
            get
            {
                return _max;
            }
            set
            {
                _max = value;
                _min = (_min.CompareTo(_max) > 0) ? _max : _min;
            }
        }

        [DataMember(Name = "Min")]
        public T Min
        {
            get
            {
                return _min;
            }
            set
            {
                _min = value;
                _max = (_max.CompareTo(_min) < 0) ? _min : _max;
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
            return this.Min.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRange<T>)) return false;

            return this.Equals((SearchRange<T>)obj);
        }

        public bool Equals(SearchRange<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((!this.Min.Equals(other.Min))
                || (!this.Max.Equals(other.Max)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("Max = {0}, Min = {1}", this.Max, this.Min);
        }

        #region ICloneable<SearchRange<T>>

        public SearchRange<T> Clone()
        {
            var ds = new DataContractSerializer(typeof(SearchRange<T>));

            using (BufferStream stream = new BufferStream(BufferManager.Instance))
            {
                using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                {
                    ds.WriteObject(textDictionaryWriter, this);
                }

                stream.Position = 0;

                using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                {
                    return (SearchRange<T>)ds.ReadObject(textDictionaryReader);
                }
            }
        }

        #endregion
    }
}
