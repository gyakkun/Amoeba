using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(SearchRegex))]
    class SearchRegex : IEquatable<SearchRegex>
    {
        private string _value;
        private bool _isIgnoreCase;

        private Regex _regex;

        private SearchRegex() { }

        public SearchRegex(string value, bool isIgnoreCase)
        {
            this.Value = value;
            this.IsIgnoreCase = isIgnoreCase;
        }

        [DataMember(Name = nameof(Value))]
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

        [DataMember(Name = nameof(IsIgnoreCase))]
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
}
