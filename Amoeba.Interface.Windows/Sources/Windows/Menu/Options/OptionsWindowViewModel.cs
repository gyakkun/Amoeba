using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Omnius.Base;
using Omnius.Configuration;
using Amoeba.Service;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class OptionsWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private Random _random = new Random();

        public event EventHandler<EventArgs> CloseEvent;

        public OptionsInfo Options { get; } = new OptionsInfo();

        public ReactiveProperty<TreeViewItem> TabSelectedItem { get; private set; }

        public AccountOptionsControlViewModel AccountOptionsControlViewModel { get; private set; }
        public ConnectionOptionsControlViewModel ConnectionOptionsControlViewModel { get; private set; }
        public DataOptionsControlViewModel DataOptionsControlViewModel { get; private set; }
        public ViewOptionsControlViewModel ViewOptionsControlViewModel { get; private set; }
        public UpdateOptionsControlViewModel UpdateOptionsControlViewModel { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public OptionsWindowViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();
        }

        private void Init()
        {
            {
                this.TabSelectedItem = new ReactiveProperty<TreeViewItem>().AddTo(_disposable);
                this.TabSelectedItem.Subscribe((item) => this.TabSelectChanged(item)).AddTo(_disposable);

                this.AccountOptionsControlViewModel = new AccountOptionsControlViewModel(_serviceManager);
                this.ConnectionOptionsControlViewModel = new ConnectionOptionsControlViewModel(_serviceManager);
                this.DataOptionsControlViewModel = new DataOptionsControlViewModel(_serviceManager);
                this.ViewOptionsControlViewModel = new ViewOptionsControlViewModel(_serviceManager);
                this.UpdateOptionsControlViewModel = new UpdateOptionsControlViewModel(_serviceManager);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(OptionsWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void TabSelectChanged(TreeViewItem item)
        {
            if (item == null) return;

            this.AccountOptionsControlViewModel.SelectedItem.Value = (string)item.Tag;
            this.ConnectionOptionsControlViewModel.SelectedItem.Value = (string)item.Tag;
            this.DataOptionsControlViewModel.SelectedItem.Value = (string)item.Tag;
            this.ViewOptionsControlViewModel.SelectedItem.Value = (string)item.Tag;
            this.UpdateOptionsControlViewModel.SelectedItem.Value = (string)item.Tag;
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            this.AccountOptionsControlViewModel.SetOptions();
            this.ConnectionOptionsControlViewModel.SetOptions();
            this.DataOptionsControlViewModel.SetOptions();
            this.ViewOptionsControlViewModel.SetOptions();
            this.UpdateOptionsControlViewModel.SetOptions();

            this.OnCloseEvent();
        }

        private void Cancel()
        {
            this.OnCloseEvent();
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("Version", 0);
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
