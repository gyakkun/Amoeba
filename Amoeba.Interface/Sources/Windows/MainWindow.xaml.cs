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
    public partial class MainWindow : RestorableWindow
    {
        private CompositeDisposable _disposable = new CompositeDisposable();

        public MainWindow()
        {
            var viewModel = new MainWindowViewModel();

            this.DataContext = viewModel;

            InitializeComponent();

            this.Icon = AmoebaEnvironment.Icons.AmoebaIcon;

            Messenger.Instance.GetEvent<OptionsWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new OptionsWindow(vm);
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

            Messenger.Instance.GetEvent<PublishDirectoryInfoEditWindowShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new PublishDirectoryInfoEditWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<NameEditWindowViewModelShowEvent>()
                .Subscribe(vm =>
                {
                    var window = new NameEditWindow(vm);
                    window.Owner = this;
                    window.ShowDialog();
                }).AddTo(_disposable);

            Messenger.Instance.GetEvent<ConfirmWindowViewModelShowEvent>()
                .Subscribe(vm =>
                {
                    var result = MessageBox.Show(vm.Message, "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                    if (result == MessageBoxResult.OK) vm.Ok();
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
