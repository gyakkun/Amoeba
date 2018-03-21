using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Service;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Io;
using Omnius.Net;
using Omnius.Security;
using Omnius.Utils;

namespace Amoeba.Simulation
{
    sealed partial class SimulationManager : ManagerBase
    {
        private TaskManager _taskManager;

        private List<Node<SessionInfo>> _nodes = new List<Node<SessionInfo>>();

        private readonly object _lockObject = new object();
        private volatile bool _isDisposed;

        private const int MaxIdLength = 8;

        public SimulationManager()
        {
            _taskManager = new TaskManager(this.Watch);
        }

        public void Run()
        {
            this.Init();

            _taskManager.Start();
        }

        private void Init()
        {
            lock (_lockObject)
            {
                var random = RandomProvider.GetThreadRandom();

                var ids = new HashSet<byte[]>(new ByteArrayEqualityComparer());

                for (; ; )
                {
                    var id = new byte[MaxIdLength];
                    random.NextBytes(id);

                    ids.Add(id);

                    if (ids.Count >= 1000) break;
                }

                foreach (var id in ids)
                {
                    _nodes.Add(new Node<SessionInfo>(id, new SessionInfo()));
                }

                foreach (var node in _nodes)
                {
                    node.Value.ConnecedNodeIds.AddRange(ComputeConnectedNodes(node.Id, ids));
                }
            }
        }

        private static IEnumerable<byte[]> ComputeConnectedNodes(byte[] myId, IEnumerable<byte[]> ids)
        {
            var table = new List<byte[]>[MaxIdLength];

            foreach (var id in ids)
            {
                var tempList = table[RouteTableMethods.Distance(myId, id)];
                if (tempList.Count >= 20) continue;

                tempList.Add(id);
            }

            return table.SelectMany(n => n);
        }

        private void Watch(CancellationToken token)
        {
            for (; ; )
            {
                if (token.WaitHandle.WaitOne(1000)) return;

                lock (_lockObject)
                {
                    foreach (var node in _nodes)
                    {
                        foreach (var receivedPacket in node.Value.PacketQueue)
                        {
                            if (receivedPacket.Type == PacketType.Result)
                            {
                                node.Value.
                            }
                            else if (receivedPacket.Type == PacketType.Request)
                            {

                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<byte[]> Upload(long uploadSize)
        {
            lock (_lockObject)
            {
            }
        }

        public void Download(IEnumerable<byte[]> blockIds)
        {
            lock (_lockObject)
            {
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                _taskManager.Stop();
                _taskManager.Dispose();
            }
        }
    }
}
