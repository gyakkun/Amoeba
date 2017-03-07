using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Core
{
    [DataContract(Name = nameof(Node<T>))]
    public struct Node<T>
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
    }
}
