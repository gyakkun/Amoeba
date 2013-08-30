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
    class TransfarLimitManager : ManagerBase, Library.Configuration.ISettings, IThisLock
    {
        private AmoebaManager _amoebaManager;

        private Settings _settings;

        private bool _isRun = true;

        private Thread _timerThread = null;

        public event EventHandler StartEvent;
        public event EventHandler StopEvent;

        private object _thisLock = new object();
        private bool _disposed = false;

        public TransfarLimitManager(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            _settings = new Settings(this.ThisLock);

            _timerThread = new Thread(this.Timer);
            _timerThread.Priority = ThreadPriority.Highest;
            _timerThread.IsBackground = true;
            _timerThread.Name = "TransfarLimitManager_TimerThread";
            _timerThread.Start();
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
            if (this.StartEvent != null)
            {
                this.StartEvent(this, new EventArgs());
            }
        }

        protected virtual void OnStopEvent()
        {
            if (this.StopEvent != null)
            {
                this.StopEvent(this, new EventArgs());
            }
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

        private long _uploadSize;
        private long _downloadSize;

        private void Timer()
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();

                var now = DateTime.Today;

                lock (_thisLock)
                {
                    _settings.UploadTransferSizeList.TryGetValue(now, out _uploadSize);
                    _settings.DownloadTransferSizeList.TryGetValue(now, out _downloadSize);
                }

                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (!_isRun) return;

                    if (!stopwatch.IsRunning || stopwatch.Elapsed > new TimeSpan(0, 1, 0))
                    {
                        stopwatch.Restart();

                        if (now != DateTime.Today)
                        {
                            now = DateTime.Today;

                            lock (_thisLock)
                            {
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

                        if (_settings.TransferLimit.Type != TransferLimitType.None)
                        {
                            foreach (var item in _settings.UploadTransferSizeList.ToArray())
                            {
                                if ((now - item.Key).TotalDays >= _settings.TransferLimit.Span)
                                    _settings.UploadTransferSizeList.Remove(item.Key);
                            }

                            foreach (var item in _settings.DownloadTransferSizeList.ToArray())
                            {
                                if ((now - item.Key).TotalDays >= _settings.TransferLimit.Span)
                                    _settings.DownloadTransferSizeList.Remove(item.Key);
                            }

                            if (_settings.TransferLimit.Type == TransferLimitType.Uploads)
                            {
                                var totalUploadSize = _settings.UploadTransferSizeList.Sum(n => n.Value);

                                if (totalUploadSize > _settings.TransferLimit.Size)
                                {
                                    if (_amoebaManager.State == ManagerState.Start) this.OnStopEvent();
                                }
                            }

                            if (_settings.TransferLimit.Type == TransferLimitType.Downloads)
                            {
                                var totalDownloadSize = _settings.DownloadTransferSizeList.Sum(n => n.Value);

                                if (totalDownloadSize > _settings.TransferLimit.Size)
                                {
                                    if (_amoebaManager.State == ManagerState.Start) this.OnStopEvent();
                                }
                            }

                            if (_settings.TransferLimit.Type == TransferLimitType.Total)
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
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        #region ISettings

        public void Load(string directoryPath)
        {
            lock (_thisLock)
            {
                _settings.Load(directoryPath);
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
                : base(new List<Library.Configuration.ISettingsContext>() { 
                new Library.Configuration.SettingsContext<TransferLimit>() { Name = "TransferLimit", Value = new TransferLimit() },
                new Library.Configuration.SettingsContext<LockedDictionary<DateTime, long>>() { Name = "UploadTransferSizeList", Value = new LockedDictionary<DateTime, long>() },
                new Library.Configuration.SettingsContext<LockedDictionary<DateTime, long>>() { Name = "DownloadTransferSizeList", Value = new LockedDictionary<DateTime, long>() },
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

            if (disposing)
            {
                _isRun = false;

                _timerThread.Join();
            }

            _disposed = true;
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

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

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
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }
}
