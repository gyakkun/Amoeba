using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Io;
using Omnius.Messaging;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Rpc
{
    public class AmoebaInterfaceManager : StateManagerBase, IService, ISynchronized
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private MessagingManager _messagingManager;
        private BufferManager _bufferManager = BufferManager.Instance;

        private Random _random = new Random();

        private LockedHashDictionary<int, WaitQueue<ResponseInfo>> _queueMap = new LockedHashDictionary<int, WaitQueue<ResponseInfo>>();

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public AmoebaInterfaceManager()
        {

        }

        public void Connect(IPEndPoint endpoint)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(endpoint);

            _networkStream = new NetworkStream(_tcpClient.Client);
            _messagingManager = new MessagingManager(_networkStream, _bufferManager);
            _messagingManager.ReceiveEvent += MessagingManager_ReceiveEvent;
            _messagingManager.Run();
        }

        private void MessagingManager_ReceiveEvent(Stream responseStream)
        {
            using (var reader = new ItemStreamReader(new WrapperStream(responseStream, true), _bufferManager))
            {
                var type = (AmoebaResponseType)reader.GetUInt32();
                int id = (int)reader.GetUInt32();

                if (_queueMap.TryGetValue(id, out var queue))
                {
                    queue.Enqueue(new ResponseInfo() { Type = type, Stream = new RangeStream(responseStream) });
                }
            }
        }

        public void Exit()
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.Exit, (object)null, CancellationToken.None);
            }
        }

        private int CreateId()
        {
            lock (_queueMap.LockObject)
            {
                for (; ; )
                {
                    int id = _random.Next();
                    if (!_queueMap.ContainsKey(id)) return id;
                }
            }
        }

        private (int, WaitQueue<ResponseInfo>) Send<TArgument>(AmoebaRequestType type, TArgument argument)
        {
            int id = this.CreateId();
            var queue = new WaitQueue<ResponseInfo>();

            _queueMap.Add(id, queue);

            using (var writer = new ItemStreamWriter(_bufferManager))
            {
                writer.Write((uint)type);
                writer.Write((uint)id);

                Stream valueStream = null;

                if (argument != null)
                {
                    try
                    {
                        valueStream = new BufferStream(_bufferManager);
                        JsonUtils.Save(valueStream, argument);
                    }
                    catch (Exception)
                    {
                        if (valueStream != null)
                        {
                            valueStream.Dispose();
                            valueStream = null;
                        }

                        throw;
                    }
                }

                _messagingManager.Send(new UniteStream(writer.GetStream(), valueStream));
            }

            return (id, queue);
        }

        private void Cancel(int id)
        {
            using (var writer = new ItemStreamWriter(_bufferManager))
            {
                writer.Write((uint)AmoebaRequestType.Cancel);
                writer.Write((uint)id);

                _messagingManager.Send(writer.GetStream());
            }
        }

        private TResult Function<TResult, TArgument, TProgress>(AmoebaRequestType type, TArgument argument, IProgress<TProgress> progress, CancellationToken token)
        {
            var (id, queue) = this.Send(type, argument);

            using (var register = token.Register(() => this.Cancel(id)))
            {
                for (; ; )
                {
                    ResponseInfo info = null;

                    try
                    {
                        info = queue.Dequeue();

                        if (info.Type == AmoebaResponseType.Result)
                        {
                            return JsonUtils.Load<TResult>(info.Stream);
                        }
                        else if (info.Type == AmoebaResponseType.Output)
                        {
                            progress.Report(JsonUtils.Load<TProgress>(info.Stream));
                        }
                        else if (info.Type == AmoebaResponseType.Error)
                        {
                            throw new AmoebaInterfaceManagerException(JsonUtils.Load<string>(info.Stream));
                        }

                        throw new NotSupportedException();
                    }
                    finally
                    {
                        queue.Dispose();
                        _queueMap.Remove(id);

                        if (info != null)
                        {
                            info.Stream.Dispose();
                        }
                    }
                }
            }
        }

        private TResult Function<TResult, TArgument>(AmoebaRequestType type, TArgument argument, CancellationToken token)
        {
            var (id, queue) = this.Send(type, argument);

            using (var register = token.Register(() => this.Cancel(id)))
            {
                ResponseInfo info = null;

                try
                {
                    info = queue.Dequeue();

                    if (info.Type == AmoebaResponseType.Result)
                    {
                        return JsonUtils.Load<TResult>(info.Stream);
                    }
                    else if (info.Type == AmoebaResponseType.Error)
                    {
                        throw new AmoebaInterfaceManagerException(JsonUtils.Load<string>(info.Stream));
                    }

                    throw new NotSupportedException();
                }
                finally
                {
                    queue.Dispose();
                    _queueMap.Remove(id);

                    if (info != null)
                    {
                        info.Stream.Dispose();
                    }
                }
            }
        }

        private void Action<TArgument, TProgress>(AmoebaRequestType type, TArgument argument, IProgress<TProgress> progress, CancellationToken token)
        {
            var (id, queue) = this.Send(type, argument);

            using (var register = token.Register(() => this.Cancel(id)))
            {
                for (; ; )
                {
                    ResponseInfo info = null;

                    try
                    {
                        info = queue.Dequeue();

                        if (info.Type == AmoebaResponseType.Result)
                        {
                            return;
                        }
                        else if (info.Type == AmoebaResponseType.Output)
                        {
                            progress.Report(JsonUtils.Load<TProgress>(info.Stream));
                        }
                        else if (info.Type == AmoebaResponseType.Error)
                        {
                            throw new AmoebaInterfaceManagerException(JsonUtils.Load<string>(info.Stream));
                        }

                        throw new NotSupportedException();
                    }
                    finally
                    {
                        queue.Dispose();
                        _queueMap.Remove(id);

                        if (info != null)
                        {
                            info.Stream.Dispose();
                        }
                    }
                }
            }
        }

        private void Action<TArgument>(AmoebaRequestType type, TArgument argument, CancellationToken token)
        {
            var (id, queue) = this.Send(type, argument);

            using (var register = token.Register(() => this.Cancel(id)))
            {
                ResponseInfo info = null;

                try
                {
                    info = queue.Dequeue();

                    if (info.Type == AmoebaResponseType.Result)
                    {
                        return;
                    }
                    else if (info.Type == AmoebaResponseType.Error)
                    {
                        throw new AmoebaInterfaceManagerException(JsonUtils.Load<string>(info.Stream));
                    }

                    throw new NotSupportedException();
                }
                finally
                {
                    queue.Dispose();
                    _queueMap.Remove(id);

                    if (info != null)
                    {
                        info.Stream.Dispose();
                    }
                }
            }
        }

        private class ResponseInfo
        {
            public AmoebaResponseType Type { get; set; }
            public Stream Stream { get; set; }
        }

        private void Check()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);
        }

        public ServiceReport Report
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return this.Function<ServiceReport, object>(AmoebaRequestType.GetReport, null, CancellationToken.None);
                }
            }
        }

        public IEnumerable<NetworkConnectionReport> GetNetworkConnectionReports()
        {
            this.Check();

            lock (_lockObject)
            {
                return this.Function<NetworkConnectionReport[], object>(AmoebaRequestType.GetNetworkConnectionReports, null, CancellationToken.None);
            }
        }

        public IEnumerable<CacheContentReport> GetCacheContentReports()
        {
            this.Check();

            lock (_lockObject)
            {
                return this.Function<CacheContentReport[], object>(AmoebaRequestType.GetCacheContentReports, null, CancellationToken.None);
            }
        }

        public IEnumerable<DownloadContentReport> GetDownloadContentReports()
        {
            this.Check();

            lock (_lockObject)
            {
                return this.Function<DownloadContentReport[], object>(AmoebaRequestType.GetDownloadContentReports, null, CancellationToken.None);
            }
        }

        public ServiceConfig Config
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return this.Function<ServiceConfig, object>(AmoebaRequestType.GetConfig, null, CancellationToken.None);
                }
            }
        }

        public void SetConfig(ServiceConfig config)
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.SetConfig, config, CancellationToken.None);
            }
        }

        public void SetCloudLocations(IEnumerable<Location> locations)
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.SetCloudLocations, locations.ToArray(), CancellationToken.None);
            }
        }

        public long Size
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return this.Function<long, object>(AmoebaRequestType.GetSize, null, CancellationToken.None);
                }
            }
        }

        public void Resize(long size)
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.Resize, size, CancellationToken.None);
            }
        }

        public Task CheckBlocks(IProgress<CheckBlocksProgressReport> progress, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    this.Action(AmoebaRequestType.CheckBlocks, (object)null, progress, token);
                });
            }
        }

        public Task<Metadata> AddContent(string path, DateTime creationTime, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    return this.Function<Metadata, (string, DateTime)>(AmoebaRequestType.AddContent, (path, creationTime), token);
                });
            }
        }

        public void RemoveContent(string path)
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.RemoveContent, path, CancellationToken.None);
            }
        }

        public void Diffusion(string path)
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.Diffusion, path, CancellationToken.None);
            }
        }

        public void AddDownload(Metadata metadata, string path, long maxLength)
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.AddDownload, (metadata, path, maxLength), CancellationToken.None);
            }
        }

        public void RemoveDownload(Metadata metadata, string path)
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.RemoveDownload, (metadata, path), CancellationToken.None);
            }
        }

        public void ResetDownload(Metadata metadata, string path)
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.ResetDownload, (metadata, path), CancellationToken.None);
            }
        }

        public Task SetProfile(Profile profile, DigitalSignature digitalSignature, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    this.Action(AmoebaRequestType.SetProfile, (profile, digitalSignature), token);
                });
            }
        }

        public Task SetStore(Store store, DigitalSignature digitalSignature, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    this.Action(AmoebaRequestType.SetStore, (store, digitalSignature), token);
                });
            }
        }

        public Task SetMailMessage(Signature targetSignature, MailMessage mailMessage, ExchangePublicKey exchangePublicKey, DigitalSignature digitalSignature, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    this.Action(AmoebaRequestType.SetMailMessage, (targetSignature, mailMessage, exchangePublicKey, digitalSignature), token);
                });
            }
        }

        public Task SetChatMessage(Tag tag, ChatMessage chatMessage, DigitalSignature digitalSignature, TimeSpan miningTimeSpan, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    this.Action(AmoebaRequestType.SetChatMessage, (tag, chatMessage, digitalSignature, miningTimeSpan), token);
                });
            }
        }

        public Task<BroadcastMessage<Profile>> GetProfile(Signature signature, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    return this.Function<BroadcastMessage<Profile>, Signature>(AmoebaRequestType.GetProfile, signature, token);
                });
            }
        }

        public Task<BroadcastMessage<Store>> GetStore(Signature signature, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    return this.Function<BroadcastMessage<Store>, Signature>(AmoebaRequestType.GetStore, signature, token);
                });
            }
        }

        public Task<IEnumerable<UnicastMessage<MailMessage>>> GetMailMessages(Signature signature, ExchangePrivateKey exchangePrivateKey, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    return this.Function<IEnumerable<UnicastMessage<MailMessage>>, (Signature, ExchangePrivateKey)>(AmoebaRequestType.GetMailMessages, (signature, exchangePrivateKey), token);
                });
            }
        }

        public Task<IEnumerable<MulticastMessage<ChatMessage>>> GetChatMessages(Tag tag, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    return this.Function<IEnumerable<MulticastMessage<ChatMessage>>, Tag>(AmoebaRequestType.GetChatMessages, tag, token);
                });
            }
        }

        public override ManagerState State
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return this.Function<ManagerState, object>(AmoebaRequestType.GetState, null, CancellationToken.None);
                }
            }
        }

        public override void Start()
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.Start, (object)null, CancellationToken.None);
            }
        }

        public override void Stop()
        {
            this.Check();

            lock (_lockObject)
            {
                this.Action(AmoebaRequestType.Stop, (object)null, CancellationToken.None);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _tcpClient.Dispose();
                _networkStream.Dispose();
                _messagingManager.Stop();
                _messagingManager.Dispose();
            }
        }

        public object LockObject
        {
            get
            {
                return _lockObject;
            }
        }
    }

    public class AmoebaInterfaceManagerException : StateManagerException
    {
        public AmoebaInterfaceManagerException() : base() { }
        public AmoebaInterfaceManagerException(string message) : base(message) { }
        public AmoebaInterfaceManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
