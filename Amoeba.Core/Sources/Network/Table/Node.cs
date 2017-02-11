using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Core.Network
{
    [DataContract(Name = "Node")]
    public struct Node<T>
    {
        public Node(byte[] id, T value)
        {
            this.Id = id;
            this.Value = value;
        }

        [DataMember(Name = "Id")]
        public byte[] Id { get; private set; }

        [DataMember(Name = "Value")]
        public T Value { get; private set; }
    }
}
