using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class UploadDirectoryInfoEditWindowViewModel : ManagerBase
    {
        private UploadDirectoryInfo _directoryInfo;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action<UploadDirectoryInfo> Callback;

        public ReactiveProperty<string> Name { get; private set; }
        public ReactiveProperty<string> Path { get; private set; }

        public ReactiveCommand EditDialogCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public UploadDirectoryInfoEditWindowViewModel(UploadDirectoryInfo info)
        {
            _directoryInfo = info;

            this.Init();

            this.Name.Value = _directoryInfo.Name;
            this.Path.Value = _directoryInfo.Path;
        }

        private void Init()
        {
            {
                this.Name = new ReactiveProperty<string>().AddTo(_disposable);
                this.Path = new ReactiveProperty<string>().AddTo(_disposable);

                this.EditDialogCommand = new ReactiveCommand().AddTo(_disposable);
                this.EditDialogCommand.Subscribe(() => this.EditDialog()).AddTo(_disposable);

                this.OkCommand = this.Name.Select(n => !string.IsNullOrWhiteSpace(n)).ToReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);
            }

            {
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(UploadDirectoryInfoEditWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void EditDialog()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = this.Path.Value;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.Name.Value = System.IO.Path.GetFileName(dialog.SelectedPath);
                    this.Path.Value = dialog.SelectedPath;
                }
            }
        }

        private void Ok()
        {
            if (!string.IsNullOrWhiteSpace(this.Name.Value)
                && !string.IsNullOrWhiteSpace(this.Path.Value))
            {
                _directoryInfo.Name = this.Name.Value;
                _directoryInfo.Path = this.Path.Value;

                this.Callback?.Invoke(_directoryInfo);
            }

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
