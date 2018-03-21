using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Configuration;
using Amoeba.Rpc;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Amoeba.Messages;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    class CheckBlocksWindowViewModel : ManagerBase
    {
        private AmoebaInterfaceManager _amoebaInterfaceManager;

        private Settings _settings;

        private CancellationTokenSource _tokenSource;
        private Task _task;

        public event EventHandler<EventArgs> CloseEvent;

        public ReactiveProperty<CheckBlocksProgressReport> Info { get; private set; }

        public ReactiveCommand CloseCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public CheckBlocksWindowViewModel(AmoebaInterfaceManager serviceManager)
        {
            _amoebaInterfaceManager = serviceManager;

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
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(CheckBlocksWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(this.DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                EventHooks.Instance.SaveEvent += this.Save;
            }

            {
                var progress = new Action<CheckBlocksProgressReport>(report =>
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
                _task = _amoebaInterfaceManager.CheckBlocks(progress, _tokenSource.Token);
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
                _settings.Save("Version", 0);
                _settings.Save(nameof(this.DynamicOptions), this.DynamicOptions.GetProperties(), true);
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                EventHooks.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
