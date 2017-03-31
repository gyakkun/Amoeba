using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Omnius.Base;

namespace Amoeba.Service
{
    public class InterfaceManager : ManagerBase
    {
        private BufferManager _bufferManager;
        private ControlManager _serviceManager;

        private IPEndPoint _listenPoint;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public InterfaceManager(IPEndPoint listenPoint)
        {
            _listenPoint = listenPoint;
            _bufferManager = new BufferManager(1024 * 1024 * 1024, 1024 * 1024 * 256);
        }

        public void Start()
        {

        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {

            }
        }
    }
}
