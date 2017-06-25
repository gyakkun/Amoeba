using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class StorePublishControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        public ReactiveProperty<bool> IsProgressVisible { get; private set; }
        public RateInfo Rate { get; } = new RateInfo();

        public ICollectionView ContentsView => CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<PublishDirectoryInfo> _contents = new ObservableCollection<PublishDirectoryInfo>();
        public ReactiveProperty<PublishDirectoryInfo> SelectedItem { get; private set; }
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand UploadCommand { get; private set; }

        public ReactiveCommand AddCommand { get; private set; }
        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand EditCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private volatile PublishStoreInfo _publishStoreInfo;

        private readonly object _lockObject = new object();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StorePublishControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();
        }

        public void Init()
        {
            {
                this.IsProgressVisible = new ReactiveProperty<bool>(false).AddTo(_disposable);

                this.SelectedItem = new ReactiveProperty<PublishDirectoryInfo>().AddTo(_disposable);

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.UploadCommand = new ReactiveCommand().AddTo(_disposable);
                this.UploadCommand.Subscribe(() => this.StoreUpload()).AddTo(_disposable);

                this.AddCommand = new ReactiveCommand().AddTo(_disposable);
                this.AddCommand.Subscribe(() => this.DirectoryAdd()).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.DirectoryDelete()).AddTo(_disposable);

                this.EditCommand = this.SelectedItem.Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.EditCommand.Subscribe(() => this.DirectoryEdit()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StorePublishControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _contents.AddRange(_settings.Load("PublishDirectoryInfos", () => Array.Empty<PublishDirectoryInfo>()));
                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));

                _publishStoreInfo = _settings.Load<PublishStoreInfo>("PublishStoreInfo", () => null);
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
                if (token.WaitHandle.WaitOne(1000 * 3)) return;

                var targetPublishStoreInfo = _publishStoreInfo;

                if (targetPublishStoreInfo == null)
                {
                    try
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            this.IsProgressVisible.Value = false;
                        }, DispatcherPriority.Background, token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }

                    continue;
                }

                // Remove
                {
                    var hashMap = new HashSet<string>();
                    hashMap.UnionWith(_serviceManager.GetContentInformations().Select(n => n.GetValue<string>("Path")));
                    hashMap.ExceptWith(targetPublishStoreInfo.Map.SelectMany(n => n.Value.SelectMany(m => m.Value)));

                    foreach (string path in hashMap)
                    {
                        if (token.IsCancellationRequested) return;
                        if (targetPublishStoreInfo != _publishStoreInfo) goto End;

                        _serviceManager.RemoveContent(path);
                    }
                }

                // Add
                {
                    var hashMap = new HashSet<string>();
                    hashMap.UnionWith(targetPublishStoreInfo.Map.SelectMany(n => n.Value.SelectMany(m => m.Value)));
                    hashMap.ExceptWith(_serviceManager.GetContentInformations().Select(n => n.GetValue<string>("Path")));

                    foreach (var (path, i) in hashMap.Select((n, i) => (n, i + 1)))
                    {
                        if (token.IsCancellationRequested) return;
                        if (targetPublishStoreInfo != _publishStoreInfo) goto End;

                        try
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                this.IsProgressVisible.Value = true;

                                double value = Math.Round(((double)i / hashMap.Count) * 100, 2);
                                this.Rate.Text = $"{value}% {i}/{hashMap.Count}";
                                this.Rate.Value = value;
                            }, DispatcherPriority.Background, token);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }

                        try
                        {
                            _serviceManager.Import(path, token).Wait();
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                        }

                        if (token.IsCancellationRequested) return;
                    }
                }

                // Publish
                {
                    var infoMap = new Dictionary<string, Information>();

                    foreach (var info in _serviceManager.GetContentInformations())
                    {
                        infoMap.Add(info.GetValue<string>("Path"), info);
                    }

                    var boxes = new List<Box>();

                    foreach (var (name, directoryPath) in targetPublishStoreInfo.Map
                        .SelectMany(n => n.Value.Select(m => (n.Key, m.Key))))
                    {
                        var tempBox = CreateBox(name, directoryPath, infoMap);
                        if (IsEmpty(tempBox)) continue;

                        boxes.Add(tempBox);
                    }

                    _serviceManager.Upload(new Store(boxes), targetPublishStoreInfo.DigitalSignature, token).Wait();
                }

                if (token.IsCancellationRequested) return;

                lock (_lockObject)
                {
                    if (targetPublishStoreInfo == _publishStoreInfo)
                    {
                        _publishStoreInfo = null;
                    }
                }

                End:;
            }
        }

        private Box CreateBox(string name, string basePath, Dictionary<string, Information> map)
        {
            var seeds = new List<Seed>();
            var boxes = new List<Box>();

            foreach (string filePath in Directory.GetFiles(basePath))
            {
                if (!map.TryGetValue(filePath, out var info)) continue;

                try
                {

                    var seed = new Seed(Path.GetFileName(filePath), info.GetValue<long>("Length"), info.GetValue<DateTime>("CreationTime"), info.GetValue<Metadata>("Metadata"));
                    seeds.Add(seed);
                }
                catch (Exception)
                {

                }
            }

            foreach (string directoryPath in Directory.GetDirectories(basePath, "*", SearchOption.TopDirectoryOnly))
            {
                var tempBox = CreateBox(Path.GetFileName(directoryPath), directoryPath, map);
                if (IsEmpty(tempBox)) continue;

                boxes.Add(tempBox);
            }

            return new Box(name, seeds, boxes);
        }

        private static bool IsEmpty(Box box)
        {
            var boxes = new List<Box>();
            boxes.Add(box);

            for (int i = 0; i < boxes.Count; i++)
            {
                boxes.AddRange(boxes[i].Boxes);
                if (boxes[i].Seeds.Count() != 0) return false;
            }

            return true;
        }

        private async void StoreUpload()
        {
            var infos = _contents.Select(n => n.Clone()).ToList();
            var digitalSignature = SettingsManager.Instance.AccountInfo.DigitalSignature;
            if (digitalSignature == null) return;

            List<(string, string, string[])> map = null;
            PublishPreviewCategoryInfo boxInfo = null;

            try
            {
                ProgressDialog.Instance.Increment();

                await Task.Run(() =>
                {
                    map = new List<(string, string, string[])>();

                    foreach (var (name, directoryPath) in infos.Select(n => (n.Name, n.Path)))
                    {
                        var filePaths = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                        map.Add((name, directoryPath, filePaths));
                    }

                    // Preview
                    {
                        boxInfo = new PublishPreviewCategoryInfo();
                        boxInfo.Name = digitalSignature.ToString();

                        foreach (var (name, directoryPath, filePaths) in map)
                        {
                            var tempCategoryInfo = CreatePreviewCategoryInfo(name, directoryPath, new HashSet<string>(filePaths));
                            if (IsEmpty(tempCategoryInfo)) continue;

                            boxInfo.CategoryInfos.Add(tempCategoryInfo);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(e);

                return;
            }
            finally
            {
                ProgressDialog.Instance.Decrement();
            }

            var viewModel = new PublishPreviewWindowViewModel(boxInfo);
            viewModel.Callback += () =>
            {
                lock (_lockObject)
                {
                    _publishStoreInfo = new PublishStoreInfo(digitalSignature, map);
                }
            };

            Messenger.Instance.GetEvent<PublishPreviewWindowShowEvent>()
                .Publish(viewModel);
        }

        private PublishPreviewCategoryInfo CreatePreviewCategoryInfo(string name, string basePath, HashSet<string> filter)
        {
            var seedInfos = new List<PublishPreviewSeedInfo>();
            var categoryInfos = new List<PublishPreviewCategoryInfo>();

            foreach (string filePath in Directory.GetFiles(basePath))
            {
                if (!filter.Contains(filePath)) continue;

                try
                {
                    var fileInfo = new FileInfo(filePath);

                    var seedInfo = new PublishPreviewSeedInfo();
                    seedInfo.Name = fileInfo.Name;
                    seedInfo.Length = fileInfo.Length;

                    seedInfos.Add(seedInfo);
                }
                catch (Exception)
                {

                }
            }

            foreach (string directoryPath in Directory.GetDirectories(basePath))
            {
                var tempCategoryInfo = CreatePreviewCategoryInfo(Path.GetFileName(directoryPath), directoryPath, filter);
                if (IsEmpty(tempCategoryInfo)) continue;

                categoryInfos.Add(tempCategoryInfo);
            }

            var rootCategoryInfo = new PublishPreviewCategoryInfo();
            rootCategoryInfo.Name = name;
            rootCategoryInfo.CategoryInfos.AddRange(categoryInfos);
            rootCategoryInfo.SeedInfos.AddRange(seedInfos);

            return rootCategoryInfo;
        }

        private static bool IsEmpty(PublishPreviewCategoryInfo info)
        {
            var infos = new List<PublishPreviewCategoryInfo>();
            infos.Add(info);

            for (int i = 0; i < infos.Count; i++)
            {
                infos.AddRange(infos[i].CategoryInfos);
                if (infos[i].SeedInfos.Count != 0) return false;
            }

            return true;
        }

        private void Sort(string propertyName)
        {
            if (propertyName == null)
            {
                this.ContentsView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(_sortInfo.PropertyName))
                {
                    this.Sort(_sortInfo.PropertyName, _sortInfo.Direction);
                }
            }
            else
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

                this.ContentsView.SortDescriptions.Clear();

                if (!string.IsNullOrEmpty(propertyName))
                {
                    this.Sort(propertyName, direction);
                }

                _sortInfo.Direction = direction;
                _sortInfo.PropertyName = propertyName;
            }
        }

        private void Sort(string propertyName, ListSortDirection direction)
        {
            switch (propertyName)
            {
                case "Name":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
                case "Path":
                    this.ContentsView.SortDescriptions.Add(new SortDescription("Path", direction));
                    break;
            }
        }

        private void DirectoryAdd()
        {
            var info = this.EditDialog();
            if (info == null) return;

            var viewModel = new PublishDirectoryInfoEditWindowViewModel(info);
            viewModel.Callback += (_) => _contents.Add(info);

            Messenger.Instance.GetEvent<PublishDirectoryInfoEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private PublishDirectoryInfo EditDialog()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return new PublishDirectoryInfo()
                    {
                        Name = System.IO.Path.GetFileName(dialog.SelectedPath),
                        Path = dialog.SelectedPath
                    };
                }
            }

            return null;
        }

        private void DirectoryDelete()
        {
            var selectedItem = this.SelectedItem.Value;
            if (selectedItem == null) return;

            var viewModel = new ConfirmWindowViewModel(ConfirmWindowType.Delete);
            viewModel.Callback += () =>
            {
                _contents.Remove(selectedItem);
            };

            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                .Publish(viewModel);
        }

        private void DirectoryEdit()
        {
            var viewModel = new PublishDirectoryInfoEditWindowViewModel(this.SelectedItem.Value);

            Messenger.Instance.GetEvent<PublishDirectoryInfoEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("PublishDirectoryInfos", _contents);
                _settings.Save("SortInfo", _sortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
                _settings.Save("PublishStoreInfo", _publishStoreInfo);
            });
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

        [DataContract(Name = nameof(PublishStoreInfo))]
        private class PublishStoreInfo
        {
            private Dictionary<string, Dictionary<string, HashSet<string>>> _map;

            private PublishStoreInfo() { }

            public PublishStoreInfo(DigitalSignature digitalSignature, IEnumerable<(string, string, string[])> map)
            {
                this.DigitalSignature = digitalSignature;

                foreach (var (name, directoryPath, filePaths) in map)
                {
                    this.Map.GetOrAdd(name, (_) => new Dictionary<string, HashSet<string>>())
                        .AddOrUpdate(directoryPath, (_) => new HashSet<string>(filePaths), (_, target) =>
                        {
                            target.UnionWith(filePaths);
                            return target;
                        });
                }
            }

            [DataMember(Name = nameof(DigitalSignature))]
            public DigitalSignature DigitalSignature { get; private set; }

            [DataMember(Name = nameof(Map))]
            public Dictionary<string, Dictionary<string, HashSet<string>>> Map
            {
                get
                {
                    if (_map == null)
                        _map = new Dictionary<string, Dictionary<string, HashSet<string>>>();

                    return _map;
                }
            }
        }
    }
}
