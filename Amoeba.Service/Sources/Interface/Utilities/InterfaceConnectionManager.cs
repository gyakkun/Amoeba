using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Configuration;
using Omnius.Io;
using Omnius.Messaging;
using Omnius.Security;
using Omnius.Serialization;
using Omnius.Utilities;
using Omnius.Net;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization;

namespace Amoeba.Service
{
    class InterfaceConnectionManager : ManagerBase
    {
        private Socket _socket;
        private BufferManager _bufferManager;

        private StreamReader _reader;
        private StreamWriter _writer;

        private TaskManager _sendTaskManager;
        private TaskManager _receiveTaskManager;

        private Random _random = new Random();

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public InterfaceConnectionManager(Socket socket, BufferManager bufferManager)
        {
            _socket = socket;
            _bufferManager = bufferManager;

            var networkStream = new NetworkStream(socket);
            _reader = new StreamReader(networkStream, new UTF8Encoding(false), false, 1024 * 32);
            _writer = new StreamWriter(networkStream, new UTF8Encoding(false), 1024 * 32);
            _writer.NewLine = "\n";

            _sendTaskManager = new TaskManager(this.SendThread);
            _receiveTaskManager = new TaskManager(this.ReceiveThread);
        }

        private void SendThread(CancellationToken token)
        {

        }

        private void ReceiveThread(CancellationToken token)
        {

        }

        class CustomContractResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                if (objectType.GetTypeInfo().GetInterfaces().Any(type => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                {
                    return base.CreateArrayContract(objectType);
                }

                if (objectType.GetTypeInfo().CustomAttributes.Any(n => n.AttributeType == typeof(DataContractAttribute)))
                {
                    var objectContract = base.CreateObjectContract(objectType);
                    objectContract.DefaultCreatorNonPublic = true;
                    objectContract.DefaultCreator = () => Activator.CreateInstance(objectType, true);

                    return objectContract;
                }

                return base.CreateContract(objectType);
            }
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
