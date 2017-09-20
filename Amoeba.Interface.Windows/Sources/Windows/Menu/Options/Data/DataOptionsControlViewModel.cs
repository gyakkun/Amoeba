using System;
using System.IO;
using System.Reactive.Disposables;
using Amoeba.Messages;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class DataOptionsControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private DialogService _dialogService;

        private Random _random = new Random();

        public DataOptionsInfo DataOptions { get; } = new DataOptionsInfo();

        public ReactiveProperty<string> SelectedItem { get; private set; }

        public ReactiveCommand DownloadDirectoryPathEditDialogCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public DataOptionsControlViewModel(ServiceManager serviceManager, DialogService dialogService)
        {
            _serviceManager = serviceManager;
            _dialogService = dialogService;

            this.Init();
        }

        private void Init()
        {
            {
                this.SelectedItem = new ReactiveProperty<string>().AddTo(_disposable);

                this.DownloadDirectoryPathEditDialogCommand = new ReactiveCommand().AddTo(_disposable);
                this.DownloadDirectoryPathEditDialogCommand.Subscribe(() => this.DownloadDirectoryPathEditDialog()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(OptionsWindow), nameof(DataOptionsControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            this.GetOptions();

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void GetOptions()
        {
            this.DataOptions.Cache.Size = _serviceManager.Size;
            this.DataOptions.Download.DirectoryPath = _serviceManager.Config.Core.Download.BasePath;
        }

        public void SetOptions()
        {
            if (this.DataOptions.Cache.Size < _serviceManager.Size)
            {
                var viewModel = new ConfirmWindowViewModel(LanguagesManager.Instance.DataOptionsControl_CacheResize_Message);
                viewModel.Callback += () =>
                {
                    ProgressDialog.Instance.Increment();

                    _serviceManager.Resize(this.DataOptions.Cache.Size)
                        .ContinueWith((_) => ProgressDialog.Instance.Decrement());
                };

                _dialogService.Show(viewModel);
            }
            else if (this.DataOptions.Cache.Size > _serviceManager.Size)
            {
                ProgressDialog.Instance.Increment();

                _serviceManager.Resize(this.DataOptions.Cache.Size)
                    .ContinueWith((_) => ProgressDialog.Instance.Decrement());
            }

            lock (_serviceManager.LockObject)
            {
                var oldConfig = _serviceManager.Config;
                _serviceManager.SetConfig(new ServiceConfig(new CoreConfig(oldConfig.Core.Network, new DownloadConfig(this.DataOptions.Download.DirectoryPath)), oldConfig.Connection, oldConfig.Message));
            }
        }

        private void DownloadDirectoryPathEditDialog()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = this.DataOptions.Download.DirectoryPath;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.DataOptions.Download.DirectoryPath = dialog.SelectedPath;
                }
            }
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
