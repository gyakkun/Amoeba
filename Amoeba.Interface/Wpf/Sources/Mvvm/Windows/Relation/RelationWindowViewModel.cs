using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Data;
using Amoeba.Messages;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class RelationWindowViewModel : ManagerBase
    {
        private Builder _builder;

        private Settings _settings;

        public QuickSearchControlViewModel QuickSearchControlViewModel { get; set; }

        public event EventHandler<EventArgs> CloseEvent;
        public event Action Callback;

        public ListCollectionView ContentsView => (ListCollectionView)CollectionViewSource.GetDefaultView(_contents);
        private ObservableCollection<ProfileViewModel> _contents = new ObservableCollection<ProfileViewModel>();
        public ReactiveProperty<ProfileViewModel> SelectedItem { get; private set; }
        private ListSortInfo _sortInfo;
        public ReactiveCommand<string> SortCommand { get; private set; }

        public ReactiveCommand CopyCommand { get; private set; }

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

        public ReadOnlyReactiveCollection<RelationSignatureViewModel> DependencyViewModels { get; private set; }
        private ObservableCollection<RelationSignatureInfo> _dependencyModels = new ObservableCollection<RelationSignatureInfo>();
        public ReactiveProperty<TreeViewModelBase> SelectedDependencyItem { get; private set; }

        public ReactiveCommand DependencyCopyCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        public ReactiveCommand CloseCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public RelationWindowViewModel(MessageManager messageManager)
        {
            _builder = new Builder(messageManager);

            this.Init();
        }

        private void Init()
        {
            {
                this.QuickSearchControlViewModel = new QuickSearchControlViewModel().AddTo(_disposable);
                this.QuickSearchControlViewModel.Text.Subscribe((words) => this.Search(words)).AddTo(_disposable);

                foreach (var rootSignature in SettingsManager.Instance.SubscribeSignatures)
                {
                    _contents.AddRange(_builder.GetProfileViewModels(rootSignature));
                }

                this.SelectedItem = new ReactiveProperty<ProfileViewModel>().AddTo(_disposable);
                this.SelectedItem.Subscribe((viewModel) => this.SelectChanged(viewModel));

                this.SortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.SortCommand.Subscribe((propertyName) => this.Sort(propertyName)).AddTo(_disposable);

                this.CopyCommand = this.SelectedItem.ObserveProperty(n => n.Value).Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.CopyCommand.Subscribe(() => this.Copy()).AddTo(_disposable);

                this.TrustSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.TrustSignaturesSortCommand.Subscribe((propertyName) => this.TrustSignaturesSort(propertyName)).AddTo(_disposable);

                this.TrustCopyCommand = this.SelectedTrustSignatureItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.TrustCopyCommand.Subscribe(() => this.TrustCopy()).AddTo(_disposable);

                this.UntrustSignaturesSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.UntrustSignaturesSortCommand.Subscribe((propertyName) => this.UntrustSignaturesSort(propertyName)).AddTo(_disposable);

                this.UntrustCopyCommand = this.SelectedUntrustSignatureItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.UntrustCopyCommand.Subscribe(() => this.UntrustCopy()).AddTo(_disposable);

                this.TagsSortCommand = new ReactiveCommand<string>().AddTo(_disposable);
                this.TagsSortCommand.Subscribe((propertyName) => this.TagsSort(propertyName)).AddTo(_disposable);

                this.TagCopyCommand = this.SelectedTagItems.ObserveProperty(n => n.Count).Select(n => n != 0).ToReactiveCommand().AddTo(_disposable);
                this.TagCopyCommand.Subscribe(() => this.TagCopy()).AddTo(_disposable);

                this.Comment = new ReactiveProperty<string>().AddTo(_disposable);

                this.DependencyViewModels = _dependencyModels.ToReadOnlyReactiveCollection(n => new RelationSignatureViewModel(null, n)).AddTo(_disposable);
                this.SelectedDependencyItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);

                this.DependencyCopyCommand = this.SelectedDependencyItem.ObserveProperty(n => n.Value).Select(n => n != null).ToReactiveCommand().AddTo(_disposable);
                this.DependencyCopyCommand.Subscribe(() => this.DependencyCopy()).AddTo(_disposable);

                this.CloseCommand = new ReactiveCommand().AddTo(_disposable);
                this.CloseCommand.Subscribe(() => this.Close()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(RelationWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                _sortInfo = _settings.Load("SortInfo ", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Ranking" });
                _trustSignaturesSortInfo = _settings.Load("TrustSignaturesSortInfo ", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Signature" });
                _untrustSignaturesSortInfo = _settings.Load("UntrustSignaturesSortInfo ", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Signature" });
                _tagsSortInfo = _settings.Load("TagsSortInfo", () => new ListSortInfo() { Direction = ListSortDirection.Ascending, PropertyName = "Name" });
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                this.Sort(null);
                this.TrustSignaturesSort(null);
                this.UntrustSignaturesSort(null);
                this.TagsSort(null);
            }
        }

        private void Search(string words)
        {
            if (string.IsNullOrWhiteSpace(words))
            {
                this.ContentsView.Filter = null;
                this.ContentsView.Refresh();

                return;
            }

            var wordList = words.Split(new string[] { " ", "Å@" }, StringSplitOptions.RemoveEmptyEntries);

            this.ContentsView.Filter = new Predicate<object>((viewModel) =>
            {
                if (viewModel is ProfileViewModel profileViewModel)
                {
                    if (wordList.All(n => profileViewModel.Signature.ToString().Contains(n, StringComparison.CurrentCultureIgnoreCase))) return true;
                }

                return false;
            });

            this.ContentsView.Refresh();
            this.Refresh();
        }

        private void Refresh()
        {
            this.SelectChanged(this.SelectedItem.Value);
        }

        private void SelectChanged(ProfileViewModel viewModel)
        {
            if (viewModel == null) return;

            _trustSignatures.Clear();
            _trustSignatures.AddRange(viewModel.Model.Value.TrustSignatures);
            _untrustSignatures.Clear();
            _untrustSignatures.AddRange(viewModel.Model.Value.DeleteSignatures);
            _tags.Clear();
            _tags.AddRange(viewModel.Model.Value.Tags);
            this.Comment.Value = viewModel.Model.Value.Comment;

            {
                _dependencyModels.Clear();

                foreach (var rootSignature in SettingsManager.Instance.SubscribeSignatures)
                {
                    _dependencyModels.Add(_builder.GetRelationSignatureInfo(rootSignature, viewModel.Model.AuthorSignature));
                }
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

            if (propertyName == "Signature")
            {
                this.ContentsView.LiveSortingProperties.Add(propertyName);

                this.ContentsView.CustomSort = new CustomSortComparer(direction, (x, y) =>
                {
                    if (x is ProfileViewModel tx && y is ProfileViewModel ty)
                    {
                        int c = tx.Signature.Name.CompareTo(ty.Signature.Name);
                        if (c != 0) return c;
                        c = Unsafe.Compare(tx.Signature.Id, ty.Signature.Id);
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

        private void Copy()
        {
            Clipboard.SetSignatures(new Signature[] { this.SelectedItem.Value.Signature });
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

        private void DependencyCopy()
        {
            if (this.SelectedDependencyItem.Value is RelationSignatureViewModel relationSignatureViewModel)
            {
                Clipboard.SetSignatures(new Signature[] { relationSignatureViewModel.Model.Signature });
            }
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
                _settings.Save("SortInfo", _sortInfo);
                _settings.Save("TrustSignaturesSortInfo", _trustSignaturesSortInfo);
                _settings.Save("UntrustSignaturesSortInfo", _untrustSignaturesSortInfo);
                _settings.Save("TagsSortInfo", _tagsSortInfo);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        class Builder
        {
            private MessageManager _messageManager;

            public Builder(MessageManager messageManager)
            {
                _messageManager = messageManager;
            }

            public IEnumerable<ProfileViewModel> GetProfileViewModels(Signature rootSignature)
            {
                var hashSet = new HashSet<Signature>();
                var viewModels = new List<ProfileViewModel>();

                {
                    hashSet.Add(rootSignature);

                    var rootProfile = _messageManager.GetProfile(rootSignature);
                    if (rootProfile == null) return Enumerable.Empty<ProfileViewModel>();

                    var viewModel = new ProfileViewModel();
                    viewModel.Ranking = 1;
                    viewModel.Signature = rootSignature;
                    viewModel.Model = rootProfile;

                    viewModels.Add(viewModel);
                }

                for (int i = 0; i < viewModels.Count; i++)
                {
                    var parentViewModel = viewModels[i];

                    foreach (var targetSignature in parentViewModel.Model.Value.TrustSignatures)
                    {
                        if (hashSet.Contains(targetSignature)) continue;
                        hashSet.Add(targetSignature);

                        var targetProfile = _messageManager.GetProfile(targetSignature);
                        if (targetProfile == null) continue;

                        var viewModel = new ProfileViewModel();
                        viewModel.Ranking = parentViewModel.Ranking + 1;
                        viewModel.Signature = targetSignature;
                        viewModel.Model = targetProfile;

                        viewModels.Add(viewModel);
                    }
                }

                return viewModels;
            }

            private RelationSignatureInfo SearchRelationSignatureInfo(Signature rootSignature, Signature searchSignature, int ranking, IDictionary<Signature, ProfileViewModel> map)
            {
                ProfileViewModel rootViewModel;
                if (!map.TryGetValue(rootSignature, out rootViewModel)) return null;
                if (rootViewModel.Ranking <= ranking) return null;

                if (rootViewModel.Signature == searchSignature)
                {
                    var info = new RelationSignatureInfo();
                    info.Signature = rootViewModel.Signature;
                    info.Profile = rootViewModel.Model;

                    return info;
                }
                else
                {
                    var tempList = new List<RelationSignatureInfo>();

                    foreach (var trustSignature in rootViewModel.Model.Value.TrustSignatures)
                    {
                        var result = this.SearchRelationSignatureInfo(trustSignature, searchSignature, ranking + 1, map);
                        if (result == null) continue;

                        tempList.Add(result);
                    }

                    if (tempList.Count == 0) return null;

                    var info = new RelationSignatureInfo();
                    info.Signature = rootViewModel.Signature;
                    info.Profile = rootViewModel.Model;
                    info.Children.AddRange(tempList);

                    return info;
                }
            }

            public RelationSignatureInfo GetRelationSignatureInfo(Signature rootSignature, Signature searchSignature)
            {
                var map = new Dictionary<Signature, ProfileViewModel>();

                foreach (var viewModel in this.GetProfileViewModels(rootSignature))
                {
                    map[viewModel.Signature] = viewModel;
                }

                return SearchRelationSignatureInfo(rootSignature, searchSignature, 0, map);
            }
        }

        public class ProfileViewModel
        {
            public int Ranking { get; set; }
            public Signature Signature { get; set; }
            public BroadcastMessage<Profile> Model { get; set; }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                Backup.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
