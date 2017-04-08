using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Utilities;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class CrowdControlViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;
        private TaskManager _watchTaskManager;

        private Settings _settings;

        public ReactiveCommand CopyLocationCommand { get; private set; }
        public ReactiveCommand PasteLocationCommand { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();
        public ObservableDictionary<byte[], DynamicViewModel> ConnectionInformations { get; } = new ObservableDictionary<byte[], DynamicViewModel>(new ByteArrayEqualityComparer());
        public ObservableDictionary<string, string> Information { get; } = new ObservableDictionary<string, string>();
        public InfoStateViewModel State { get; } = new InfoStateViewModel();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public CrowdControlViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Init();

            _watchTaskManager = new TaskManager(this.WatchThread);
            _watchTaskManager.Start();
        }

        public void Init()
        {
            {
                this.CopyLocationCommand = new ReactiveCommand().AddTo(_disposable);
                this.CopyLocationCommand.Subscribe(() => this.CopyLocation()).AddTo(_disposable);

                this.PasteLocationCommand = new ReactiveCommand().AddTo(_disposable);
                this.PasteLocationCommand.Subscribe(() => this.PasteLocation()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(CrowdControl));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));
            }
        }

        private void WatchThread(CancellationToken token)
        {
            for (;;)
            {
                if (token.WaitHandle.WaitOne(1000)) return;
                {
                    {
                        var dic = new Dictionary<byte[], Information>(new ByteArrayEqualityComparer());

                        foreach (var info in _serviceManager.GetConnectionInformations())
                        {
                            dic.Add(info.GetValue<byte[]>("Id"), info);
                        }

                        App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested) return;

                            foreach (var (key, value) in this.ConnectionInformations.ToArray())
                            {
                                if (!dic.ContainsKey(key))
                                {
                                    this.ConnectionInformations.Remove(key);
                                }
                            }

                            foreach (var (key, info) in dic)
                            {
                                DynamicViewModel viewModel;

                                if (!this.ConnectionInformations.TryGetValue(key, out viewModel))
                                {
                                    viewModel = new DynamicViewModel();
                                    this.ConnectionInformations[key] = viewModel;
                                }

                                foreach (var (name, value) in info)
                                {
                                    viewModel[name] = value;
                                }
                            }
                        });
                    }

                    {
                        App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested) return;

                            this.State.Location = AmoebaConverter.ToLocationString(_serviceManager.MyLocation);
                        });
                    }

                    {
                        var information = _serviceManager.Information;

                        App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (token.IsCancellationRequested) return;

                            foreach (var (key, value) in information)
                            {
                                this.Information[key] = value.ToString();
                            }
                        });
                    }
                }
            }
        }

        public void CopyLocation()
        {

        }

        public void PasteLocation()
        {
            _serviceManager.SetCrowdLocations(Clipboard.GetLocations());
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
                _disposable.Dispose();
            }
        }
    }
}
