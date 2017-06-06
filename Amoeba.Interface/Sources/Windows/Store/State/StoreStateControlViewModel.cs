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

namespace Amoeba.Interface
{
    class StoreStateControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        public ObservableDictionary<(Metadata, string), DynamicOptions> Contents { get; } = new ObservableDictionary<(Metadata, string), DynamicOptions>(new CustomEqualityComparer());
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();

        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand PasteCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StoreStateControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();
        }

        public void Init()
        {
            {
                this.DeleteCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.PasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.PasteCommand.Subscribe(() => this.Paste()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StoreStateControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                var downloadItemInfos = new Dictionary<(Metadata, string), DownloadItemInfo>(new CustomEqualityComparer());

                foreach (var item in SettingsManager.Instance.DownloadItemInfos.ToArray())
                {
                    downloadItemInfos.Add((item.Seed.Metadata, item.Path), item);
                }

                var map = new Dictionary<(Metadata, string), Information>(new CustomEqualityComparer());

                foreach (var info in _serviceManager.GetDownloadInformations())
                {
                    map.Add((info.GetValue<Metadata>("Metadata"), info.GetValue<string>("Path")), info);
                }

                foreach (var item in downloadItemInfos.Values)
                {
                    if (!map.TryGetValue((item.Seed.Metadata, item.Path), out var info))
                    {
                        _serviceManager.AddDownload(item.Seed.Metadata, item.Path, item.Seed.Length);
                    }
                }

                foreach (var (metadata, path) in map.Keys)
                {
                    if (!downloadItemInfos.ContainsKey((metadata, path)))
                    {
                        _serviceManager.RemoveDownload(metadata, path);
                    }
                }

                App.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;

                    foreach (var (metadata, path) in this.Contents.Keys.ToArray())
                    {
                        if (!downloadItemInfos.ContainsKey((metadata, path)))
                        {
                            this.Contents.Remove((metadata, path));
                        }
                    }

                    foreach (var item in downloadItemInfos.Values)
                    {
                        if (!map.TryGetValue((item.Seed.Metadata, item.Path), out var info)) continue;

                        DynamicOptions viewModel;

                        if (!this.Contents.TryGetValue((item.Seed.Metadata, item.Path), out viewModel))
                        {
                            viewModel = new DynamicOptions();
                            this.Contents[(item.Seed.Metadata, item.Path)] = viewModel;
                        }

                        viewModel.SetValue("Icon", IconUtils.GetImage(item.Seed.Name));
                        viewModel.SetValue("Name", item.Seed.Name);
                        viewModel.SetValue("CreationTime", item.Seed.CreationTime);
                        viewModel.SetValue("Length", item.Seed.Length);
                        // Rate
                        {
                            double value = Math.Round(((double)info.GetValue<int>("DownloadBlockCount") / (info.GetValue<int>("BlockCount") - info.GetValue<int>("ParityBlockCount"))) * 100, 2);

                            string text = string.Format("{0}% {1}/{2}({3}) [{4}/{5}]",
                                value,
                                info.GetValue<int>("DownloadBlockCount"),
                                (info.GetValue<int>("BlockCount") - info.GetValue<int>("ParityBlockCount")),
                                info.GetValue<int>("BlockCount"),
                                info.GetValue<int>("Depth"),
                                info.GetValue<Metadata>("Metadata").Depth);

                            viewModel.SetValue("Rate_Value", value);
                            viewModel.SetValue("Rate_Text", text);
                        }
                        viewModel.SetValue("State", info.GetValue<DownloadState>("State"));
                        viewModel.SetValue("Path", item.Path);
                        viewModel.SetValue("Model", item);
                    }
                });

                if (token.WaitHandle.WaitOne(1000 * 3)) return;
            }
        }

        private void Delete()
        {
            var selectedItems = this.SelectedItems.OfType<KeyValuePair<(Metadata, string), DynamicOptions>>()
                .Select(n => n.Value.GetValue<DownloadItemInfo>("Model")).ToList();
            if (selectedItems.Count == 0) return;

            foreach (var selectedItem in selectedItems)
            {
                SettingsManager.Instance.DownloadItemInfos.Remove(selectedItem);
            }
        }

        private void Copy()
        {
            var selectedItems = this.SelectedItems.OfType<KeyValuePair<(Metadata, string), DynamicOptions>>()
                .Select(n => n.Value.GetValue<DownloadItemInfo>("Model")).ToList();
            if (selectedItems.Count == 0) return;

            Clipboard.SetSeeds(selectedItems.Select(n => n.Seed).ToArray());
        }

        private void Paste()
        {
            foreach (var seed in Clipboard.GetSeeds())
            {
                var downloadItemInfo = new DownloadItemInfo(seed, seed.Name);
                SettingsManager.Instance.DownloadItemInfos.Add(downloadItemInfo);
            }
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

                _disposable.Dispose();
            }
        }

        class CustomEqualityComparer : IEqualityComparer<(Metadata Metadata, string Path)>
        {
            public bool Equals((Metadata Metadata, string Path) x, (Metadata Metadata, string Path) y)
            {
                return (x.Metadata == y.Metadata && x.Path == y.Path);
            }

            public int GetHashCode((Metadata Metadata, string Path) value)
            {
                return value.Metadata.GetHashCode();
            }
        }
    }
}
