using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Amoeba.Messages;
using Amoeba.Rpc;
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
        private AmoebaInterfaceManager _amoebaInterfaceManager;
        private MessageManager _messageManager;

        private Settings _settings;

        private CancellationToken _token;

        public event EventHandler<EventArgs> CloseEvent;

        public ReactiveProperty<string> Comment { get; private set; }

        public ReactiveCommand OkCommand { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public ChatMessageEditWindowViewModel(Tag tag, string comment, AmoebaInterfaceManager serviceManager, MessageManager messageManager, CancellationToken token)
        {
            _tag = tag;
            _amoebaInterfaceManager = serviceManager;
            _messageManager = messageManager;
            _token = token;

            this.Init(comment);
        }

        private void Init(string comment)
        {
            {
                this.Comment = new ReactiveProperty<string>().AddTo(_disposable);
                this.Comment.Value = comment;

                this.OkCommand = this.Comment.Select(n => !string.IsNullOrWhiteSpace(n)).ToReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);
            }

            {
                string configPath = Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(ChatMessageEditWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
            }

            {
                EventHooks.Instance.SaveEvent += this.Save;
            }
        }

        private void OnCloseEvent()
        {
            this.CloseEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Ok()
        {
            var trustSignatures = new HashSet<Signature>(_messageManager.TrustSignatures);

            TimeSpan miningTime = TimeSpan.Zero;

            if (!trustSignatures.Contains(SettingsManager.Instance.AccountInfo.DigitalSignature.GetSignature()))
            {
                miningTime = TimeSpan.FromMinutes(3);
            }

            _amoebaInterfaceManager.SetChatMessage(_tag, new ChatMessage(this.Comment.Value), SettingsManager.Instance.AccountInfo.DigitalSignature, miningTime, _token);

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
                EventHooks.Instance.SaveEvent -= this.Save;

                this.Save();

                _disposable.Dispose();
            }
        }
    }
}
