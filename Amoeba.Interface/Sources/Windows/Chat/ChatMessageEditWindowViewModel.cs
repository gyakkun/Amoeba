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
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Omnius.Security;

namespace Amoeba.Interface
{
    class ChatMessageEditWindowViewModel : ManagerBase
    {
        private ServiceManager _serviceManager;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;

        public ReactiveProperty<WindowSettings> WindowSettings { get; private set; }
        public ReactiveProperty<string> Comment { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }

        public DynamicViewModel Config { get; } = new DynamicViewModel();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ChatMessageEditWindowViewModel(ServiceManager serviceManager)
        {
            _serviceManager = serviceManager;

            this.Load();
        }

        public void Load()
        {
            {
                this.WindowSettings = new ReactiveProperty<WindowSettings>().AddTo(_disposable);
                this.Comment = new ReactiveProperty<string>().AddTo(_disposable);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok());
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", "ChatMessageEditWindow");
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                this.WindowSettings.Value = _settings.Load(nameof(WindowSettings), () => new WindowSettings());
                this.Config.SetPairs(_settings.Load("Config", () => new Dictionary<string, object>()));
            }
        }

        public void Save()
        {
            _settings.Save(nameof(WindowSettings), this.WindowSettings.Value);
            _settings.Save("Config", this.Config.GetPairs());
        }

        private void OnClose()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            var tag = new Tag("Amoeba", Sha256.ComputeHash("Amoeba"));
            var miner = new Miner(CashAlgorithm.Version1, 0, TimeSpan.Zero);
            _serviceManager.Upload(tag, new ChatMessage(this.Comment.Value), new DigitalSignature("_TEST_", DigitalSignatureAlgorithm.EcDsaP521_Sha256), miner, new CancellationToken());

            this.OnClose();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
