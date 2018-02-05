using System;
using System.Windows;

namespace Amoeba.Interface
{
    /// <summary>
    /// VersionWindow.xaml の相互作用ロジック
    /// </summary>
    partial class VersionWindow : Window
    {
        public VersionWindow(VersionWindowViewModel viewModel)
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
