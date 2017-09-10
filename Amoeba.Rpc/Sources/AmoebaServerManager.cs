using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Collections;
using Omnius.Io;
using Omnius.Messaging;
using Omnius.Security;
using Omnius.Serialization;

namespace Amoeba.Rpc
{
    public class AmoebaServerManager : ManagerBase
    {
        private BufferManager _bufferManager = BufferManager.Instance;

        private Random _random = new Random();

        private LockedHashDictionary<int, ResponseTask> _tasks = new LockedHashDictionary<int, ResponseTask>();

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public AmoebaServerManager()
        {

        }

        public Task Watch(ServiceManager serviceManager, IPEndPoint endpoint)
        {
            return Task.Run(() =>
            {
                var tcpListener = new TcpListener(endpoint);
                tcpListener.Start();

                using (var socket = tcpListener.AcceptSocket())
                using (var networkStream = new NetworkStream(socket))
                using (var messagingManager = new MessagingManager(networkStream, _bufferManager))
                using (var listenTokenSource = new CancellationTokenSource())
                {
                    messagingManager.ReceiveEvent += (stream) => this.MessagingManager_ReceiveEvent(serviceManager, stream, messagingManager.Send, () => listenTokenSource.Cancel());
                    messagingManager.Run();

                    listenTokenSource.Token.WaitHandle.WaitOne();

                    messagingManager.Stop();
                }

                foreach (var responseTask in _tasks.Values)
                {
                    try
                    {
                        responseTask.Stop();
                    }
                    catch (Exception)
                    {

                    }
                }
            });
        }

        private void MessagingManager_ReceiveEvent(ServiceManager serviceManager, Stream requestStream, Action<Stream> sendAction, Action exitAction)
        {
            using (var reader = new ItemStreamReader(new WrapperStream(requestStream, true), _bufferManager))
            {
                var type = (AmoebaRequestType)reader.GetUInt32();
                int id = (int)reader.GetUInt32();

                if (type == AmoebaRequestType.Exit)
                {
                    SendResponse(AmoebaResponseType.Result, id, (object)null);
                    exitAction();
                }
                else if (type == AmoebaRequestType.Cancel)
                {
                    if (_tasks.TryGetValue(id, out var responseTask))
                    {
                        responseTask.Stop();
                    }
                }
                else
                {
                    var responseTask = ResponseTask.Create((token) =>
                    {
                        try
                        {
                            switch (type)
                            {
                                case AmoebaRequestType.GetState:
                                    {
                                        SendResponse(AmoebaResponseType.Result, id, serviceManager.State);
                                        break;
                                    }
                                case AmoebaRequestType.Start:
                                    {
                                        serviceManager.Start();
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.Stop:
                                    {
                                        serviceManager.Stop();
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.GetReport:
                                    {
                                        SendResponse(AmoebaResponseType.Result, id, serviceManager.Report);
                                        break;
                                    }
                                case AmoebaRequestType.GetNetworkConnectionReports:
                                    {
                                        SendResponse(AmoebaResponseType.Result, id, serviceManager.GetNetworkConnectionReports());
                                        break;
                                    }
                                case AmoebaRequestType.GetCacheContentReports:
                                    {
                                        SendResponse(AmoebaResponseType.Result, id, serviceManager.GetCacheContentReports());
                                        break;
                                    }
                                case AmoebaRequestType.GetDownloadContentReports:
                                    {
                                        SendResponse(AmoebaResponseType.Result, id, serviceManager.GetDownloadContentReports());
                                        break;
                                    }
                                case AmoebaRequestType.GetConfig:
                                    {
                                        SendResponse(AmoebaResponseType.Result, id, serviceManager.Config);
                                        break;
                                    }
                                case AmoebaRequestType.SetConfig:
                                    {
                                        var config = JsonUtils.Load<ServiceConfig>(requestStream);
                                        serviceManager.SetConfig(config);
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.SetCloudLocations:
                                    {
                                        var cloudLocations = JsonUtils.Load<Location[]>(requestStream);
                                        serviceManager.SetCloudLocations(cloudLocations);
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.GetSize:
                                    {
                                        SendResponse(AmoebaResponseType.Result, id, serviceManager.Size);
                                        break;
                                    }
                                case AmoebaRequestType.Resize:
                                    {
                                        long size = JsonUtils.Load<long>(requestStream);
                                        serviceManager.Resize(size).Wait();
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.CheckBlocks:
                                    {
                                        try
                                        {
                                            serviceManager.CheckBlocks(new Progress<CheckBlocksProgressReport>((report) =>
                                            {
                                                SendResponse(AmoebaResponseType.Output, id, report);
                                            }), token).Wait();
                                        }
                                        catch (Exception)
                                        {

                                        }

                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.AddContent:
                                    {
                                        string path = JsonUtils.Load<string>(requestStream);
                                        var result = serviceManager.AddContent(path, token).Result;
                                        SendResponse(AmoebaResponseType.Result, id, result);
                                        break;
                                    }
                                case AmoebaRequestType.RemoveContent:
                                    {
                                        string path = JsonUtils.Load<string>(requestStream);
                                        serviceManager.RemoveContent(path);
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.AddDownload:
                                    {
                                        var arguments = JsonUtils.Load<(Metadata, string, long)>(requestStream);
                                        serviceManager.AddDownload(arguments.Item1, arguments.Item2, arguments.Item3);
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.RemoveDownload:
                                    {
                                        var arguments = JsonUtils.Load<(Metadata, string)>(requestStream);
                                        serviceManager.RemoveDownload(arguments.Item1, arguments.Item2);
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.ResetDownload:
                                    {
                                        var arguments = JsonUtils.Load<(Metadata, string)>(requestStream);
                                        serviceManager.ResetDownload(arguments.Item1, arguments.Item2);
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.SetProfile:
                                    {
                                        var arguments = JsonUtils.Load<(Profile, DigitalSignature)>(requestStream);
                                        serviceManager.SetProfile(arguments.Item1, arguments.Item2, token).Wait();
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.SetStore:
                                    {
                                        var arguments = JsonUtils.Load<(Store, DigitalSignature)>(requestStream);
                                        serviceManager.SetStore(arguments.Item1, arguments.Item2, token).Wait();
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.SetMailMessage:
                                    {
                                        var arguments = JsonUtils.Load<(Signature, MailMessage, ExchangePublicKey, DigitalSignature)>(requestStream);
                                        serviceManager.SetMailMessage(arguments.Item1, arguments.Item2, arguments.Item3, arguments.Item4, token).Wait();
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.SetChatMessage:
                                    {
                                        var arguments = JsonUtils.Load<(Tag, ChatMessage, DigitalSignature, TimeSpan)>(requestStream);
                                        var minier = new Miner(CashAlgorithm.Version1, -1, arguments.Item4);

                                        serviceManager.SetChatMessage(arguments.Item1, arguments.Item2, arguments.Item3, minier, token).Wait();
                                        SendResponse(AmoebaResponseType.Result, id, (object)null);
                                        break;
                                    }
                                case AmoebaRequestType.GetProfile:
                                    {
                                        var signature = JsonUtils.Load<Signature>(requestStream);
                                        var result = serviceManager.GetProfile(signature).Result;
                                        SendResponse(AmoebaResponseType.Result, id, result);
                                        break;
                                    }
                                case AmoebaRequestType.GetStore:
                                    {
                                        var signature = JsonUtils.Load<Signature>(requestStream);
                                        var result = serviceManager.GetStore(signature).Result;
                                        SendResponse(AmoebaResponseType.Result, id, result);
                                        break;
                                    }
                                case AmoebaRequestType.GetMailMessages:
                                    {
                                        var arguments = JsonUtils.Load<(Signature, ExchangePrivateKey)>(requestStream);
                                        var result = serviceManager.GetMailMessages(arguments.Item1, arguments.Item2).Result;
                                        SendResponse(AmoebaResponseType.Result, id, result);
                                        break;
                                    }
                                case AmoebaRequestType.GetChatMessages:
                                    {
                                        var tag = JsonUtils.Load<Tag>(requestStream);
                                        var result = serviceManager.GetChatMessages(tag).Result;
                                        SendResponse(AmoebaResponseType.Result, id, result);
                                        break;
                                    }
                            }
                        }
                        catch (Exception e)
                        {
                            SendResponse(AmoebaResponseType.Error, id, e.Message);
                        }
                        finally
                        {
                            _tasks.Remove(id);
                        }
                    });

                    _tasks.Add(id, responseTask);
                    responseTask.Start();
                }
            }

            void SendResponse<T>(AmoebaResponseType type, int id, T value)
            {
                using (var writer = new ItemStreamWriter(_bufferManager))
                {
                    writer.Write((uint)type);
                    writer.Write((uint)id);

                    Stream valueStream = null;

                    if (value != null)
                    {
                        try
                        {
                            valueStream = new BufferStream(_bufferManager);
                            JsonUtils.Save(valueStream, value);
                        }
                        catch (Exception)
                        {
                            if (valueStream != null)
                            {
                                valueStream.Dispose();
                                valueStream = null;
                            }

                            return;
                        }
                    }

                    sendAction(new UniteStream(writer.GetStream(), valueStream));
                }
            }
        }

        public class ResponseTask
        {
            private CancellationTokenSource _tokenSource;
            private Task _task;

            private ResponseTask(Action<CancellationToken> action)
            {
                _tokenSource = new CancellationTokenSource();
                _task = new Task(() => action(_tokenSource.Token));
            }

            public static ResponseTask Create(Action<CancellationToken> action)
            {
                return new ResponseTask(action);
            }

            public void Start()
            {
                _task.Start();
            }

            public void Stop()
            {
                _tokenSource.Cancel();
                _task.Wait();
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
