using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Amoeba.Interface
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    readonly struct SearchRange<T> : IEquatable<SearchRange<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        [JsonConstructor]
        public SearchRange(T min, T max)
        {
            this.Min = min;
            this.Max = (max.CompareTo(this.Min) < 0) ? this.Min : max;
        }

        [JsonProperty]
        public T Min { get; }

        [JsonProperty]
        public T Max { get; }

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
            if (!(obj is SearchRange<T>)) return false;
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
