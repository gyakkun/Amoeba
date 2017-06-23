using System;
using System.Windows;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    /// <summary>
    /// PublishDirectoryInfoEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class PublishDirectoryInfoEditWindow : RestorableWindow
    {
        public PublishDirectoryInfoEditWindow(PublishDirectoryInfoEditWindowViewModel viewModel)
        {
            this.DataContext = viewModel;
            viewModel.CloseEvent += (sender, e) => this.Close();

            InitializeComponent();

            this.Icon = AmoebaEnvironment.Icons.Amoeba;
        }

        private void RestorableWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;
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
