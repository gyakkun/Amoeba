using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Amoeba.Interface
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    class SearchRegex : IEquatable<SearchRegex>
    {
        private Regex _regex;

        [JsonConstructor]
        public SearchRegex(string value, bool isIgnoreCase)
        {
            this.Value = value;
            this.IsIgnoreCase = isIgnoreCase;

            this.RegexUpdate();
        }

        [JsonProperty]
        public string Value { get; }

        [JsonProperty]
        public bool IsIgnoreCase { get; }

        private void RegexUpdate()
        {
            var o = RegexOptions.Compiled | RegexOptions.Singleline;
            if (this.IsIgnoreCase) o |= RegexOptions.IgnoreCase;

            if (this.Value != null) _regex = new Regex(this.Value, o);
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
}
