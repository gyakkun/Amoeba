using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Amoeba.Windows;
using Library;
using Library.Collections;
using Library.Net.Amoeba;

namespace Amoeba
{
    class TransfarLimitManager : StateManagerBase, Library.Configuration.ISettings
    {
        private AmoebaManager _amoebaManager;

        private Settings _settings;

        private long _uploadSize;
        private long _downloadSize;

        private Thread _watchThread;

        private volatile ManagerState _state = ManagerState.Stop;

        public EventHandler _startEvent;
        public EventHandler _stopEvent;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public TransfarLimitManager(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            _settings = new Settings(_thisLock);
        }

        public event EventHandler StartEvent
        {
            add
            {
                lock (_thisLock)
                {
                    _startEvent += value;
                }
            }
            remove
            {
                lock (_thisLock)
                {
                    _startEvent -= value;
                }
            }
        }

        public event EventHandler StopEvent
        {
            add
            {
                lock (_thisLock)
                {
                    _stopEvent += value;
                }
            }
            remove
            {
                lock (_thisLock)
                {
                    _stopEvent -= value;
                }
            }
        }

        public TransferLimit TransferLimit
        {
            get
            {
                lock (_thisLock)
                {
                    return _settings.TransferLimit;
                }
            }
            set
            {
                lock (_thisLock)
                {
                    _settings.TransferLimit = value;
                }
            }
        }

        public long TotalUploadSize
        {
            get
            {
                lock (_thisLock)
                {
                    return _settings.UploadTransferSizeList.Sum(n => n.Value);
                }
            }
        }

        public long TotalDownloadSize
        {
            get
            {
                lock (_thisLock)
                {
                    return _settings.DownloadTransferSizeList.Sum(n => n.Value);
                }
            }
        }

        protected virtual void OnStartEvent()
        {
            _startEvent?.Invoke(this, new EventArgs());
        }

        protected virtual void OnStopEvent()
        {
            _stopEvent?.Invoke(this, new EventArgs());
        }

        public void Reset()
        {
            lock (_thisLock)
            {
                _settings.UploadTransferSizeList.Clear();
                _settings.DownloadTransferSizeList.Clear();

                _uploadSize = -_amoebaManager.SentByteCount;
                _downloadSize = -_amoebaManager.ReceivedByteCount;
            }
        }

        private void WatchThread()
        {
            try
            {
                var stopwatch = new Stopwatch();

                var now = DateTime.Today;

                lock (_thisLock)
                {
                    foreach (var item in _settings.UploadTransferSizeList.ToArray())
                    {
                        if ((now - item.Key).TotalDays >= _settings.TransferLimit.Span)
                        {
                            _settings.UploadTransferSizeList.Remove(item.Key);
                        }
                    }

                    foreach (var item in _settings.DownloadTransferSizeList.ToArray())
                    {
                        if ((now - item.Key).TotalDays >= _settings.TransferLimit.Span)
                        {
                            _settings.DownloadTransferSizeList.Remove(item.Key);
                        }
                    }
                }

                for (;;)
                {
                    Thread.Sleep(1000 * 1);
                    if (this.State == ManagerState.Stop) return;

                    if (!stopwatch.IsRunning || stopwatch.ElapsedMilliseconds > 1000 * 20)
                    {
                        stopwatch.Restart();

                        if (now != DateTime.Today)
                        {
                            now = DateTime.Today;

                            lock (_thisLock)
                            {
                                foreach (var item in _settings.UploadTransferSizeList.ToArray())
                                {
                                    if ((now - item.Key).TotalDays >= _settings.TransferLimit.Span)
                                    {
                                        _settings.UploadTransferSizeList.Remove(item.Key);
                                    }
                                }

                                foreach (var item in _settings.DownloadTransferSizeList.ToArray())
                                {
                                    if ((now - item.Key).TotalDays >= _settings.TransferLimit.Span)
                                    {
                                        _settings.DownloadTransferSizeList.Remove(item.Key);
                                    }
                                }

                                _uploadSize = -_amoebaManager.SentByteCount;
                                _downloadSize = -_amoebaManager.ReceivedByteCount;
                            }

                            if (_amoebaManager.State == ManagerState.Stop) this.OnStartEvent();
                        }
                        else
                        {
                            lock (_thisLock)
                            {
                                _settings.UploadTransferSizeList[now] = _uploadSize + _amoebaManager.SentByteCount;
                                _settings.DownloadTransferSizeList[now] = _downloadSize + _amoebaManager.ReceivedByteCount;
                            }
                        }

                        if (_settings.TransferLimit.Type == TransferLimitType.Uploads)
                        {
                            var totalUploadSize = _settings.UploadTransferSizeList.Sum(n => n.Value);

                            if (totalUploadSize > _settings.TransferLimit.Size)
                            {
                                if (_amoebaManager.State == ManagerState.Start) this.OnStopEvent();
                            }
                        }
                        else if (_settings.TransferLimit.Type == TransferLimitType.Downloads)
                        {
                            var totalDownloadSize = _settings.DownloadTransferSizeList.Sum(n => n.Value);

                            if (totalDownloadSize > _settings.TransferLimit.Size)
                            {
                                if (_amoebaManager.State == ManagerState.Start) this.OnStopEvent();
                            }
                        }
                        else if (_settings.TransferLimit.Type == TransferLimitType.Total)
                        {
                            var totalUploadSize = _settings.UploadTransferSizeList.Sum(n => n.Value);
                            var totalDownloadSize = _settings.DownloadTransferSizeList.Sum(n => n.Value);

                            if ((totalUploadSize + totalDownloadSize) > _settings.TransferLimit.Size)
                            {
                                if (_amoebaManager.State == ManagerState.Start) this.OnStopEvent();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public override ManagerState State
        {
            get
            {
                return _state;
            }
        }

        private readonly object _stateLock = new object();

        public override void Start()
        {
            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Start) return;
                    _state = ManagerState.Start;

                    _watchThread = new Thread(this.WatchThread);
                    _watchThread.Priority = ThreadPriority.Lowest;
                    _watchThread.Name = "TransfarLimitManager_WatchThread";
                    _watchThread.Start();
                }
            }
        }

        public override void Stop()
        {
            lock (_stateLock)
            {
                lock (_thisLock)
                {
                    if (this.State == ManagerState.Stop) return;
                    _state = ManagerState.Stop;
                }

                _watchThread.Join();
                _watchThread = null;
            }
        }

        #region ISettings

        public void Load(string directoryPath)
        {
            lock (_thisLock)
            {
                _settings.Load(directoryPath);

                var now = DateTime.Today;

                _settings.UploadTransferSizeList.TryGetValue(now, out _uploadSize);
                _settings.DownloadTransferSizeList.TryGetValue(now, out _downloadSize);
            }
        }

        public void Save(string directoryPath)
        {
            lock (_thisLock)
            {
                _settings.Save(directoryPath);
            }
        }

        #endregion

        private class Settings : Library.Configuration.SettingsBase
        {
            private object _thisLock;

            public Settings(object lockObject)
                : base(new List<Library.Configuration.ISettingContent>() {
                new Library.Configuration.SettingContent<TransferLimit>() { Name = "TransferLimit", Value = new TransferLimit() },
                new Library.Configuration.SettingContent<LockedHashDictionary<DateTime, long>>() { Name = "UploadTransferSizeList", Value = new LockedHashDictionary<DateTime, long>() },
                new Library.Configuration.SettingContent<LockedHashDictionary<DateTime, long>>() { Name = "DownloadTransferSizeList", Value = new LockedHashDictionary<DateTime, long>() },
                })
            {
                _thisLock = lockObject;
            }

            public override void Load(string directoryPath)
            {
                lock (_thisLock)
                {
                    base.Load(directoryPath);
                }
            }

            public override void Save(string directoryPath)
            {
                lock (_thisLock)
                {
                    base.Save(directoryPath);
                }
            }

            public TransferLimit TransferLimit
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (TransferLimit)this["TransferLimit"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["TransferLimit"] = value;
                    }
                }
            }

            public LockedHashDictionary<DateTime, long> UploadTransferSizeList
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (LockedHashDictionary<DateTime, long>)this["UploadTransferSizeList"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["UploadTransferSizeList"] = value;
                    }
                }
            }

            public LockedHashDictionary<DateTime, long> DownloadTransferSizeList
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (LockedHashDictionary<DateTime, long>)this["DownloadTransferSizeList"];
                    }
                }
                set
                {
                    lock (_thisLock)
                    {
                        this["DownloadTransferSizeList"] = value;
                    }
                }
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

    [DataContract(Name = "TransferLimitType")]
    enum TransferLimitType
    {
        [EnumMember(Value = "None")]
        None,

        [EnumMember(Value = "Uploads")]
        Uploads,

        [EnumMember(Value = "Downloads")]
        Downloads,

        [EnumMember(Value = "Total")]
        Total,
    }

    [DataContract(Name = "TransferLimit")]
    class TransferLimit : IThisLock
    {
        private TransferLimitType _type = TransferLimitType.None;
        private int _span = 1;
        private long _size = 1024 * 1024;

        private static readonly object _initializeLock = new object();
        private volatile object _thisLock;

        [DataMember(Name = "Type")]
        public TransferLimitType Type
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _type;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _type = value;
                }
            }
        }

        [DataMember(Name = "Span")]
        public int Span
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _span;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _span = value;
                }
            }
        }

        [DataMember(Name = "Size")]
        public long Size
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _size;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _size = value;
                }
            }
        }

        #region IThisLock

        public object ThisLock
        {
            get
            {
                if (_thisLock == null)
                {
                    lock (_initializeLock)
                    {
                        if (_thisLock == null)
                        {
                            _thisLock = new object();
                        }
                    }
                }

                return _thisLock;
            }
        }

        #endregion
    }
}
