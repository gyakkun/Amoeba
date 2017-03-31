using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Serialization;

namespace Amoeba.Service
{
    public class InterfaceManager : ManagerBase
    {
        private BufferManager _bufferManager;
        private ControlManager _serviceManager;

        private Stream _stream;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public InterfaceManager(Stream stream)
        {
            _stream = stream;
            _bufferManager = new BufferManager(1024 * 1024 * 1024, 1024 * 1024 * 256);
        }

        public void Start()
        {
            var task = new Task(this.WatchThread, TaskCreationOptions.LongRunning);
            task.Start();
            task.Wait();
        }

        private void WatchThread()
        {
            for (;;)
            {
                long type = VintUtils.Get(_stream);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
            }
        }
    }
}
