using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Linq;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using Amoeba.Messages;
using Omnius.Collections;
using System.Threading;
using System.Net;

namespace Amoeba.Simulation
{
    class MainWindowViewModel : ManagerBase
    {
        private Settings _settings;

        public ObservableCollection<string> Contents { get; } = new ObservableCollection<string>();

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();

        private volatile bool _isDisposed;

        public MainWindowViewModel()
        {
            this.Init();

            this.SendReceiveTestsRun();
        }

        private void Init()
        {
            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
            }

            {
                string configPath = Path.Combine("Config", "View", "MainWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
            }
        }

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

        private void SendReceiveTestsRun()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            if (args.Length == 2 && args[0] == "Upload")
            {
                Debug.Listeners.Add(new MyTraceListener((message) =>
                {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        this.Contents.Add(message);
                    });
                }));

                Debug.WriteLine(string.Join(" ", args));

                Task.Run(() =>
                {
                    int port = int.Parse(args[1]);

                    using (var simulationManager = new SimulationManager(port, (message) => Debug.WriteLine(message)))
                    {
                        simulationManager.Setup();
                        simulationManager.MessageUpload();

                        Thread.Sleep(1000 * 60 * 60 * 24);
                    }

                    App.Current.Shutdown();
                });
            }
            else
            {
                Debug.WriteLine("Download");

                Task.Run(() =>
                {
                    var tuples = new LockedList<(Metadata metadata, Hash hash)>();
                    const int maxNodeCount = 40;

                    var resetEvent = new ManualResetEvent(false);

                    Task.Run(() =>
                    {
                        Parallel.For(0, maxNodeCount, (i) =>
                        {
                            var startInfo = new ProcessStartInfo(@"Amoeba.Simulation.exe");
                            startInfo.CreateNoWindow = true;
                            startInfo.Arguments = $"Upload {i + 60000}";
                            startInfo.RedirectStandardOutput = true;
                            startInfo.RedirectStandardInput = true;
                            startInfo.UseShellExecute = false;

                            using (var process = Process.Start(startInfo))
                            {
                                process.WaitForInputIdle();

                                using (var metadataMemoryStream = new MemoryStream(NetworkConverter.FromBase64UrlString(process.StandardOutput.ReadLine())))
                                using (var hashMemoryStream = new MemoryStream(NetworkConverter.FromBase64UrlString(process.StandardOutput.ReadLine())))
                                {
                                    tuples.Add((Metadata.Import(metadataMemoryStream, BufferManager.Instance), HashConverter.FromStream(hashMemoryStream)));

                                    if (tuples.Count == maxNodeCount) resetEvent.Set();
                                }

                                process.WaitForExit();
                            }
                        });
                    });

                    resetEvent.WaitOne();

                    var cloudLocations = new List<Location>();
                    {
                        foreach (var uri in Enumerable.Range(0, maxNodeCount)
                            .Select(n => n + 60000).Select(port => $"{IPAddress.Loopback}:{port}"))
                        {
                            cloudLocations.Add(new Location(new string[] { uri }));
                        }
                    }

                    using (var simulationManager = new SimulationManager(50000, (message) =>
                    {
                        App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            this.Contents.Add(message);
                        });
                    }))
                    {
                        simulationManager.Setup();
                        simulationManager.SetCloudLocations(cloudLocations);

                        simulationManager.MessageDownload(tuples);
                    }
                });
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (isDisposing)
            {
                _settings.Save("Version", 0);
                _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);

                _disposable.Dispose();
            }
        }
    }
}
