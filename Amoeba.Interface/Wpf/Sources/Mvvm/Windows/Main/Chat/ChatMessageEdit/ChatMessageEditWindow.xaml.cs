using System;
using System.Windows;

namespace Amoeba.Interface
{
    /// <summary>
    /// ChatMessageEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ChatMessageEditWindow : Window
    {
        public ChatMessageEditWindow(ChatMessageEditWindowViewModel viewModel)
        {
            this.DataContext = viewModel;
            viewModel.CloseEvent += (sender, e) => this.Close();

            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (this.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
