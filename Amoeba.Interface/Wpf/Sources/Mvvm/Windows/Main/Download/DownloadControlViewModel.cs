using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using Amoeba.Messages;
using Amoeba.Rpc;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class DownloadControlViewModel : ManagerBase
    {
        private AmoebaInterfaceManager _amoebaInterfaceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        private DialogService _dialogService;

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents.Values);
        private ObservableSimpleDictionary<(Metadata, string), DownloadListViewItemInfo> _contents = new ObservableSimpleDictionary<(Metadata, string), DownloadListViewItemInfo>(new CustomEqualityComparer());
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand PasteCommand { get; private set; }
        public ReactiveCommand ResetCommand { get; private set; }

        public ReactiveCommand RemoveCompletedItemCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public DownloadControlViewModel(AmoebaInterfaceManager serviceManager, DialogService dialogService)
        {
            _amoebaInterfaceManager = serviceManager;
            _dialogService = dialogService;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();
        }

        public void Init()
        {
            {
                IObservable<object> clipboardObservable;
                {
                    var returnObservable = Observable.Return((object)null);
                    var watchObservable = Observable.FromEventPattern<EventHandler, EventArgs>(h => Clipboard.ClipboardChanged += h, h => Clipboard.ClipboardChanged -= h).Select(n => (object)null);
                    clipboardObservable = Observable.Merge(returnObservable, watchObservable);
                }

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.PasteCommand = clipboardObservable.Select(n => Clipboard.ContainsSeeds()).ToReactiveCommand().AddTo(_disposable);
                this.PasteCommand.Subscribe(() => this.Paste()).AddTo(_disposable);

                this.ResetCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.ResetCommand.Subscribe(() => this.Reset()).AddTo(_disposable);

                this.RemoveCompletedItemCommand = new ReactiveCommand().AddTo(_disposable);
                this.RemoveCompletedItemCommand.Subscribe(() => this.RemoveCompletedItem()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(DownloadControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                EventHooks.Instance.SaveEvent += this.Save;
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (; ; )
            {
                var downloadItemInfos = new Dictionary<(Metadata, string), DownloadItemInfo>(new CustomEqualityComparer());

                foreach (var newValue in SettingsManager.Instance.DownloadItemInfos.ToArray())
                {
                    var key = (newValue.Seed.Metadata, newValue.Path);

                    if (!downloadItemInfos.TryGetValue(key, out var oldValue))
                    {
                        downloadItemInfos.Add(key, newValue);
                    }
                    else
                    {
                        if (oldValue.Seed.CreationTime < newValue.Seed.CreationTime)
                        {
                            downloadItemInfos[key] = newValue;
                            SettingsManager.Instance.DownloadItemInfos.Remove(oldValue);
                        }
                        else
                        {
                            SettingsManager.Instance.DownloadItemInfos.Remove(newValue);
                        }
                    }
                }

                var map = new Dictionary<(Metadata, string), DownloadContentReport>(new CustomEqualityComparer());

                foreach (var report in _amoebaInterfaceManager.GetDownloadContentReports())
                {
                    map.Add((report.Metadata, report.Path), report);
                }

                foreach (var item in downloadItemInfos.Values)
                {
                    if (!map.TryGetValue((item.Seed.Metadata, item.Path), out var info))
                    {
                        _amoebaInterfaceManager.AddDownload(item.Seed.Metadata, item.Path, item.Seed.Length);
                    }
                }

                foreach (var (metadata, path) in map.Keys)
                {
                    if (!downloadItemInfos.ContainsKey((metadata, path)))
                    {
                        _amoebaInterfaceManager.RemoveDownload(metadata, path);
                    }
                }

                App.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;

                    foreach (var (metadata, path) in _contents.Keys.ToArray())
                    {
                        if (!downloadItemInfos.ContainsKey((metadata, path)))
                        {
                            _contents.Remove((metadata, path));
                        }
                    }

                    foreach (var item in downloadItemInfos.Values)
                    {
                        if (!map.TryGetValue((item.Seed.Metadata, item.Path), out var report)) continue;

                        DownloadListViewItemInfo viewModel;

                        if (!_contents.TryGetValue((item.Seed.Metadata, item.Path), out viewModel))
                        {
                            viewModel = new DownloadListViewItemInfo();
                            _contents[(item.Seed.Metadata, item.Path)] = viewModel;
                        }

                        viewModel.Icon = IconUtils.GetImage(item.Seed.Name);
                        viewModel.Name = item.Seed.Name;
                        viewModel.Length = item.Seed.Length;
                        viewModel.CreationTime = item.Seed.CreationTime;

                        // Rate
                        {
                            viewModel.Rate.Depth = report.Depth;

                            double value = Math.Round(((double)report.DownloadBlockCount / (report.BlockCount - report.ParityBlockCount)) * 100, 2);
                            string text = string.Format("{0}% {1}/{2}({3}) [{4}/{5}]",
                                value,
                                report.DownloadBlockCount,
                                (report.BlockCount - report.ParityBlockCount),
                                report.BlockCount,
                                report.Depth,
                                report.Metadata.Depth);

                            viewModel.Rate.Value = value;
                            viewModel.Rate.Text = text;
                        }

                        viewModel.State = report.State;
                        viewModel.Path = item.Path;
                        viewModel.Model = item;
                    }

                    this.Sort();
                });

                if (token.WaitHandle.WaitOne(1000 * 3)) return;
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

        private int Sort(DownloadListViewItemInfo x, DownloadListViewItemInfo y, string propertyName, ListSortDirection direction)
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
            else if (propertyName == "Rate")
            {
                int c = a * x.Rate.Depth.CompareTo(y.Rate.Depth);
                if (c != 0) return c;
                c = a * x.Rate.Value.CompareTo(y.Rate.Value);
                if (c != 0) return c;
                c = a * x.Name.CompareTo(y.Name);
                return c;
            }
            else if (propertyName == "State")
            {
                int c = a * x.State.CompareTo(y.State);
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

        private void Delete()
        {
            var selectedItems = this.SelectedItems.OfType<DownloadListViewItemInfo>().ToList();
            if (selectedItems.Count == 0) return;

            if (_dialogService.ShowDialog(LanguagesManager.Instance.ConfirmWindow_DeleteMessage,
                MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                foreach (var selectedItem in selectedItems.Select(n => n.Model))
                {
                    SettingsManager.Instance.DownloadItemInfos.Remove(selectedItem);
                }

                foreach (var selectedItem in selectedItems.Where(n => n.State == DownloadState.Completed)
                    .Select(n => n.Model.Seed))
                {
                    SettingsManager.Instance.DownloadedSeeds.Add(selectedItem);
                }
            }
        }

        private void Copy()
        {
            var selectedItems = this.SelectedItems.OfType<DownloadListViewItemInfo>()
                .Select(n => n.Model).ToList();
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

        private void Reset()
        {
            var selectedItems = this.SelectedItems.OfType<DownloadListViewItemInfo>()
                .Select(n => n.Model)
                .Select(n => (n.Seed.Metadata, n.Path)).ToList();
            if (selectedItems.Count == 0) return;

            foreach (var (metadata, path) in selectedItems)
            {
                _amoebaInterfaceManager.ResetDownload(metadata, path);
            }
        }

        private void RemoveCompletedItem()
        {
            var hashSet = new HashSet<(Metadata, string)>(new CustomEqualityComparer());

            foreach (var report in _amoebaInterfaceManager.GetDownloadContentReports())
            {
                if (report.State != DownloadState.Completed) continue;
                hashSet.Add((report.Metadata, report.Path));
            }

            if (hashSet.Count == 0) return;

            foreach (var item in SettingsManager.Instance.DownloadItemInfos.ToArray())
            {
                if (!hashSet.Contains((item.Seed.Metadata, item.Path))) continue;

                SettingsManager.Instance.DownloadItemInfos.Remove(item);
                SettingsManager.Instance.DownloadedSeeds.Add(item.Seed);
            }
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

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                EventHooks.Instance.SaveEvent -= this.Save;

                _watchTaskManager.Stop();
                _watchTaskManager.Dispose();

                this.Save();

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
