using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Net;
using System.Threading;
using Amoeba.Core;

namespace Amoeba.Service
{
    public sealed class ServiceManager : StateManagerBase, ISettings
    {
        private BufferManager _bufferManager;
        private CoreManager _coreManager;
        private ConnectionManager _connectionManager;
        private MessageManager _messageManager;

        private volatile ManagerState _state = ManagerState.Stop;

        private bool _isLoaded = false;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public ServiceManager(string configPath, string blocksPath, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _coreManager = new CoreManager(Path.Combine(configPath, "CoreManager"), blocksPath, _bufferManager);
            _connectionManager = new ConnectionManager(Path.Combine(configPath, "ConnectionManager"), _coreManager, _bufferManager);
            _messageManager = new MessageManager(Path.Combine(configPath, "MessageManager"), _coreManager, _bufferManager);
        }

        private void Check()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);
            if (!_isLoaded) throw new ServiceManagerException("ServiceManager is not loaded.");
        }

        public Information Information
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    var contexts = new List<InformationContext>();
                    contexts.AddRange(_coreManager.Information);
                    contexts.AddRange(_connectionManager.Information);

                    return new Information(contexts);
                }
            }
        }

        public Location MyLocation
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _coreManager.MyLocation;
                }
            }
        }

        public IEnumerable<Location> CrowdLocations
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _coreManager.CrowdLocations;
                }
            }
        }

        public IEnumerable<Signature> SearchSignatures
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _messageManager.SearchSignatures;
                }
            }
        }

        public int ConnectionCountLimit
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _coreManager.ConnectionCountLimit;
                }
            }
            set
            {
                this.Check();

                lock (_lockObject)
                {
                    _coreManager.ConnectionCountLimit = value;
                }
            }
        }

        public int BandwidthLimit
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _coreManager.BandwidthLimit;
                }
            }
            set
            {
                this.Check();

                lock (_lockObject)
                {
                    _coreManager.BandwidthLimit = value;
                }
            }
        }

        public long Size
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _coreManager.Size;
                }
            }
        }

        public IEnumerable<Information> GetConnectionInformations()
        {
            this.Check();

            lock (_lockObject)
            {
                return _coreManager.GetConnectionInformations();
            }
        }

        public void SetMyLocation(Location location)
        {
            this.Check();

            lock (_lockObject)
            {
                _coreManager.SetMyLocation(location);
            }
        }

        public void SetCrowdLocations(IEnumerable<Location> locations)
        {
            this.Check();

            lock (_lockObject)
            {
                _coreManager.SetCrowdLocations(locations);
            }
        }

        public void SetSearchSignatures(IEnumerable<Signature> signatures)
        {
            this.Check();

            lock (_lockObject)
            {
                _messageManager.SetSearchSignatures(signatures);
            }
        }

        public void Resize(long size)
        {
            this.Check();

            lock (_lockObject)
            {
                _coreManager.Resize(size);
            }
        }

        public Task CheckBlocks(CheckBlocksProgressEventHandler checkBlocksProgressEvent, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _coreManager.CheckBlocks(checkBlocksProgressEvent, token);
            }
        }

        public CatharsisConfig CatharsisConfig
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _connectionManager.CatharsisConfig;
                }
            }
            set
            {
                this.Check();

                lock (_lockObject)
                {
                    _connectionManager.CatharsisConfig = value;
                }
            }
        }

        public TcpConnectionConfig TcpConnectionConfig
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _connectionManager.TcpConnectionConfig;
                }
            }
            set
            {
                this.Check();

                lock (_lockObject)
                {
                    _connectionManager.TcpConnectionConfig = value;
                }
            }
        }

        public I2pConnectionConfig I2pConnectionConfig
        {
            get
            {
                this.Check();

                lock (_lockObject)
                {
                    return _connectionManager.I2pConnectionConfig;
                }
            }
            set
            {
                this.Check();

                lock (_lockObject)
                {
                    _connectionManager.I2pConnectionConfig = value;
                }
            }
        }

        #region Content

        public Task<Metadata> Import(string path, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _coreManager.Import(path, token);
            }
        }

        public IEnumerable<Information> GetContentInformations()
        {
            this.Check();

            lock (_lockObject)
            {
                return _coreManager.GetContentInformations();
            }
        }

        public void RemoveContent(string path)
        {
            this.Check();

            lock (_lockObject)
            {
                _coreManager.RemoveContent(path);
            }
        }

        public void Download(Metadata metadata, long maxLength)
        {
            this.Check();

            lock (_lockObject)
            {
                _coreManager.Download(metadata, maxLength);
            }
        }

        public IEnumerable<Information> GetDownloadInformations()
        {
            this.Check();

            lock (_lockObject)
            {
                return _coreManager.GetDownloadInformations();
            }
        }

        public void RemoveDownload(Metadata metadata)
        {
            this.Check();

            lock (_lockObject)
            {
                _coreManager.RemoveDownload(metadata);
            }
        }

        public void ResetDownload(Metadata metadata)
        {
            this.Check();

            lock (_lockObject)
            {
                _coreManager.ResetDownload(metadata);
            }
        }

        public Task Export(Metadata metadata, Stream outStream, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _coreManager.Export(metadata, outStream, token);
            }
        }

        #endregion

        #region Message

        public Task<BroadcastMessage<Profile>> GetProfile(Signature signature)
        {
            this.Check();

            lock (_lockObject)
            {
                return _messageManager.GetProfile(signature);
            }
        }

        public Task<BroadcastMessage<Store>> GetStore(Signature signature)
        {
            this.Check();

            lock (_lockObject)
            {
                return _messageManager.GetStore(signature);
            }
        }

        public Task<IEnumerable<UnicastMessage<MailMessage>>> GetMailMessages(Signature signature, IExchangeDecrypt exchangePrivateKey)
        {
            this.Check();

            lock (_lockObject)
            {
                return _messageManager.GetMailMessages(signature, exchangePrivateKey);
            }
        }

        public Task<IEnumerable<MulticastMessage<ChatMessage>>> GetChatMessages(Tag tag, IExchangeDecrypt exchangePrivateKey)
        {
            this.Check();

            lock (_lockObject)
            {
                return _messageManager.GetChatMessages(tag, exchangePrivateKey);
            }
        }

        public Task Upload(Profile profile, DigitalSignature digitalSignature, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _messageManager.Upload(profile, digitalSignature, token);
            }
        }

        public Task Upload(Store store, DigitalSignature digitalSignature, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _messageManager.Upload(store, digitalSignature, token);
            }
        }

        public Task Upload(Signature targetSignature, MailMessage mailMessage, DigitalSignature digitalSignature, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _messageManager.Upload(targetSignature, mailMessage, digitalSignature, token);
            }
        }

        public Task Upload(Tag tag, ChatMessage chatMessage, DigitalSignature digitalSignature, Miner miner, CancellationToken token)
        {
            this.Check();

            lock (_lockObject)
            {
                return _messageManager.Upload(tag, chatMessage, digitalSignature, miner, token);
            }
        }

        #endregion

        public override ManagerState State
        {
            get
            {
                this.Check();

                return _state;
            }
        }

        public override void Start()
        {
            this.Check();

            lock (_lockObject)
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                _coreManager.Start();
                _connectionManager.Start();
            }
        }

        public override void Stop()
        {
            this.Check();

            lock (_lockObject)
            {
                if (this.State == ManagerState.Stop) return;
                _state = ManagerState.Stop;

                _connectionManager.Stop();
                _coreManager.Stop();
            }
        }

        #region ISettings

        public void Load()
        {
            if (_disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (_lockObject)
            {
                if (_isLoaded) throw new ServiceManagerException("ServiceManager was already loaded.");
                _isLoaded = true;

#if DEBUG
                var stopwatch = new Stopwatch();
                stopwatch.Start();
#endif

                {
                    var tasks = new List<Task>();

                    tasks.Add(Task.Run(() => _coreManager.Load()));

                    Task.WaitAll(tasks.ToArray());
                }

                {
                    var tasks = new List<Task>();

                    tasks.Add(Task.Run(() => _connectionManager.Load()));
                    tasks.Add(Task.Run(() => _messageManager.Load()));

                    Task.WaitAll(tasks.ToArray());
                }

#if DEBUG
                stopwatch.Stop();
                Debug.WriteLine("ServiceManager Load: {0}", stopwatch.ElapsedMilliseconds);
#endif
            }
        }

        public void Save()
        {
            this.Check();

            lock (_lockObject)
            {
#if DEBUG
                var stopwatch = new Stopwatch();
                stopwatch.Start();
#endif

                {
                    var tasks = new List<Task>();

                    tasks.Add(Task.Run(() => _messageManager.Save()));
                    tasks.Add(Task.Run(() => _connectionManager.Save()));

                    Task.WaitAll(tasks.ToArray());
                }

                {
                    var tasks = new List<Task>();

                    tasks.Add(Task.Run(() => _coreManager.Save()));

                    Task.WaitAll(tasks.ToArray());
                }

#if DEBUG
                stopwatch.Stop();
                Debug.WriteLine("ServiceManager Save: {0}", stopwatch.ElapsedMilliseconds);
#endif
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _messageManager.Dispose();
                _connectionManager.Dispose();
                _coreManager.Dispose();
            }
        }
    }

    [Serializable]
    class ServiceManagerException : StateManagerException
    {
        public ServiceManagerException() : base() { }
        public ServiceManagerException(string message) : base(message) { }
        public ServiceManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
