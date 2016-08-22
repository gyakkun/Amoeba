using System;
using System.Collections.Generic;
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
        private List<SearchContains<string>> _searchNameCollection;
        private List<SearchContains<SearchRegex>> _searchNameRegexCollection;
        private List<SearchContains<SearchRegex>> _searchSignatureCollection;
        private List<SearchContains<string>> _searchKeywordCollection;
        private List<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;
        private List<SearchContains<SearchRange<long>>> _searchLengthRangeCollection;
        private List<SearchContains<Seed>> _searchSeedCollection;
        private List<SearchContains<SearchState>> _searchStateCollection;

        private static readonly object _initializeLock = new object();
        private volatile object _thisLock;

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
        public List<SearchContains<string>> SearchNameCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchNameCollection == null)
                        _searchNameCollection = new List<SearchContains<string>>();

                    return _searchNameCollection;
                }
            }
        }

        [DataMember(Name = "SearchNameRegexCollection")]
        public List<SearchContains<SearchRegex>> SearchNameRegexCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchNameRegexCollection == null)
                        _searchNameRegexCollection = new List<SearchContains<SearchRegex>>();

                    return _searchNameRegexCollection;
                }
            }
        }

        [DataMember(Name = "SearchSignatureCollection")]
        public List<SearchContains<SearchRegex>> SearchSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSignatureCollection == null)
                        _searchSignatureCollection = new List<SearchContains<SearchRegex>>();

                    return _searchSignatureCollection;
                }
            }
        }

        [DataMember(Name = "SearchKeywordCollection")]
        public List<SearchContains<string>> SearchKeywordCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchKeywordCollection == null)
                        _searchKeywordCollection = new List<SearchContains<string>>();

                    return _searchKeywordCollection;
                }
            }
        }

        [DataMember(Name = "SearchCreationTimeRangeCollection")]
        public List<SearchContains<SearchRange<DateTime>>> SearchCreationTimeRangeCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchCreationTimeRangeCollection == null)
                        _searchCreationTimeRangeCollection = new List<SearchContains<SearchRange<DateTime>>>();

                    return _searchCreationTimeRangeCollection;
                }
            }
        }

        [DataMember(Name = "SearchLengthRangeCollection")]
        public List<SearchContains<SearchRange<long>>> SearchLengthRangeCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchLengthRangeCollection == null)
                        _searchLengthRangeCollection = new List<SearchContains<SearchRange<long>>>();

                    return _searchLengthRangeCollection;
                }
            }
        }

        [DataMember(Name = "SearchSeedCollection")]
        public List<SearchContains<Seed>> SearchSeedCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSeedCollection == null)
                        _searchSeedCollection = new List<SearchContains<Seed>>();

                    return _searchSeedCollection;
                }
            }
        }

        [DataMember(Name = "SearchStateCollection")]
        public List<SearchContains<SearchState>> SearchStateCollection
        {
            get
            {
                lock (this.ThisLock)
                {

                    if (_searchStateCollection == null)
                        _searchStateCollection = new List<SearchContains<SearchState>>();

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
                    using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                    {
                        ds.WriteObject(xmlDictionaryWriter, this);
                    }

                    stream.Seek(0, SeekOrigin.Begin);

                    using (XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchItem)ds.ReadObject(xmlDictionaryReader);
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
        [EnumMember(Value = "Link")]
        Link = 0x1,

        [EnumMember(Value = "Box")]
        Store = 0x2,

        [EnumMember(Value = "Cache")]
        Cache = 0x4,

        //[EnumMember(Value = "Share")]
        //Share = 0x8,

        [EnumMember(Value = "Downloading")]
        Downloading = 0x10,

        [EnumMember(Value = "Uploading")]
        Uploading = 0x20,

        [EnumMember(Value = "Downloaded")]
        Downloaded = 0x40,

        [EnumMember(Value = "Uploaded")]
        Uploaded = 0x80,
    }

    [DataContract(Name = "SearchContains", Namespace = "http://Amoeba/Windows")]
    class SearchContains<T> : IEquatable<SearchContains<T>>
    {
        private bool _contains;
        private T _value;

        public SearchContains(bool contains, T value)
        {
            this.Contains = contains;
            this.Value = value;
        }

        [DataMember(Name = "Contains")]
        public bool Contains
        {
            get
            {
                return _contains;
            }
            private set
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
            private set
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

            if ((this.Contains != other.Contains)
                || (!this.Value.Equals(other.Value)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("Contains = {0}, Value = {1}", this.Contains, this.Value);
        }
    }

    [DataContract(Name = "SearchRegex", Namespace = "http://Amoeba/Windows")]
    class SearchRegex : IEquatable<SearchRegex>
    {
        private string _value;
        private bool _isIgnoreCase;

        private Regex _regex;

        public SearchRegex(string value, bool isIgnoreCase)
        {
            this.Value = value;
            this.IsIgnoreCase = isIgnoreCase;
        }

        [DataMember(Name = "Value")]
        public string Value
        {
            get
            {
                return _value;
            }
            private set
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
            private set
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

            if ((this.IsIgnoreCase != other.IsIgnoreCase)
                || (this.Value != other.Value))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return this.Value;
        }
    }

    [DataContract(Name = "SearchRange", Namespace = "http://Amoeba/Windows")]
    struct SearchRange<T> : IEquatable<SearchRange<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        private T _min;
        private T _max;

        public SearchRange(T min, T max)
        {
            _min = min;
            _max = (max.CompareTo(_min) < 0) ? _min : max;
        }

        [DataMember(Name = "Min")]
        public T Min
        {
            get
            {
                return _min;
            }
            private set
            {
                _min = value;
            }
        }

        [DataMember(Name = "Max")]
        public T Max
        {
            get
            {
                return _max;
            }
            private set
            {
                _max = value;
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
            return this.Min.GetHashCode() ^ this.Max.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRange<T>)) return false;

            return this.Equals((SearchRange<T>)obj);
        }

        public bool Equals(SearchRange<T> other)
        {
            if (!this.Min.Equals(other.Min) || !this.Max.Equals(other.Max))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("Min = {0}, Max = {1}", this.Min, this.Max);
        }
    }
}
