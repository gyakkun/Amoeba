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
    class TransfarLimitManager : StateManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private AmoebaManager _amoebaManager;

        private Settings _settings;

        private long _uploadSize;
        private long _downloadSize;

        private volatile Thread _timerThread;

        private ManagerState _state = ManagerState.Stop;

        public EventHandler _startEvent;
        public EventHandler _stopEvent;

        private volatile bool _disposed;
        private readonly object _thisLock = new object();

        public TransfarLimitManager(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            _settings = new Settings(this.ThisLock);
        }

        public event EventHandler StartEvent
        {
            add
            {
                lock (this.ThisLock)
                {
                    _startEvent += value;
                }
            }
            remove
            {
                lock (this.ThisLock)
                {
                    _startEvent -= value;
                }
            }
        }

        public event EventHandler StopEvent
        {
            add
            {
                lock (this.ThisLock)
                {
                    _stopEvent += value;
                }
            }
            remove
            {
                lock (this.ThisLock)
                {
                    _stopEvent -= value;
                }
            }
        }

        public TransferLimit TransferLimit
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _settings.TransferLimit;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _settings.TransferLimit = value;
                }
            }
        }

        public long TotalUploadSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _settings.UploadTransferSizeList.Sum(n => n.Value);
                }
            }
        }

        public long TotalDownloadSize
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _settings.DownloadTransferSizeList.Sum(n => n.Value);
                }
            }
        }

        protected virtual void OnStartEvent()
        {
            if (_startEvent != null)
            {
                _startEvent(this, new EventArgs());
            }
        }

        protected virtual void OnStopEvent()
        {
            if (_stopEvent != null)
            {
                _stopEvent(this, new EventArgs());
            }
        }

        public void Reset()
        {
            lock (this.ThisLock)
            {
                _settings.UploadTransferSizeList.Clear();
                _settings.DownloadTransferSizeList.Clear();

                _uploadSize = -_amoebaManager.SentByteCount;
                _downloadSize = -_amoebaManager.ReceivedByteCount;
            }
        }

        private void Timer()
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();

                var now = DateTime.Today;

                lock (this.ThisLock)
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

                for (; ; )
                {
                    Thread.Sleep(1000 * 1);
                    if (this.State == ManagerState.Stop) return;

                    if (!stopwatch.IsRunning || stopwatch.ElapsedMilliseconds > 1000 * 20)
                    {
                        stopwatch.Restart();

                        if (now != DateTime.Today)
                        {
                            now = DateTime.Today;

                            lock (this.ThisLock)
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
                            lock (this.ThisLock)
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
                lock (this.ThisLock)
                {
                    return _state;
                }
            }
        }

        public override void Start()
        {
            while (_timerThread != null) Thread.Sleep(1000);

            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Start) return;
                _state = ManagerState.Start;

                _timerThread = new Thread(this.Timer);
                _timerThread.Priority = ThreadPriority.Lowest;
                _timerThread.Name = "TransfarLimitManager_Timer";
                _timerThread.Start();
            }
        }

        public override void Stop()
        {
            lock (this.ThisLock)
            {
                if (this.State == ManagerState.Stop) return;
                _state = ManagerState.Stop;
            }

            _timerThread.Join();
            _timerThread = null;
        }

        #region ISettings

        public void Load(string directoryPath)
        {
            lock (this.ThisLock)
            {
                _settings.Load(directoryPath);

                var now = DateTime.Today;

                _settings.UploadTransferSizeList.TryGetValue(now, out _uploadSize);
                _settings.DownloadTransferSizeList.TryGetValue(now, out _downloadSize);
            }
        }

        public void Save(string directoryPath)
        {
            lock (this.ThisLock)
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
                new Library.Configuration.SettingContent<LockedDictionary<DateTime, long>>() { Name = "UploadTransferSizeList", Value = new LockedDictionary<DateTime, long>() },
                new Library.Configuration.SettingContent<LockedDictionary<DateTime, long>>() { Name = "DownloadTransferSizeList", Value = new LockedDictionary<DateTime, long>() },
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

            public LockedDictionary<DateTime, long> UploadTransferSizeList
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (LockedDictionary<DateTime, long>)this["UploadTransferSizeList"];
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

            public LockedDictionary<DateTime, long> DownloadTransferSizeList
            {
                get
                {
                    lock (_thisLock)
                    {
                        return (LockedDictionary<DateTime, long>)this["DownloadTransferSizeList"];
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

        #region IThisLock

        public object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        #endregion
    }

    [DataContract(Name = "TransferLimitType", Namespace = "http://Amoeba")]
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

    [DataContract(Name = "TransferLimit", Namespace = "http://Amoeba")]
    class TransferLimit : IThisLock
    {
        private TransferLimitType _type = TransferLimitType.None;
        private int _span = 1;
        private long _size = 1024 * 1024;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

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
