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
                var amoebaIcon = new System.Drawing.Icon(System.IO.Path.Combine(AmoebaEnvironment.Paths.IconsDirectoryPath, "Amoeba.ico"));
                _notifyIcon.Icon = new System.Drawing.Icon(amoebaIcon, new System.Drawing.Size(16, 16));
                _notifyIcon.Visible = true;
            }

            var viewModel = new MainWindowViewModel(new DialogService());

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

            this.Setting_SessionEnding();
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

            if (ProgressCircleService.Instance.Value != 0)
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
