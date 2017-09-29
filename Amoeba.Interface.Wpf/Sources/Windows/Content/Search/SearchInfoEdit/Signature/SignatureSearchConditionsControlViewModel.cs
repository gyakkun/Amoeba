using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class SignatureSearchConditionsControlViewModel : ManagerBase
    {
        private Settings _settings;

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<SearchCondition<Signature>> _contents = new ObservableCollection<SearchCondition<Signature>>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand DeleteCommand { get; private set; }
        public ReactiveCommand CopyCommand { get; private set; }
        public ReactiveCommand PasteCommand { get; private set; }

        public ReactiveCommand ContainsCommand { get; private set; }
        public ReactiveCommand NotContainsCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private readonly object _lockObject = new object();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public SignatureSearchConditionsControlViewModel(IEnumerable<SearchCondition<Signature>> contents)
        {
            _contents.AddRange(contents);

            this.Init();
        }

        public IEnumerable<SearchCondition<Signature>> GetContents()
        {
            return _contents.ToArray();
        }

        public void Init()
        {
            {
                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.DeleteCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.DeleteCommand.Subscribe(() => this.Delete()).AddTo(_disposable);

                this.CopyCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.PasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.PasteCommand.Subscribe(() => this.Paste()).AddTo(_disposable);

                this.ContainsCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.ContainsCommand.Subscribe(() => this.ChangeContains()).AddTo(_disposable);

                this.NotContainsCommand = this.SelectedItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.NotContainsCommand.Subscribe(() => this.ChangeNotContains()).AddTo(_disposable);

            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(SignatureSearchConditionsControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _sortInfo = _settings.Load("SortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Contains" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.Sort(null);
            }
        }

        private void Sort(string propertyName)
        {
            if (propertyName == null)
            {
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
            this.ContentsView.IsLiveSorting = true;
            this.ContentsView.LiveSortingProperties.Clear();
            this.ContentsView.SortDescriptions.Clear();

            if (propertyName == "Value")
            {
                this.ContentsView.LiveSortingProperties.Add(propertyName);

                this.ContentsView.CustomSort = new CustomSortComparer(direction, (x, y) =>
                {
                    if (x is SearchCondition<Signature> tx && y is SearchCondition<Signature> ty)
                    {
                        int c = tx.Value.Name.CompareTo(ty.Value.Name);
                        if (c != 0) return c;
                        c = Unsafe.Compare(tx.Value.Id, ty.Value.Id);
                        if (c != 0) return c;
                    }

                    return 0;
                });
            }
            else
            {
                this.ContentsView.LiveSortingProperties.Add(propertyName);
                this.ContentsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void Delete()
        {
            foreach (var item in this.SelectedItems.OfType<SearchCondition<Signature>>().ToArray())
            {
                _contents.Remove(item);
            }
        }

        private void Copy()
        {
            Clipboard.SetSignatures(this.SelectedItems.OfType<SearchCondition<Signature>>().Select(n => n.Value).ToArray());
        }

        private void Paste()
        {
            foreach (var item in Clipboard.GetSignatures())
            {
                if (_contents.Any(n => n.Value == item)) continue;

                var value = new SearchCondition<Signature>(true, item);

                _contents.Add(value);
            }
        }

        private void ChangeContains()
        {
            foreach (var item in this.SelectedItems.OfType<SearchCondition<Signature>>().ToArray())
            {
                int index = _contents.IndexOf(item);
                _contents[index] = new SearchCondition<Signature>(true, item.Value);
            }
        }

        private void ChangeNotContains()
        {
            foreach (var item in this.SelectedItems.OfType<SearchCondition<Signature>>().ToArray())
            {
                int index = _contents.IndexOf(item);
                _contents[index] = new SearchCondition<Signature>(false, item.Value);
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

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
