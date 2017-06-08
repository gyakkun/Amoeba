using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using Omnius.Utilities;
using Omnius.Security;
using Prism.Events;
using Prism.Interactivity.InteractionRequest;
using System.Runtime.Serialization;

namespace Amoeba.Interface
{
    class StorePublishControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        public ReactiveProperty<PublishDirectoryInfo> SelectedItem { get; private set; }

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
                this.SelectedItem = new ReactiveProperty<PublishDirectoryInfo>().AddTo(_disposable);

                this.UploadCommand = SettingsManager.Instance.PublishDirectoryInfos.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand();
                this.UploadCommand.Subscribe(() => this.StoreUpload()).AddTo(_disposable);

                this.AddCommand = new ReactiveCommand().AddTo(_disposable);
                this.AddCommand.Subscribe(() => this.DirectoryAdd()).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItem.Select(n => n != null).ToReactiveCommand();
                this.DeleteCommand.Subscribe(() => this.DirectoryDelete()).AddTo(_disposable);

                this.EditCommand = this.SelectedItem.Select(n => n != null).ToReactiveCommand();
                this.EditCommand.Subscribe(() => this.DirectoryEdit()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StorePublishControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));

                _publishStoreInfo = _settings.Load<PublishStoreInfo>("PublishStoreInfo", () => null);
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                if (token.WaitHandle.WaitOne(1000 * 3)) return;

                var targetPublishStoreInfo = _publishStoreInfo;
                if (targetPublishStoreInfo == null) continue;

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

                    foreach (string path in hashMap)
                    {
                        if (token.IsCancellationRequested) return;
                        if (targetPublishStoreInfo != _publishStoreInfo) goto End;

                        _serviceManager.Import(path, token).Wait();
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
                        boxes.Add(CreateBox(name, directoryPath, infoMap));
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

            foreach (string directoryPath in Directory.GetDirectories(basePath))
            {
                boxes.Add(CreateBox(Path.GetFileName(directoryPath), directoryPath, map));
            }

            return new Box(name, seeds, boxes);
        }

        private async void StoreUpload()
        {
            var infos = SettingsManager.Instance.PublishDirectoryInfos.Select(n => n.Clone()).ToList();
            var digitalSignature = SettingsManager.Instance.AccountInfo.DigitalSignature;
            if (digitalSignature == null) return;

            List<(string, string, string[])> map = null;
            PublishPreviewBoxInfo boxInfo = null;

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
                        boxInfo = new PublishPreviewBoxInfo();
                        boxInfo.Name = digitalSignature.ToString();

                        foreach (var (name, directoryPath, filePaths) in map)
                        {
                            boxInfo.BoxInfos.Add(CreatePreviewBoxInfo(name, directoryPath, new HashSet<string>(filePaths)));
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

        private PublishPreviewBoxInfo CreatePreviewBoxInfo(string name, string basePath, HashSet<string> filter)
        {
            var seedInfos = new List<PublishPreviewSeedInfo>();
            var boxInfos = new List<PublishPreviewBoxInfo>();

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
                boxInfos.Add(CreatePreviewBoxInfo(Path.GetFileName(directoryPath), directoryPath, filter));
            }

            var rootBoxInfo = new PublishPreviewBoxInfo();
            rootBoxInfo.Name = name;
            rootBoxInfo.BoxInfos.AddRange(boxInfos);
            rootBoxInfo.SeedInfos.AddRange(seedInfos);

            return rootBoxInfo;
        }

        private void DirectoryAdd()
        {
            var viewModel = new PublishDirectoryInfoEditWindowViewModel(new PublishDirectoryInfo());
            viewModel.Callback += (info) => SettingsManager.Instance.PublishDirectoryInfos.Add(info);

            Messenger.Instance.GetEvent<PublishDirectoryInfoEditWindowShowEvent>()
                .Publish(viewModel);
        }

        private void DirectoryDelete()
        {
            var selectedItem = this.SelectedItem.Value;
            if (selectedItem == null) return;

            SettingsManager.Instance.PublishDirectoryInfos.Remove(selectedItem);
        }

        private void DirectoryEdit()
        {
            var viewModel = new PublishDirectoryInfoEditWindowViewModel(this.SelectedItem.Value);

            Messenger.Instance.GetEvent<PublishDirectoryInfoEditWindowShowEvent>()
                .Publish(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                _settings.Save("Version", 0);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
                _settings.Save("PublishStoreInfo", _publishStoreInfo);

                _disposable.Dispose();
            }
        }

        [DataContract(Name = nameof(PublishStoreInfo))]
        private class PublishStoreInfo
        {
            private Dictionary<string, Dictionary<string, HashSet<string>>> _map;

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
