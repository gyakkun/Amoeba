using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows;
using Omnius.Wpf;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RestorableWindow
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private WindowState _windowState;

        private volatile bool _isSessionEnding = false;
        private volatile bool _isClosed = false;

        private CompositeDisposable _disposable = new CompositeDisposable();

        public MainWindow()
        {
            // NotifyIcon
            {
                var amoebaIcon = new System.Drawing.Icon(System.IO.Path.Combine(AmoebaEnvironment.Paths.IconsPath, "Amoeba.ico"));
                _notifyIcon.Icon = new System.Drawing.Icon(amoebaIcon, new System.Drawing.Size(16, 16));
                _notifyIcon.Visible = true;
            }

            var viewModel = new MainWindowViewModel();

            this.DataContext = viewModel;

            InitializeComponent();

            // NotifyIcon
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Click += (sender, e) =>
                {
                    if (_isClosed) return;

                    try
                    {
                        this.Show();
                        this.Activate();
                        this.WindowState = _windowState;

                        _notifyIcon.Visible = false;
                    }
                    catch (Exception)
                    {

                    }
                };
            }

            this.Setting_Messenger();
            this.Setting_SessionEnding();
        }

        private void Setting_Messenger()
        {
            Messenger.Instance.GetEvent<RelationWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new RelationWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<OptionsWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new OptionsWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<VersionWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new VersionWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<ChatMessageEditWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new ChatMessageEditWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<SearchInfoEditWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new SearchInfoEditWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<PublishDirectoryInfoEditWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new PublishDirectoryInfoEditWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<PublishPreviewWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new PublishPreviewWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<NameEditWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new NameEditWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<ConfirmWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var result = MessageBox.Show(vm.Message, LanguagesManager.Instance.ConfirmWindow_Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                    if (result == MessageBoxResult.OK) vm.Ok();
                }).AddTo(_disposable);
        }

        private void Setting_SessionEnding()
        {
            App.Current.SessionEnding += (sender, e) =>
            {
                _isSessionEnding = true;
                this.Close();
            };
        }

        private void RestorableWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();

                _notifyIcon.Visible = true;
            }
            else
            {
                _windowState = this.WindowState;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_isSessionEnding) return;

            if (ProgressDialog.Instance.Value != 0)
            {
                e.Cancel = true;
                return;
            }

            if (MessageBoxResult.No == MessageBox.Show(this, LanguagesManager.Instance.MainWindow_Close_Message, "Amoeba",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes))
            {
                e.Cancel = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _isClosed = true;

            if (this.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _disposable.Dispose();
        }
    }
}
