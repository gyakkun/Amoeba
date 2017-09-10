using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Amoeba.Messages;
using Amoeba.Rpc;
using Omnius.Base;

namespace Amoeba.Interface
{
    class WatchManager : ManagerBase
    {
        private AmoebaClientManager _serviceManager;

        private WatchTimer _checkUpdateTimer;
        private WatchTimer _checkDiskSpaceTimer;
        private WatchTimer _backupTimer;

        public event Action SaveEvent;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public WatchManager(AmoebaClientManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Setting_ChechUpdate();
            this.Setting_Backup();
        }

        private void Setting_ChechUpdate()
        {
            _checkUpdateTimer = new WatchTimer(() =>
            {
                try
                {
                    var updateInfo = SettingsManager.Instance.UpdateInfo;
                    if (!updateInfo.IsEnabled) return;

                    var store = _serviceManager.GetStore(updateInfo.Signature);
                    if (store == null) return;

                    var updateBox = store.Value.Boxes.FirstOrDefault(n => n.Name == "Update")?.Boxes.FirstOrDefault(n => n.Name == "Windows");
                    if (updateBox == null) return;

                    Seed targetSeed = null;

                    {
                        var map = new Dictionary<Seed, Version>();
                        var regex = new Regex(@"Amoeba.+?((\d*)\.(\d*)\.(\d*)).*?\.zip", RegexOptions.Compiled);

                        foreach (var seed in updateBox.Seeds)
                        {
                            var match = regex.Match(seed.Name);
                            if (!match.Success) continue;

                            var version = new Version(match.Groups[1].Value);
                            if (version <= AmoebaEnvironment.Version) continue;

                            map.Add(seed, version);
                        }

                        if (map.Count > 0)
                        {
                            var sortedList = map.ToList();
                            sortedList.Sort((x, y) => y.Value.CompareTo(x.Value));

                            targetSeed = sortedList.First().Key;
                        }
                    }

                    if (targetSeed == null) return;

                    string fullPath = Path.GetFullPath(Path.Combine(AmoebaEnvironment.Paths.UpdatePath, targetSeed.Name));
                    if (File.Exists(fullPath)) return;

                    var downloadItemInfo = new DownloadItemInfo(targetSeed, fullPath);
                    SettingsManager.Instance.DownloadItemInfos.Add(downloadItemInfo);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            });
            _checkUpdateTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 3, 0));
        }

        private void Setting_Backup()
        {
            var sw = Stopwatch.StartNew();

            _backupTimer = new WatchTimer(() =>
            {
                if (sw.Elapsed.TotalMinutes > 30)
                {
                    sw.Restart();

                    try
                    {
                        this.SaveEvent?.Invoke();
                        this.GarbageCollect();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            });
            _backupTimer.Start(new TimeSpan(0, 0, 30));
        }

        private void GarbageCollect()
        {
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _checkUpdateTimer.Stop();
                _checkUpdateTimer.Dispose();

                _checkDiskSpaceTimer.Stop();
                _checkDiskSpaceTimer.Dispose();

                _backupTimer.Stop();
                _backupTimer.Dispose();
            }
        }
    }
}
