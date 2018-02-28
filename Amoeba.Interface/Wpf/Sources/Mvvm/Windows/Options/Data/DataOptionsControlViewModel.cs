using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using Amoeba.Messages;
using Amoeba.Rpc;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class DataOptionsControlViewModel : ManagerBase
    {
        private DialogService _dialogService;

        private Settings _settings;

        public DataOptionsInfo Options { get; }

        public ReactiveProperty<string> SelectedItem { get; private set; }

        public ReactiveCommand DownloadDirectoryPathEditDialogCommand { get; private set; }

        public ObservableCollection<int> ProtectedPercentageList { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public DataOptionsControlViewModel(DataOptionsInfo options, DialogService dialogService)
        {
            _dialogService = dialogService;

            this.Options = options;

            this.Init();
        }

        private void Init()
        {
            {
                this.SelectedItem = new ReactiveProperty<string>().AddTo(_disposable);

                this.DownloadDirectoryPathEditDialogCommand = new ReactiveCommand().AddTo(_disposable);
                this.DownloadDirectoryPathEditDialogCommand.Subscribe(() => this.DownloadDirectoryPathEditDialog()).AddTo(_disposable);

                this.ProtectedPercentageList = new ObservableCollection<int>(Enumerable.Range(0, 50 + 1));
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(DataOptionsControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void DownloadDirectoryPathEditDialog()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = this.Options.Download.DirectoryPath;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.Options.Download.DirectoryPath = dialog.SelectedPath;
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
