using System;
using System.Windows;

namespace Amoeba.Interface
{
    /// <summary>
    /// UploadItemsPreviewWindow.xaml の相互作用ロジック
    /// </summary>
    partial class UploadItemsPreviewWindow : Window
    {
        public UploadItemsPreviewWindow(UploadItemsPreviewWindowViewModel viewModel)
        {
            this.DataContext = viewModel;
            viewModel.CloseEvent += (sender, e) => this.Close();

            InitializeComponent();

            this.MouseLeftButtonDown += (sender, e) => this.DragMove();
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
