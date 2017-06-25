using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Utilities;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    class ControlManager : ManagerBase
    {
        private ServiceManager _serviceManager;

        private WatchTimer _checkUpdateTimer;
        private WatchTimer _checkDiskSpaceTimer;
        private WatchTimer _backupTimer;

        private readonly object _lockObject = new object();
        private volatile bool _disposed;

        public ControlManager(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Setting_ChechUpdate();
            this.Setting_CheckDiskSpace();
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

                    var store = _serviceManager.GetStore(updateInfo.Signature).Result;
                    if (store == null) return;

                    var updateBox = store.Value.Boxes.FirstOrDefault(n => n.Name == "Update")?.Boxes.FirstOrDefault(n => n.Name == "Windows");
                    if (updateBox == null) return;

                    Seed targetSeed = null;

                    {
                        var map = new Dictionary<Seed, Version>();
                        var regex = new Regex(@"Amoeba ((\d*)\.(\d*)\.(\d*))\.zip", RegexOptions.Compiled);

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
                            sortedList.Sort((x, y) => y.Value.CompareTo(x));

                            targetSeed = sortedList.First().Key;
                        }
                    }

                    if (targetSeed == null) return;

                    var downloadItemInfo = new DownloadItemInfo(targetSeed, Path.Combine(AmoebaEnvironment.Paths.UpdatePath, targetSeed.Name));
                    SettingsManager.Instance.DownloadItemInfos.Add(downloadItemInfo);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            });
            _checkUpdateTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 3, 0));
        }

        private void Setting_CheckDiskSpace()
        {
            _checkDiskSpaceTimer = new WatchTimer(() =>
            {
                var paths = new List<string>();
                paths.Add(AmoebaEnvironment.Config.Cache.BlocksPath);

                bool flag = false;

                foreach (string path in paths)
                {
                    var drive = new DriveInfo(Path.GetFullPath(path));

                    if (drive.AvailableFreeSpace < NetworkConverter.FromSizeString("256MB"))
                    {
                        flag |= true;
                        break;
                    }
                }

                if (_serviceManager.Information.GetValue<long>("Cache_FreeSpace") < NetworkConverter.FromSizeString("1024MB"))
                {
                    flag |= true;
                }

                if (!flag)
                {
                    if (_serviceManager.State == ManagerState.Stop)
                    {
                        _serviceManager.Start();
                        Log.Information("Start");
                    }
                }
                else
                {
                    if (_serviceManager.State == ManagerState.Start)
                    {
                        _serviceManager.Stop();
                        Log.Information("Stop");

                        App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var viewModel = new ConfirmWindowViewModel(LanguagesManager.Instance.MainWindow_DiskSpaceNotFound_Message);

                            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                                .Publish(viewModel);
                        });
                    }
                }
            });
            _checkDiskSpaceTimer.Start(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 30));
        }

        private void Setting_Backup()
        {
            var sw = Stopwatch.StartNew();

            _backupTimer = new WatchTimer(() =>
            {
                if ((!Process.GetCurrentProcess().IsActivated() && sw.Elapsed.TotalMinutes > 30)
                    || sw.Elapsed.TotalHours > 3)
                {
                    sw.Restart();

                    Backup.Instance.Run();
                    this.GarbageCollect();
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
