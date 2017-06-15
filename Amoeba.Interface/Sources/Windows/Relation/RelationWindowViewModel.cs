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
    class RelationWindowViewModel : ManagerBase
    {
        private List<RelationSignatureInfo> _relationSignatureInfos = new List<RelationSignatureInfo>();

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action Callback;

        public ReactiveCollection<RelationSignatureViewModel> TabViewModels { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabCopyCommand { get; private set; }

        public ICollectionView TrustSignaturesView => CollectionViewSource.GetDefaultView(_trustSignatures);
        private ObservableCollection<Signature> _trustSignatures = new ObservableCollection<Signature>();
        public ObservableCollection<object> SelectedTrustSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _trustSignaturesSortInfo;
        public ReactiveCommand<string> TrustSignaturesSortCommand { get; private set; }

        public ReactiveCommand TrustCopyCommand { get; private set; }

        public ICollectionView UntrustSignaturesView => CollectionViewSource.GetDefaultView(_untrustSignatures);
        private ObservableCollection<Signature> _untrustSignatures = new ObservableCollection<Signature>();
        public ObservableCollection<object> SelectedUntrustSignatureItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _untrustSignaturesSortInfo;
        public ReactiveCommand<string> UntrustSignaturesSortCommand { get; private set; }

        public ReactiveCommand UntrustCopyCommand { get; private set; }

        public ICollectionView TagsView => CollectionViewSource.GetDefaultView(_tags);
        private ObservableCollection<Tag> _tags = new ObservableCollection<Tag>();
        public ObservableCollection<object> SelectedTagItems { get; } = new ObservableCollection<object>();
        private ListSortInfo _tagsSortInfo;
        public ReactiveCommand<string> TagsSortCommand { get; private set; }

        public ReactiveCommand TagCopyCommand { get; private set; }

        public ReactiveProperty<string> Comment { get; private set; }

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

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
                this.TabViewModels = new ReactiveCollection<RelationSignatureViewModel>();
                this.TabViewModels.AddRange(_relationSignatureInfos.Select(n => new RelationSignatureViewModel(null, n)));

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Subscribe(() => this.TabSelectChanged(this.TabSelectedItem.Value)).AddTo(_disposable);

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

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(RelationWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _trustSignaturesSortInfo = _settings.Load("TrustSignaturesSortInfo ", () => new ListSortInfo());
                _untrustSignaturesSortInfo = _settings.Load("UntrustSignaturesSortInfo ", () => new ListSortInfo());
                _tagsSortInfo = _settings.Load("TagsSortInfo", () => new ListSortInfo());
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += () => this.Save();
            }

            {
                this.TrustSignaturesSort(null);
                this.UntrustSignaturesSort(null);
                this.TagsSort(null);
            }
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is RelationSignatureViewModel relationSignatureViewModel)
            {
                var profile = relationSignatureViewModel.Model.Profile;
                if (profile == null) return;

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
                this.TrustSignaturesView.SortDescriptions.Clear();

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

                this.TrustSignaturesView.SortDescriptions.Clear();

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
            switch (propertyName)
            {
                case "Signature":
                    this.TrustSignaturesView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
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
                this.UntrustSignaturesView.SortDescriptions.Clear();

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

                this.UntrustSignaturesView.SortDescriptions.Clear();

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
            switch (propertyName)
            {
                case "Signature":
                    this.UntrustSignaturesView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
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
                this.TagsView.SortDescriptions.Clear();

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

                this.TagsView.SortDescriptions.Clear();

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
            switch (propertyName)
            {
                case "Name":
                    this.TagsView.SortDescriptions.Add(new SortDescription("Name", direction));
                    break;
                case "Id":
                    {
                        var view = ((ListCollectionView)this.TagsView);
                        view.CustomSort = new CustomSortComparer(direction, (x, y) => Unsafe.Compare(((Tag)x).Id, ((Tag)y).Id));
                        view.Refresh();
                    }
                    break;
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
                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
