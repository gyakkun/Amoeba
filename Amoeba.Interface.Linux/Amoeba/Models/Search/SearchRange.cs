using System;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    [DataContract(Name = nameof(SearchRange<T>))]
    public struct SearchRange<T> : IEquatable<SearchRange<T>>
        where T : IComparable<T>, IEquatable<T>
    {
        private T _min;
        private T _max;

        public SearchRange(T min, T max)
        {
            _min = min;
            _max = (max.CompareTo(_min) < 0) ? _min : max;
        }

        [DataMember(Name = nameof(Min))]
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

        [DataMember(Name = nameof(Max))]
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
