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
using Amoeba.Service;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Amoeba.Messages;

namespace Amoeba.Interface
{
    class RelationWindowViewModel : ManagerBase
    {
        private Settings _settings;

        private List<RelationSignatureInfo> _relationSignatureInfos = new List<RelationSignatureInfo>();

        public event EventHandler<EventArgs> CloseEvent;
        public event Action Callback;

        public ReactiveCollection<RelationSignatureViewModel> TabViewModels { get; } = new ReactiveCollection<RelationSignatureViewModel>();
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabCopyCommand { get; private set; }

        public ListCollectionView TrustSignaturesView => (ListCollectionView)CollectionViewSource.GetDefaultView(_trustSignatures);
        private ObservableCollection<Signature> _trustSignatures = new ObservableCollection<Signature>();
        public ObservableCollection<object> SelectedTrustSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _trustSignaturesSortInfo;
        public ReactiveCommand<string> TrustSignaturesSortCommand { get; private set; }

        public ReactiveCommand TrustCopyCommand { get; private set; }

        public ListCollectionView UntrustSignaturesView => (ListCollectionView)CollectionViewSource.GetDefaultView(_untrustSignatures);
        private ObservableCollection<Signature> _untrustSignatures = new ObservableCollection<Signature>();
        public ObservableCollection<object> SelectedUntrustSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _untrustSignaturesSortInfo;
        public ReactiveCommand<string> UntrustSignaturesSortCommand { get; private set; }

        public ReactiveCommand UntrustCopyCommand { get; private set; }

        public ListCollectionView TagsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_tags);
        private ObservableCollection<Tag> _tags = new ObservableCollection<Tag>();
        public ObservableCollection<object> SelectedTagItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _tagsSortInfo;
        public ReactiveCommand<string> TagsSortCommand { get; private set; }

        public ReactiveCommand TagCopyCommand { get; private set; }

        public ReactiveProperty<string> Comment { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        public ReactiveCommand CloseCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public RelationWindowViewModel(IEnumerable<RelationSignatureInfo> infos)
        {
            _relationSignatureInfos.AddRange(infos);

            this.Init();
        }

        private void Init()
        {
            {
                this.TabViewModels.AddRange(_relationSignatureInfos.Select(n => new RelationSignatureViewModel(null, n)));

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Where(n => n == this.TabSelectedItem.Value).Subscribe((_) => this.Refresh()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TrustSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.TrustSignaturesSortCommand.Subscribe((propertyName) => this.TrustSignaturesSort(propertyName)).AddTo(_disposable);

                this.TrustCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TrustCopyCommand.Subscribe(() => this.TrustCopy()).AddTo(_disposable);

                this.UntrustSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.UntrustSignaturesSortCommand.Subscribe((propertyName) => this.UntrustSignaturesSort(propertyName)).AddTo(_disposable);

                this.UntrustCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.UntrustCopyCommand.Subscribe(() => this.UntrustCopy()).AddTo(_disposable);

                this.TagsSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.TagsSortCommand.Subscribe((propertyName) => this.TagsSort(propertyName)).AddTo(_disposable);

                this.TagCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TagCopyCommand.Subscribe(() => this.TagCopy()).AddTo(_disposable);

                this.Comment = new ReactiveProperty<string>().AddTo(_disposable);

                this.CloseCommand = new ReactiveCommand().AddTo(_disposable);
                this.CloseCommand.Subscribe(() => this.Close()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(RelationWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _trustSignaturesSortInfo = _settings.Load("TrustSignaturesSortInfo ", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Signature" });
                _untrustSignaturesSortInfo = _settings.Load("UntrustSignaturesSortInfo ", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Signature" });
                _tagsSortInfo = _settings.Load("TagsSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.TrustSignaturesSort(null);
                this.UntrustSignaturesSort(null);
                this.TagsSort(null);
            }
        }

        private void Refresh()
        {
            this.TabSelectChanged(this.TabSelectedItem.Value);
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is RelationSignatureViewModel relationSignatureViewModel)
            {
                var profile = relationSignatureViewModel.Model.Profile;

                if (profile == null)
                {
                    _trustSignatures.Clear();
                    _untrustSignatures.Clear();
                    _tags.Clear();
                    this.Comment.Value = "";

                    return;
                }

                _trustSignatures.Clear();
                _trustSignatures.AddRange(profile.Value.TrustSignatures);
                _untrustSignatures.Clear();
                _untrustSignatures.AddRange(profile.Value.DeleteSignatures);
                _tags.Clear();
                _tags.AddRange(profile.Value.Tags);
                this.Comment.Value = profile.Value.Comment;
            }
        }

        private void TabCopy()
        {
            if (this.TabSelectedItem.Value is RelationSignatureViewModel relationSignatureViewModel)
            {
                Clipboard.SetSignatures(new Signature[] { relationSignatureViewModel.Model.Signature });
            }
        }

        private void TrustSignaturesSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_tagsSortInfo.PropertyName))
                {
                    this.TrustSignaturesSort(_tagsSortInfo.PropertyName, _tagsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_tagsSortInfo.PropertyName == propertyName)
                {
                    if (_tagsSortInfo.Direction == ListSortDirection.Ascending)
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
                    this.TrustSignaturesSort(propertyName, direction);
                }

                _tagsSortInfo.Direction = direction;
                _tagsSortInfo.PropertyName = propertyName;
            }
        }

        private void TrustSignaturesSort(string propertyName, ListSortDirection direction)
        {
            this.TrustSignaturesView.IsLiveSorting = true;
            this.TrustSignaturesView.LiveSortingProperties.Clear();
            this.TrustSignaturesView.SortDescriptions.Clear();

            if (propertyName == "Signature")
            {
                this.TrustSignaturesView.LiveSortingProperties.Add(propertyName);

                this.TrustSignaturesView.CustomSort = new CustomSortComparer(direction, (x, y) =>
                {
                    if (x is Signature tx && y is Signature ty)
                    {
                        int c = tx.Name.CompareTo(ty.Name);
                        if (c != 0) return c;
                        c = Unsafe.Compare(tx.Id, ty.Id);
                        if (c != 0) return c;
                    }

                    return 0;
                });
            }
            else
            {
                this.TrustSignaturesView.LiveSortingProperties.Add(propertyName);
                this.TrustSignaturesView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void TrustCopy()
        {
            Clipboard.SetSignatures(this.SelectedTrustSignatureItems.OfType<Signature>().ToArray());
        }

        private void UntrustSignaturesSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_untrustSignaturesSortInfo.PropertyName))
                {
                    this.UntrustSignaturesSort(_untrustSignaturesSortInfo.PropertyName, _untrustSignaturesSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_untrustSignaturesSortInfo.PropertyName == propertyName)
                {
                    if (_untrustSignaturesSortInfo.Direction == ListSortDirection.Ascending)
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
                    this.UntrustSignaturesSort(propertyName, direction);
                }

                _untrustSignaturesSortInfo.Direction = direction;
                _untrustSignaturesSortInfo.PropertyName = propertyName;
            }
        }

        private void UntrustSignaturesSort(string propertyName, ListSortDirection direction)
        {
            this.UntrustSignaturesView.IsLiveSorting = true;
            this.UntrustSignaturesView.LiveSortingProperties.Clear();
            this.UntrustSignaturesView.SortDescriptions.Clear();

            if (propertyName == "Signature")
            {
                this.UntrustSignaturesView.LiveSortingProperties.Add(propertyName);

                this.UntrustSignaturesView.CustomSort = new CustomSortComparer(direction, (x, y) =>
                {
                    if (x is Signature tx && y is Signature ty)
                    {
                        int c = tx.Name.CompareTo(ty.Name);
                        if (c != 0) return c;
                        c = Unsafe.Compare(tx.Id, ty.Id);
                        if (c != 0) return c;
                    }

                    return 0;
                });
            }
            else
            {
                this.UntrustSignaturesView.LiveSortingProperties.Add(propertyName);
                this.UntrustSignaturesView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void UntrustCopy()
        {
            Clipboard.SetSignatures(this.SelectedUntrustSignatureItems.OfType<Signature>().ToArray());
        }

        private void TagsSort(string propertyName)
        {
            if (propertyName == null)
            {
                if (!string.IsNullOrEmpty(_tagsSortInfo.PropertyName))
                {
                    this.TagsSort(_tagsSortInfo.PropertyName, _tagsSortInfo.Direction);
                }
            }
            else
            {
                var direction = ListSortDirection.Ascending;

                if (_tagsSortInfo.PropertyName == propertyName)
                {
                    if (_tagsSortInfo.Direction == ListSortDirection.Ascending)
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
                    this.TagsSort(propertyName, direction);
                }

                _tagsSortInfo.Direction = direction;
                _tagsSortInfo.PropertyName = propertyName;
            }
        }

        private void TagsSort(string propertyName, ListSortDirection direction)
        {
            this.TagsView.IsLiveSorting = true;
            this.TagsView.LiveSortingProperties.Clear();
            this.TagsView.SortDescriptions.Clear();

            if (propertyName == "Id")
            {
                this.TagsView.LiveSortingProperties.Add(propertyName);

                this.TagsView.CustomSort = new CustomSortComparer(direction, (x, y) => Unsafe.Compare(((Tag)x).Id, ((Tag)y).Id));
            }
            else
            {
                this.TagsView.LiveSortingProperties.Add(propertyName);
                this.TagsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void TagCopy()
        {
            Clipboard.SetTags(this.SelectedTagItems.OfType<Tag>().ToArray());
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Close()
        {
            this.Callback?.Invoke();

            this.OnCloseEvent();
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
                _settings.Save("TrustSignaturesSortInfo", _trustSignaturesSortInfo);
                _settings.Save("UntrustSignaturesSortInfo", _untrustSignaturesSortInfo);
                _settings.Save("TagsSortInfo", _tagsSortInfo);
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
