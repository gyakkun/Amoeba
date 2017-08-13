using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Net.Amoeba;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UploadControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;
        private TaskManager _uploadTaskManager;

        private Settings _settings;

        public FileDragAcceptDescription FileDragAcceptDescription { get; private set; }

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents.Values);
        private ObservableSimpleDictionary<string, UploadListViewItemInfo> _contents = new ObservableSimpleDictionary<string, UploadListViewItemInfo>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand SyncCommand { get; private set; }

        public ReactiveCommand AddCommand { get; private set; }
        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand CopyCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private readonly object _lockObject = new object();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public UploadControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            _uploadTaskManager = new TaskManager(this.UploadThread);
            _uploadTaskManager.Start();

            this.FileDragAcceptDescription = new FileDragAcceptDescription() { Effects = DragDropEffects.Move };
            this.FileDragAcceptDescription.DragDrop += this.FileDragAcceptDescription_DragDrop;
        }

        private async void FileDragAcceptDescription_DragDrop(FileDragAcceptEventArgs args)
        {
            var filePaths = new HashSet<string>();

            await Task.Run(() =>
            {
                foreach (string path in args.Paths)
                {
                    if (File.Exists(path)) filePaths.Add(path);
                    else if (Directory.Exists(path)) filePaths.UnionWith(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
                }

                foreach (string path in _serviceManager.GetContentInformations().Select(n => n.GetValue<string>("Path")).ToArray())
                {
                    filePaths.Remove(path);
                }
            });

            var viewModel = new UploadPreviewWindowViewModel(filePaths);
            viewModel.Callback += (results) =>
            {
                foreach (string path in results)
                {
                    SettingsManager.Instance.UploadItemInfos.Add(new UploadItemInfo(path));
                }
            };

            Messenger.Instance.GetEvent<UploadPreviewWindowShowEvent>()
                .Publish(viewModel);
        }

        public void Init()
        {
            {
                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.SyncCommand = new ReactiveCommand().AddTo(_disposable);
                this.SyncCommand.Subscribe(() => this.Sync()).AddTo(_disposable);

                this.AddCommand = new ReactiveCommand().AddTo(_disposable);
                this.AddCommand.Subscribe(() => this.Add()).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(UploadControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.Sort(null);
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                var map = new Dictionary<string, Information>();

                foreach (var info in _serviceManager.GetContentInformations())
                {
                    map.Add(info.GetValue<string>("Path"), info);
                }

                var uploadItemInfos = new Dictionary<string, UploadItemInfo>();

                foreach (var item in SettingsManager.Instance.UploadItemInfos.ToArray())
                {
                    if (map.ContainsKey(item.Path))
                    {
                        SettingsManager.Instance.UploadItemInfos.Remove(item);
                        continue;
                    }

                    uploadItemInfos.Add(item.Path, item);
                }

                App.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;

                    foreach (string path in _contents.Keys.ToArray())
                    {
                        if (!uploadItemInfos.ContainsKey(path) && !map.ContainsKey(path))
                        {
                            _contents.Remove(path);
                        }
                    }

                    foreach (var (path, info) in map)
                    {
                        UploadListViewItemInfo viewModel;

                        if (!_contents.TryGetValue(path, out viewModel))
                        {
                            viewModel = new UploadListViewItemInfo();
                            _contents[path] = viewModel;
                        }

                        viewModel.Icon = IconUtils.GetImage(path);
                        viewModel.Name = Path.GetFileName(path);
                        viewModel.Length = info.GetValue<long>("Length");
                        viewModel.CreationTime = info.GetValue<DateTime>("CreationTime");
                        viewModel.Path = path;
                        viewModel.Seed = new Seed(viewModel.Name, viewModel.Length, viewModel.CreationTime, info.GetValue<Metadata>("Metadata"));
                    }

                    foreach (var (path, item) in uploadItemInfos)
                    {
                        UploadListViewItemInfo viewModel;

                        if (!_contents.TryGetValue(path, out viewModel))
                        {
                            viewModel = new UploadListViewItemInfo();
                            _contents[path] = viewModel;
                        }

                        viewModel.Name = Path.GetFileName(path);
                        viewModel.Path = path;
                    }

                    this.Sort();
                });

                if (token.WaitHandle.WaitOne(1000 * 3)) return;
            }
        }

        private void UploadThread(CancellationToken token)
        {
            var digitalSignature = SettingsManager.Instance.AccountInfo.DigitalSignature;
            if (digitalSignature == null) return;

            for (;;)
            {
                if (token.WaitHandle.WaitOne(1000 * 1)) return;

                var item = SettingsManager.Instance.UploadItemInfos.ToArray().FirstOrDefault();
                if (item == null) continue;

                try
                {
                    using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        var task = _serviceManager.Import(item.Path, linkedTokenSource.Token);

                        while (!task.IsCompleted)
                        {
                            Thread.Sleep(1000);

                            if (!SettingsManager.Instance.UploadItemInfos.Contains(item))
                            {
                                linkedTokenSource.Cancel();
                                break;
                            }
                        }

                        task.Wait();
                    }

                    SettingsManager.Instance.UploadItemInfos.Remove(item);
                }
                catch (TaskCanceledException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private void Sort(string propertyName)
        {
            var direction = ListSortDirection.Ascending;

            if (_sortInfo.PropertyName == propertyName)
            {
                if (_sortInfo.Direction == ListSortDirection.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    direction = ListSortDirection.Ascending;
                }
            }

            _sortInfo.Direction = direction;
            _sortInfo.PropertyName = propertyName;

            this.Sort();
        }

        private void Sort()
        {
            if (this.ContentsView.SortDescriptions.Count != 0) this.ContentsView.SortDescriptions.Clear();
            _contents.Sort((x, y) => this.Sort(x, y, _sortInfo.PropertyName, _sortInfo.Direction));
        }

        private int Sort(UploadListViewItemInfo x, UploadListViewItemInfo y, string propertyName, ListSortDirection direction)
        {
            int a = direction == ListSortDirection.Ascending ? 1 : -1;

            if (propertyName == "Name")
            {
                int c = a * x.Name.CompareTo(y.Name);
                return c;
            }
            else if (propertyName == "Length")
            {
                int c = a * x.Length.CompareTo(y.Length);
                if (c != 0) return c;
                c = a * x.Name.CompareTo(y.Name);
                return c;
            }
            else if (propertyName == "CreationTime")
            {
                int c = a * x.CreationTime.CompareTo(y.CreationTime);
                if (c != 0) return c;
                c = a * x.Name.CompareTo(y.Name);
                return c;
            }
            else if (propertyName == "Path")
            {
                int c = a * x.Path.CompareTo(y.Path);
                if (c != 0) return c;
                c = a * x.Name.CompareTo(y.Name);
                return c;
            }

            return 0;
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("SortInfo", _sortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        private void Sync()
        {
            foreach (string path in _serviceManager.GetContentInformations().Select(n => n.GetValue<string>("Path")))
            {
                if (!File.Exists(path))
                {
                    _serviceManager.RemoveContent(path);
                }
            }
        }

        private async void Add()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var filePaths = new HashSet<string>();

                await Task.Run(() =>
                {
                    foreach (string path in dialog.FileNames)
                    {
                        if (File.Exists(path)) filePaths.Add(path);
                    }

                    foreach (string path in _serviceManager.GetContentInformations().Select(n => n.GetValue<string>("Path")).ToArray())
                    {
                        filePaths.Remove(path);
                    }
                });

                var viewModel = new UploadPreviewWindowViewModel(filePaths);
                viewModel.Callback += (results) =>
                {
                    foreach (string path in results)
                    {
                        SettingsManager.Instance.UploadItemInfos.Add(new UploadItemInfo(path));
                    }
                };

                Messenger.Instance.GetEvent<UploadPreviewWindowShowEvent>()
                    .Publish(viewModel);
            }
        }

        private void Delete()
        {
            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                var selectedItems = new HashSet<string>(this.SelectedItems.OfType<UploadListViewItemInfo>()
                    .Select(n => n.Path));

                foreach (var item in SettingsManager.Instance.UploadItemInfos.ToArray())
                {
                    if (!selectedItems.Contains(item.Path)) continue;

                    SettingsManager.Instance.UploadItemInfos.Remove(item);
                }

                Task.Run(() =>
                {
                    foreach (string path in selectedItems)
                    {
                        _serviceManager.RemoveContent(path);
                    }
                });
            };

            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                .Publish(viewModel);
        }

        private void Copy()
        {
            var selectedItems = this.SelectedItems.OfType<UploadListViewItemInfo>()
                .Select(n => n.Seed).Where(n => n != null).ToList();

            Clipboard.SetSeeds(selectedItems);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
