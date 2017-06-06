using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Omnius.Security;

namespace Amoeba.Interface
{
    class PublishDirectoryInfoEditWindowViewModel : ManagerBase
    {
        private PublishDirectoryInfo _directoryInfo;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action<PublishDirectoryInfo> Callback;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }
        public ReactiveProperty<string> Name { get; private set; }
        public ReactiveProperty<string> Path { get; private set; }

        public ReactiveCommand EditDialogCommand { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public PublishDirectoryInfoEditWindowViewModel(PublishDirectoryInfo info)
        {
            _directoryInfo = info;

            this.Init();

            this.Name.Value = _directoryInfo.Name;
            this.Path.Value = _directoryInfo.Path;

            if (string.IsNullOrWhiteSpace(this.Path.Value))
            {
                this.EditDialog();
            }
        }

        private void Init()
        {
            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
                this.Name = new ReactiveProperty<string>().AddTo(_disposable);
                this.Path = new ReactiveProperty<string>().AddTo(_disposable);

                this.EditDialogCommand = new ReactiveCommand().AddTo(_disposable);
                this.EditDialogCommand.Subscribe(() => this.EditDialog()).AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);
            }

            {
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(PublishDirectoryInfoEditWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
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
            _directoryInfo.Name = this.Name.Value;
            _directoryInfo.Path = this.Path.Value;

            this.Callback?.Invoke(_directoryInfo);

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

                _disposable.Dispose();
            }
        }
    }
}
