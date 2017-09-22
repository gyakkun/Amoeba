using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Configuration;
using Amoeba.Service;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Amoeba.Messages;

namespace Amoeba.Interface
{
    class CheckBlocksWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        private CancellationTokenSource _tokenSource;
        private Task _task;

        public event EventHandler<EventArgs> CloseEvent;

        public ReactiveProperty<CheckBlocksProgressReport> Info { get; private set; }

        public ReactiveCommand CloseCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public CheckBlocksWindowViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();
        }

        private void Init()
        {
            {
                this.Info = new ReactiveProperty<CheckBlocksProgressReport>().AddTo(_disposable);

                this.CloseCommand = new ReactiveCommand().AddTo(_disposable);
                this.CloseCommand.Subscribe(() => this.Close()).AddTo(_disposable);
            }

            {
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(CheckBlocksWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);
            }

            {
                Backup.Instance.SaveEvent += this.Save;
            }

            {
                var progress = new Progress<CheckBlocksProgressReport>(report =>
                {
                    try
                    {
                        App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            this.Info.Value = report;
                        });
                    }
                    catch (Exception)
                    {

                    }
                });

                _tokenSource = new CancellationTokenSource();
                _task = _serviceManager.CheckBlocks(progress, _tokenSource.Token);
            }
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Close()
        {
            try
            {
                _tokenSource.Cancel();
                _task.Wait();
            }
            catch (Exception)
            {

            }

            this.OnCloseEvent();
        }

        private void Save()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _settings.Save("CheckBlocks", 0);
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
