using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omnius.Utils;

namespace Amoeba.Simulation
{
    partial class SimulationManager
    {
        sealed class SessionInfo
        {
            public List<byte[]> ConnecedNodeIds { get; } = new List<byte[]>();
            public HashSet<byte[]> StockedBlockIds { get; } = new HashSet<byte[]>(new ByteArrayEqualityComparer());
            public Queue<Packet> PacketQueue { get; } = new Queue<Packet>();

            public HashSet<byte[]> ReceivedRequestIds { get; } = new HashSet<byte[]>(new ByteArrayEqualityComparer());
            public HashSet<byte[]> ReceivedLinkIds { get; } = new HashSet<byte[]>(new ByteArrayEqualityComparer());
        }
    }
}
