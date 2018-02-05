using System;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(SearchCondition<T>))]
    class SearchCondition<T> : IEquatable<SearchCondition<T>>
    {
        private bool _isContains;
        private T _value;

        public SearchCondition(bool isContains, T value)
        {
            this.IsContains = isContains;
            this.Value = value;
        }

        [DataMember(Name = nameof(IsContains))]
        public bool IsContains
        {
            get
            {
                return _isContains;
            }
            private set
            {
                _isContains = value;
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
            if ((object)obj == null || !(obj is SearchCondition<T>)) return false;

            return this.Equals((SearchCondition<T>)obj);
        }

        public bool Equals(SearchCondition<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if ((this.IsContains != other.IsContains)
                || (!this.Value.Equals(other.Value)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("Contains = {0}, Value = {1}", this.IsContains, this.Value);
        }
    }
}
