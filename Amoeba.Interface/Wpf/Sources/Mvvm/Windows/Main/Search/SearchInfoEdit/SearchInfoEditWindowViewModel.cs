using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using Omnius.Base;
using Omnius.Configuration;
using Omnius.Wpf;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    class SearchInfoEditWindowViewModel : ManagerBase
    {
        private SearchInfo _info;

        private Settings _settings;

        public event EventHandler<EventArgs> CloseEvent;
        public event Action<SearchInfo> Callback;

        public ReactiveProperty<string> SearchName { get; private set; }
        public NameSearchConditionsControlViewModel NameSearchConditionsControlViewModel { get; private set; }
        public RegexSearchConditionsControlViewModel RegexSearchConditionsControlViewModel { get; private set; }
        public SignatureSearchConditionsControlViewModel SignatureSearchConditionsControlViewModel { get; private set; }
        public CreationTimeSearchConditionsControlViewModel CreationTimeSearchConditionsControlViewModel { get; private set; }
        public LengthSearchConditionsControlViewModel LengthSearchConditionsControlViewModel { get; private set; }
        public StateSearchConditionsControlViewModel StateSearchConditionsControlViewModel { get; private set; }

        public DynamicOptions DynamicOptions { get; } = new DynamicOptions();

        public ReactiveCommand OkCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _isDisposed;

        public SearchInfoEditWindowViewModel(SearchInfo info)
        {
            _info = info;

            this.Init();
        }

        private void Init()
        {
            {
                this.SearchName = new ReactiveProperty<string>().AddTo(_disposable);
                this.SearchName.Value = _info.Name;

                this.NameSearchConditionsControlViewModel = new NameSearchConditionsControlViewModel(_info.Conditions.SearchNames);
                this.RegexSearchConditionsControlViewModel = new RegexSearchConditionsControlViewModel(_info.Conditions.SearchRegexes);
                this.SignatureSearchConditionsControlViewModel = new SignatureSearchConditionsControlViewModel(_info.Conditions.SearchSignatures);
                this.CreationTimeSearchConditionsControlViewModel = new CreationTimeSearchConditionsControlViewModel(_info.Conditions.SearchCreationTimeRanges);
                this.LengthSearchConditionsControlViewModel = new LengthSearchConditionsControlViewModel(_info.Conditions.SearchLengthRanges);
                this.StateSearchConditionsControlViewModel = new StateSearchConditionsControlViewModel(_info.Conditions.SearchStates);

                this.OkCommand = new ReactiveCommand().AddTo(_disposable);
                this.OkCommand.Subscribe(() => this.Ok()).AddTo(_disposable);

                this.CancelCommand = new ReactiveCommand().AddTo(_disposable);
                this.CancelCommand.Subscribe(() => this.Cancel()).AddTo(_disposable);
            }

            {
                string configPath = System.IO.Path.Combine(AmoebaEnvironment.Paths.ConfigDirectoryPath, "View", nameof(SearchInfoEditWindow));
                if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

                _settings = new Settings(configPath);
                int version = _settings.Load("Version", () => 0);

                this.DynamicOptions.SetProperties(_settings.Load(nameof(this.DynamicOptions), () => Array.Empty<DynamicOptions.DynamicPropertyInfo>()));
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
            _info.Name = this.SearchName.Value;
            _info.Conditions.SearchNames.Clear();
            _info.Conditions.SearchNames.AddRange(this.NameSearchConditionsControlViewModel.GetContents());
            _info.Conditions.SearchRegexes.Clear();
            _info.Conditions.SearchRegexes.AddRange(this.RegexSearchConditionsControlViewModel.GetContents());
            _info.Conditions.SearchSignatures.Clear();
            _info.Conditions.SearchSignatures.AddRange(this.SignatureSearchConditionsControlViewModel.GetContents());
            _info.Conditions.SearchCreationTimeRanges.Clear();
            _info.Conditions.SearchCreationTimeRanges.AddRange(this.CreationTimeSearchConditionsControlViewModel.GetContents());
            _info.Conditions.SearchLengthRanges.Clear();
            _info.Conditions.SearchLengthRanges.AddRange(this.LengthSearchConditionsControlViewModel.GetContents());
            _info.Conditions.SearchStates.Clear();
            _info.Conditions.SearchStates.AddRange(this.StateSearchConditionsControlViewModel.GetContents());

            this.Callback?.Invoke(_info);

            this.OnCloseEvent();
        }

        private void Cancel()
        {
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

                this.NameSearchConditionsControlViewModel.Dispose();
                this.RegexSearchConditionsControlViewModel.Dispose();
                this.SignatureSearchConditionsControlViewModel.Dispose();
                this.CreationTimeSearchConditionsControlViewModel.Dispose();
                this.LengthSearchConditionsControlViewModel.Dispose();
                this.StateSearchConditionsControlViewModel.Dispose();

                _disposable.Dispose();
            }
        }
    }
}
