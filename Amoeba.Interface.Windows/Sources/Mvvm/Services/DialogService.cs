using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Amoeba.Interface
{
    class DialogService
    {
        public void Show(RelationWindowViewModel viewModel)
        {
            var window = new RelationWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(OptionsWindowViewModel viewModel)
        {
            var window = new OptionsWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(CheckBlocksWindowViewModel viewModel)
        {
            var window = new CheckBlocksWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(VersionWindowViewModel viewModel)
        {
            var window = new VersionWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(ChatMessageEditWindowViewModel viewModel)
        {
            var window = new ChatMessageEditWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(ChatTagListWindowViewModel viewModel)
        {
            var window = new ChatTagListWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(SearchInfoEditWindowViewModel viewModel)
        {
            var window = new SearchInfoEditWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(UploadPreviewWindowViewModel viewModel)
        {
            var window = new UploadPreviewWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(NameEditWindowViewModel viewModel)
        {
            var window = new NameEditWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(ConfirmWindowViewModel viewModel)
        {
            MessageBox.Show(viewModel.Message, LanguagesManager.Instance.ConfirmWindow_Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        }

        public void Show(NoticeWindowViewModel viewModel)
        {
            MessageBox.Show(viewModel.Message, LanguagesManager.Instance.ConfirmWindow_Title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }
    }
}
