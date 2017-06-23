using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Simulation
{
    class MainWindowViewModel : ManagerBase
    {
        private Settings _settings;

        public ObservableCollection<string> Contents { get; } = new ObservableCollection<string>();
        public ReactiveCommand SendReceiveTestsCommand { get; private set; }

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public MainWindowViewModel()
        {
            this.Init();
        }

        private void Init()
        {
            {
                this.SendReceiveTestsCommand = new ReactiveCommand().AddTo(_disposable);
                this.SendReceiveTestsCommand.Subscribe(() => this.SendReceiveTestsRun()).AddTo(_disposable);

                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine("Config", "View", "MainWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
            }

            //{
            //    Debug.Listeners.Add(new MyTraceListener((message) => this.Contents.Add(message)));
            //}
        }

        /*
        private class MyTraceListener : TraceListener
        {
            private Action<string> _callback;

            public MyTraceListener(Action<string> callback)
            {
                _callback = callback;
            }

            public override void Write(string message)
            {
                _callback?.Invoke(message);
            }

            public override void WriteLine(string message)
            {
                _callback?.Invoke(message);
            }
        }
        */

        private void SendReceiveTestsRun()
        {
            Task.Run(() =>
            {
                var test = new CoreManagerTests((message) =>
                {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        this.Contents.Add(message);
                    });
                });
                test.Setup();
                test.Test_SendReceive();
                test.Shutdown();
            });
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
