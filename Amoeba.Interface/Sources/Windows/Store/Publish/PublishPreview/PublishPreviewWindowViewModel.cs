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
    class PublishPreviewWindowViewModel : ManagerBase
    {
        private PublishPreviewBoxInfo _previewBoxInfo;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action Callback;

        public ReactiveProperty<PublishPreviewBoxViewModel> TabViewModel { get; private set; }
        public ReactiveProperty<TreeViewModelBase> TabSelectedItem { get; private set; }

        public ReactiveCommand TabClickCommand { get; private set; }

        public ObservableCollection<PublishPreviewItemViewModel> Contents { get; } = new ObservableCollection<PublishPreviewItemViewModel>();
        public ObservableCollection<object> SelectedItems { get; } = new ObservableCollection<object>();

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public PublishPreviewWindowViewModel(PublishPreviewBoxInfo info)
        {
            _previewBoxInfo = info;

            this.Init();
        }

        private void Init()
        {
            {
                this.TabViewModel = new ReactiveProperty<PublishPreviewBoxViewModel>().AddTo(_disposable);
                this.TabViewModel.Value = new PublishPreviewBoxViewModel(null, _previewBoxInfo);

                this.TabSelectedItem = new ReactiveProperty<TreeViewModelBase>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((viewModel) => this.TabSelectChanged(viewModel)).AddTo(_disposable);

                this.TabClickCommand = new ReactiveCommand().AddTo(_disposable);
                this.TabClickCommand.Subscribe(() => this.TabSelectChanged(this.TabSelectedItem.Value)).AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(PublishPreviewWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }
        }

        private void TabSelectChanged(TreeViewModelBase viewModel)
        {
            if (viewModel is PublishPreviewBoxViewModel boxViewModel)
            {
                var list = new List<PublishPreviewItemViewModel>();

                foreach (var item in boxViewModel.Model.BoxInfos)
                {
                    var vm = new PublishPreviewItemViewModel();
                    vm.Icon = AmoebaEnvironment.Icons.BoxIcon;
                    vm.Name = item.Name;
                    vm.Length = GetBoxLength(item);

                    list.Add(vm);
                }

                foreach (var item in boxViewModel.Model.SeedInfos)
                {
                    var vm = new PublishPreviewItemViewModel();
                    vm.Icon = IconUtils.GetImage(item.Name);
                    vm.Name = item.Name;
                    vm.Length = item.Length;

                    list.Add(vm);
                }

                this.Contents.Clear();
                this.Contents.AddRange(list);
            }
        }

        private long GetBoxLength(PublishPreviewBoxInfo boxInfo)
        {
            long value = 0;
            value += boxInfo.SeedInfos.Sum(n => n.Length);
            value += boxInfo.BoxInfos.Sum(n => GetBoxLength(n));

            return value;
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            this.Callback?.Invoke();

            this.OnCloseEvent();
        }

        private void Cancel()
        {
            this.OnCloseEvent();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _settings.Save("Version", 0);
                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
                _settings.Save(nameof(DynamicOptions), this.DynamicOptions.GetProperties(), true);

                _disposable.Dispose();
            }
        }
    }
}
