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
    class StoreSubscribeControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        public ReactiveProperty<SubscribeCategoryViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }
        public DragAcceptDescription DragAcceptDescription { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ReactiveCommand TabNewCategoryCommand { get; private set; }
        public ReactiveCommand TabNewStoreCommand { get; private set; }
        public ReactiveCommand TabEditCommand { get; private set; }
        public ReactiveCommand TabDeleteCommand { get; private set; }
        public ReactiveCommand TabCopyCommand { get; private set; }
        public ReactiveCommand TabPasteCommand { get; private set; }

        public ObservableCollection<SubscribeItemViewModel> Contents { get; } = new ObservableCollection<SubscribeItemViewModel>();

        public DynamicViewModel Config { get; } = new DynamicViewModel();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public StoreSubscribeControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();

            this.DragAcceptDescription = new DragAcceptDescription() { Effects = DragDropEffects.Move, Format = "Store" };
            this.DragAcceptDescription.DragDrop += this.DragAcceptDescription_DragDrop;
        }

        private void DragAcceptDescription_DragDrop(DragAcceptEventArgs args)
        {
            var source = args.Source as TreeViewModelBase;
            var dest = args.Destination as TreeViewModelBase;
            if (source == null || dest == null) return;

            if (dest.GetAncestors().Contains(source)) return;

            if (dest.TryAdd(source))
            {
                source.Parent.TryRemove(source);
            }
        }

        public void Init()
        {
            {
                this.TabViewModel = new ReactiveProperty<SubscribeCategoryViewModel>().AddTo(_disposable);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Subscribe(() => this.TabSelectChanged(this.TabSelectedItem.Value)).AddTo(_disposable);

                this.TabNewCategoryCommand = this.TabSelectedItem.Select(n => n is SubscribeCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabNewCategoryCommand.Subscribe(() => this.TabNewCategory()).AddTo(_disposable);

                this.TabNewStoreCommand = this.TabSelectedItem.Select(n => n is SubscribeCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabNewStoreCommand.Subscribe(() => this.TabNewStore()).AddTo(_disposable);

                this.TabEditCommand = this.TabSelectedItem.Select(n => n is SubscribeCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabEditCommand.Subscribe(() => this.TabEdit()).AddTo(_disposable);

                this.TabDeleteCommand = this.TabSelectedItem.Select(n => n != this.TabViewModel.Value).ToReactiveCommand().AddTo(_disposable);
                this.TabDeleteCommand.Subscribe(() => this.TabDelete()).AddTo(_disposable);

                this.TabCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabCopyCommand.Subscribe(() => this.TabCopy()).AddTo(_disposable);

                this.TabPasteCommand = this.TabSelectedItem.Select(n => n is SubscribeCategoryViewModel).ToReactiveCommand().AddTo(_disposable);
                this.TabPasteCommand.Subscribe(() => this.TabPaste()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(StoreSubscribeControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));

                {
                    var model = _settings.Load("Tab", () =>
                    {
                        var categoryInfo = new SubscribeCategoryInfo() { Name = "Category", IsExpanded = true };
                        categoryInfo.StoreInfos.Add(new SubscribeStoreInfo() { AuthorSignature = Signature.Parse("Lyrise@i-2IpSdusn_TKfn6NSECLYRVO4r51cpHZ5wFgBo_0eU") });

                        return categoryInfo;
                    });

                    this.TabViewModel.Value = new SubscribeCategoryViewModel(null, model);
                }
            }
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is SubscribeCategoryViewModel subscribeCategoryViewModel)
            {
                this.Contents.Clear();
            }
            else if (viewModel is SubscribeStoreViewModel storeViewModel)
            {
                storeViewModel.Model.IsUpdated = false;

                var list = new List<SubscribeItemViewModel>();

                foreach (var item in storeViewModel.Model.BoxInfos)
                {
                    var vm = new SubscribeItemViewModel();
                    vm.Name = item.Name;

                    list.Add(vm);
                }

                this.Contents.Clear();
                this.Contents.AddRange(list);
            }
            else if (viewModel is SubscribeBoxViewModel boxViewModel)
            {
                var list = new List<SubscribeItemViewModel>();

                foreach (var item in boxViewModel.Model.BoxInfos)
                {
                    var vm = new SubscribeItemViewModel();
                    vm.Name = item.Name;
                    vm.Model = item;

                    list.Add(vm);
                }

                foreach (var item in boxViewModel.Model.Seeds)
                {
                    var vm = new SubscribeItemViewModel();
                    vm.Name = item.Name;
                    vm.CreationTime = item.CreationTime;
                    vm.Length = item.Length;
                    vm.Model = item;

                    list.Add(vm);
                }

                this.Contents.Clear();
                this.Contents.AddRange(list);
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                var subscribeStoreInfos = new List<SubscribeStoreInfo>();

                App.Current.Dispatcher.Invoke(() =>
                {
                    if (token.IsCancellationRequested) return;

                    var subscribeCategoryInfos = new List<SubscribeCategoryInfo>();
                    subscribeCategoryInfos.Add(this.TabViewModel.Value.Model);

                    for (int i = 0; i < subscribeCategoryInfos.Count; i++)
                    {
                        subscribeCategoryInfos.AddRange(subscribeCategoryInfos[i].CategoryInfos);
                        subscribeStoreInfos.AddRange(subscribeCategoryInfos[i].StoreInfos);
                    }
                });

                foreach (var storeInfo in subscribeStoreInfos)
                {
                    if (token.IsCancellationRequested) return;

                    var message = _serviceManager.GetStore(storeInfo.AuthorSignature).Result;
                    if (message == null || storeInfo.CreationTime == message.CreationTime) continue;

                    var tempBoxInfos = new List<SubscribeBoxInfo>();

                    foreach (var targetBox in message.Value.Boxes)
                    {
                        tempBoxInfos.Add(CreateBoxInfo(targetBox, storeInfo.BoxInfos.FirstOrDefault(n => n.Name == targetBox.Name)));
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return;

                        storeInfo.CreationTime = message.CreationTime;
                        storeInfo.IsUpdated = true;
                        storeInfo.BoxInfos.Clear();
                        storeInfo.BoxInfos.AddRange(tempBoxInfos);
                    });
                }

                if (token.WaitHandle.WaitOne(1000 * 30)) return;
            }
        }

        private SubscribeBoxInfo CreateBoxInfo(Box targetBox, SubscribeBoxInfo oldBoxInfo)
        {
            var info = new SubscribeBoxInfo();
            info.IsExpanded = oldBoxInfo?.IsExpanded ?? false;
            info.Name = targetBox.Name;
            info.Seeds.AddRange(targetBox.Seeds);

            foreach (var tempBox in targetBox.Boxes)
            {
                info.BoxInfos.Add(CreateBoxInfo(tempBox, oldBoxInfo.BoxInfos.FirstOrDefault(n => n.Name == tempBox.Name)));
            }

            return info;
        }

        private void TabNewCategory()
        {
            var viewModel = new NameEditWindowViewModel("");
            viewModel.Callback += (name) =>
            {
                var subscribeCategoryViewModel = this.TabSelectedItem.Value as SubscribeCategoryViewModel;
                if (subscribeCategoryViewModel == null) return;

                subscribeCategoryViewModel.Model.CategoryInfos.Add(new SubscribeCategoryInfo() { Name = name });
            };

            Messenger.Instance.GetEvent<NameEditWindowViewModelShowEvent>()
                .Publish(viewModel);
        }

        private void TabNewStore()
        {

        }

        private void TabEdit()
        {

        }

        private void TabDelete()
        {

        }

        private void TabCopy()
        {

        }

        private void TabPaste()
        {
            if (this.TabSelectedItem.Value is SubscribeCategoryViewModel subscribeCategoryViewModel)
            {
                subscribeCategoryViewModel.Model.CategoryInfos.AddRange(Clipboard.GetSubscribeCategoryInfos());
                subscribeCategoryViewModel.Model.StoreInfos.AddRange(Clipboard.GetSubscribeStoreInfos());

                foreach (var signature in Clipboard.GetSignatures())
                {
                    if (subscribeCategoryViewModel.Model.StoreInfos.Any(n => n.AuthorSignature == signature)) continue;
                    subscribeCategoryViewModel.Model.StoreInfos.Add(new SubscribeStoreInfo() { AuthorSignature = signature });
                }
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

                _settings.Save("Config", this.Config.GetPairs());
                _settings.Save("Tab", this.TabViewModel.Value.Model);

                _disposable.Dispose();
            }
        }
    }
}
