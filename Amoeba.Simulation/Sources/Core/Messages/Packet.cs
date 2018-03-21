using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Amoeba.Simulation
{
    partial class SimulationManager
    {
        enum PacketType
        {
            Request,
            Link,
            Result,
        }

        sealed class Packet
        {
            private PacketType _type;
            private byte[] _id;
            private List<byte[]> _routes = new List<byte[]>();
            private ReadOnlyCollection<byte[]> _readOnlyRoutes;

            public Packet(PacketType type, byte[] id)
            {
                _type = type;
                _id = id;
            }

            public PacketType Type => _type;
            public byte[] Id => _id;

            public void AddRoute(byte[] id)
            {
                _routes.Add(id);
            }

            public IReadOnlyList<byte[]> GetRoutes(byte[] id)
            {
                if (_readOnlyRoutes == null)
                {
                    _readOnlyRoutes = new ReadOnlyCollection<byte[]>(_routes);
                }

                return _readOnlyRoutes;
            }
        }
    }
}
