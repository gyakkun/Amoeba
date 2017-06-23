using System;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    /// <summary>
    /// RelationWindow.xaml の相互作用ロジック
    /// </summary>
    partial class RelationWindow : RestorableWindow
    {
        public RelationWindow(RelationWindowViewModel viewModel)
        {
            this.DataContext = viewModel;
            viewModel.CloseEvent += (sender, e) => this.Close();

            InitializeComponent();

            this.Icon = AmoebaEnvironment.Icons.Amoeba;
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
