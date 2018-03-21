using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Amoeba.Interface
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    class SearchCondition<T> : IEquatable<SearchCondition<T>>
    {
        public SearchCondition(bool isContains, T value)
        {
            this.IsContains = isContains;
            this.Value = value;
        }

        [JsonProperty]
        public bool IsContains { get; }

        [JsonProperty]
        public T Value { get; }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SearchCondition<T>)) return false;
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
