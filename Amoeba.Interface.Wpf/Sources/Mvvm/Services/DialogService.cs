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

        public void Show(UploadDirectoryInfoEditWindowViewModel viewModel)
        {
            var window = new UploadDirectoryInfoEditWindow(viewModel);
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
            if (MessageBox.Show(viewModel.Message, LanguagesManager.Instance.ConfirmWindow_Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                viewModel.Ok();
            }
        }

        public void Show(NoticeWindowViewModel viewModel)
        {
            if (MessageBox.Show(viewModel.Message, LanguagesManager.Instance.ConfirmWindow_Title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK) == MessageBoxResult.OK)
            {
                viewModel.Ok();
            }
        }
    }
}
