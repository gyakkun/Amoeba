using System;
using System.Windows;

namespace Amoeba.Interface
{
    /// <summary>
    /// PublishPreviewWindow.xaml の相互作用ロジック
    /// </summary>
    partial class PublishPreviewWindow : Window
    {
        public PublishPreviewWindow(PublishPreviewWindowViewModel viewModel)
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
