using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
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

        public event EventHandler StartEvent;
        public event EventHandler StopEvent;

        private readonly object _thisLock = new object();
        private volatile bool _disposed;

        public TransfarLimitManager(AmoebaManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            _settings = new Settings();
        }

        protected virtual void OnStartEvent()
        {
            this.StartEvent?.Invoke(this, new EventArgs());
        }

        protected virtual void OnStopEvent()
        {
            this.StopEvent?.Invoke(this, new EventArgs());
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
        }

        public void SetTransferLimit(TransferLimit transferLimit)
        {
            lock (_thisLock)
            {
                _settings.TransferLimit = transferLimit;

                this.GarbageCollection();
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

        private void GarbageCollection()
        {
            lock (_thisLock)
            {
                var now = DateTime.UtcNow;

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
                var now = DateTime.Today;

                var stopwatch = new Stopwatch();

                for (;;)
                {
                    Thread.Sleep(1000 * 1);
                    if (this.State == ManagerState.Stop) return;

                    if (!stopwatch.IsRunning || stopwatch.ElapsedMilliseconds > 1000 * 20)
                    {
                        stopwatch.Restart();

                        TransferLimit transferLimit;

                        lock (_thisLock)
                        {
                            transferLimit = _settings.TransferLimit;
                        }

                        if (now != DateTime.Today)
                        {
                            lock (_thisLock)
                            {
                                this.GarbageCollection();

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

                        if (transferLimit.Type == TransferLimitType.Uploads)
                        {
                            var totalUploadSize = _settings.UploadTransferSizeList.Sum(n => n.Value);

                            if (totalUploadSize > transferLimit.Size)
                            {
                                if (_amoebaManager.State == ManagerState.Start) this.OnStopEvent();
                            }
                        }
                        else if (transferLimit.Type == TransferLimitType.Downloads)
                        {
                            var totalDownloadSize = _settings.DownloadTransferSizeList.Sum(n => n.Value);

                            if (totalDownloadSize > transferLimit.Size)
                            {
                                if (_amoebaManager.State == ManagerState.Start) this.OnStopEvent();
                            }
                        }
                        else if (transferLimit.Type == TransferLimitType.Total)
                        {
                            var totalUploadSize = _settings.UploadTransferSizeList.Sum(n => n.Value);
                            var totalDownloadSize = _settings.DownloadTransferSizeList.Sum(n => n.Value);

                            if ((totalUploadSize + totalDownloadSize) > transferLimit.Size)
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
            public Settings()
                : base(new List<Library.Configuration.ISettingContent>() {
                new Library.Configuration.SettingContent<TransferLimit>() { Name = "TransferLimit", Value = new TransferLimit(TransferLimitType.None, 1, (long)1024 * 1024 * 1024 * 32) },
                new Library.Configuration.SettingContent<LockedHashDictionary<DateTime, long>>() { Name = "UploadTransferSizeList", Value = new LockedHashDictionary<DateTime, long>() },
                new Library.Configuration.SettingContent<LockedHashDictionary<DateTime, long>>() { Name = "DownloadTransferSizeList", Value = new LockedHashDictionary<DateTime, long>() },
                })
            {

            }

            public TransferLimit TransferLimit
            {
                get
                {
                    return (TransferLimit)this["TransferLimit"];
                }
                set
                {
                    this["TransferLimit"] = value;
                }
            }

            public LockedHashDictionary<DateTime, long> UploadTransferSizeList
            {
                get
                {
                    return (LockedHashDictionary<DateTime, long>)this["UploadTransferSizeList"];
                }
                set
                {
                    this["UploadTransferSizeList"] = value;
                }
            }

            public LockedHashDictionary<DateTime, long> DownloadTransferSizeList
            {
                get
                {
                    return (LockedHashDictionary<DateTime, long>)this["DownloadTransferSizeList"];
                }
                set
                {
                    this["DownloadTransferSizeList"] = value;
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
    public enum TransferLimitType
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
    public class TransferLimit
    {
        private TransferLimitType _type;
        private int _span;
        private long _size;

        public TransferLimit(TransferLimitType type, int span, long size)
        {
            _type = type;
            _span = span;
            _size = size;
        }

        [DataMember(Name = "Type")]
        public TransferLimitType Type
        {
            get
            {
                return _type;
            }
        }

        [DataMember(Name = "Span")]
        public int Span
        {
            get
            {
                return _span;
            }
        }

        [DataMember(Name = "Size")]
        public long Size
        {
            get
            {
                return _size;
            }
        }
    }
}
