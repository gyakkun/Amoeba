using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
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

        public ObservableDictionary<byte[], DynamicViewModel> ConnectionInformations { get; } = new ObservableDictionary<byte[], DynamicViewModel>(new ByteArrayEqualityComparer());
        public ObservableCollection<object> ConnectionSelectedItems { get; } = new ObservableCollection<object>();

        public ReactiveCommand ConnectionCopyCommand { get; private set; }
        public ReactiveCommand ConnectionPasteCommand { get; private set; }

        public CrowdStateInfo State { get; } = new CrowdStateInfo();
        public ObservableDictionary<string, string> Information { get; } = new ObservableDictionary<string, string>();
        public ObservableCollection<object> StateSelectedItems { get; } = new ObservableCollection<object>();

        public ReactiveCommand StateCopyCommand { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();

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
                this.ConnectionCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.ConnectionCopyCommand.Subscribe(() => this.ConnectionCopy()).AddTo(_disposable);

                this.ConnectionPasteCommand = new ReactiveCommand().AddTo(_disposable);
                this.ConnectionPasteCommand.Subscribe(() => this.ConnectionPaste()).AddTo(_disposable);

                this.StateCopyCommand = new ReactiveCommand().AddTo(_disposable);
                this.StateCopyCommand.Subscribe(() => this.StateCopy()).AddTo(_disposable);
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
            var sizeTypeHashSet = new HashSet<string>()
            {
                "Cache_FreeSpace",
                "Cache_LockSpace",
                "Cache_UsingSpace",
                "Network_ReceivedByteCount",
                "Network_SentByteCount",
            };

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

                            foreach (var (i, key, info) in dic.Select((item, i) => (i, item.Key, item.Value)))
                            {
                                DynamicViewModel viewModel;

                                if (!this.ConnectionInformations.TryGetValue(key, out viewModel))
                                {
                                    viewModel = new DynamicViewModel();
                                    this.ConnectionInformations[key] = viewModel;
                                }

                                viewModel.SetValue("Index", i);

                                foreach (var (name, value) in info)
                                {
                                    viewModel.SetValue(name, value);
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
                                if (sizeTypeHashSet.Contains(key))
                                {
                                    this.Information[key] = NetworkConverter.ToSizeString((long)value);
                                }
                                else
                                {
                                    this.Information[key] = value.ToString();
                                }
                            }
                        });
                    }
                }
            }
        }

        public void ConnectionCopy()
        {
            var list = new List<Location>();

            foreach (var (key, value) in this.ConnectionSelectedItems.Cast<KeyValuePair<byte[], DynamicViewModel>>())
            {
                list.Add(value.GetValue<Location>("Location"));
            }

            Clipboard.SetLocations(list);
        }

        public void ConnectionPaste()
        {
            _serviceManager.SetCrowdLocations(Clipboard.GetLocations());
        }

        public void StateCopy()
        {
            var sb = new StringBuilder();

            foreach (var (key, value) in this.StateSelectedItems.Cast<KeyValuePair<string, string>>())
            {
                sb.AppendLine($"{key}: {value}");
            }

            Clipboard.SetText(sb.ToString());
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
