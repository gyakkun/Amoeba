using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(SearchContains<T>))]
    class SearchContains<T> : IEquatable<SearchContains<T>>
    {
        private bool _contains;
        private T _value;

        public SearchContains(bool contains, T value)
        {
            this.Contains = contains;
            this.Value = value;
        }

        [DataMember(Name = nameof(Contains))]
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

        [DataMember(Name = nameof(Value))]
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
}
