using System;
using System.Reactive.Disposables;
using Omnius.Wpf;

namespace Amoeba.Simulation
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : RestorableWindow
    {
        private CompositeDisposable _disposable = new CompositeDisposable();

        public MainWindow()
        {
            var viewModel = new MainWindowViewModel();

            this.DataContext = viewModel;

            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (this.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _disposable.Dispose();
        }
    }
}
