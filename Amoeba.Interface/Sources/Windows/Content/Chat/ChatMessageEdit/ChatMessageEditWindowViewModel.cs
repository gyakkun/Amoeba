using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using Amoeba.Service;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Security;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class ChatMessageEditWindowViewModel : ManagerBase
    {
        private Tag _tag;
        private ServiceManager _serviceManager;
        private MessageManager _messageManager;

        private Settings _settings;

        private CancellationToken _token;

        public event EventHandler<EventArgs> CloseEvent;

        public ReactiveCommand OkCommand { get; private set; }

        public ReactiveProperty<string> Comment { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public ChatMessageEditWindowViewModel(Tag tag, string comment, ServiceManager serviceManager, MessageManager messageManager, CancellationToken token)
        {
            _tag = tag;
            _serviceManager = serviceManager;
            _messageManager = messageManager;
            _token = token;

            this.Init(comment);
        }

        private void Init(string comment)
        {
            {
                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.Comment = new ReactiveProperty<string>().AddTo(_disposable);
                this.Comment.Value = comment;
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigPath, "View", nameof(ChatMessageEditWindow));
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

        private void Ok()
        {
            var trustSignatures = new HashSet<Signature>(_messageManager.TrustSignatures);

            Miner miner = null;

            if (trustSignatures.Contains(SettingsManager.Instance.AccountInfo.DigitalSignature.GetSignature()))
            {
                miner = new Miner(CashAlgorithm.Version1, 0, TimeSpan.Zero);
            }
            else
            {
                miner = new Miner(CashAlgorithm.Version1, -1, TimeSpan.FromMinutes(3));
            }

            _serviceManager.Upload(_tag, new ChatMessage(this.Comment.Value), SettingsManager.Instance.AccountInfo.DigitalSignature, miner, _token);

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
