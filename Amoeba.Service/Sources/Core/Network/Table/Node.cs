using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Utilities;

namespace Amoeba.Service
{
    [DataContract(Name = nameof(Node<T>))]
    public struct Node<T> : IEquatable<Node<T>>
    {
        public Node(byte[] id, T value)
        {
            this.Id = id;
            this.Value = value;
        }

        [DataMember(Name = nameof(Id))]
        public byte[] Id { get; private set; }

        [DataMember(Name = nameof(Value))]
        public T Value { get; private set; }

        public static bool operator ==(Node<T> x, Node<T> y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Node<T> x, Node<T> y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return (this.Id != null) ? ItemUtils.GetHashCode(this.Id) : 0;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is Node<T>)) return false;

            return this.Equals((Node<T>)obj);
        }

        public bool Equals(Node<T> other)
        {
            if ((object)other == null) return false;

            if ((this.Id == null) != (other.Id == null)
                || !this.Value.Equals(other.Value))
            {
                return false;
            }

            if (this.Id != null && other.Id != null)
            {
                if (!Unsafe.Equals(this.Id, other.Id)) return false;
            }

            return true;
        }
    }
}
