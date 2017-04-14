using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Omnius.Configuration;
using Omnius.Wpf;
using Prism.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Amoeba.Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    partial class MainWindow : RestorableWindow
    {
        private CompositeDisposable _disposable = new CompositeDisposable();

        public MainWindow()
        {
            var viewModel = new MainWindowViewModel();

            this.DataContext = viewModel;

            InitializeComponent();

            this.Icon = AmoebaEnvironment.Icons.AmoebaIcon;

            MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<OptionsWindowViewModel>>()
                .Subscribe(vm =>
                {
                    var window = new OptionsWindow(vm);
                    window.ShowDialog();
                }).AddTo(_disposable);
            MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<ChatMessageEditWindowViewModel>>()
                .Subscribe(vm =>
                {
                    var window = new ChatMessageEditWindow(vm);
                    window.ShowDialog();
                }).AddTo(_disposable);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (MessageBoxResult.No == MessageBox.Show(this, "終了しますか？", "Amoeba",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes))
            {
                e.Cancel = true;
            }
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
