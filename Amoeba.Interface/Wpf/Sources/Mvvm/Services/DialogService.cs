using System.Windows;

namespace Amoeba.Interface
{
    class DialogService
    {
        public void ShowDialog(RelationWindowViewModel viewModel)
        {
            var window = new RelationWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void ShowDialog(OptionsWindowViewModel viewModel)
        {
            var window = new OptionsWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void Show(CheckBlocksWindowViewModel viewModel)
        {
            var window = new CheckBlocksWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.Show();
        }

        public void ShowDialog(VersionWindowViewModel viewModel)
        {
            var window = new VersionWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void ShowDialog(ChatMessageEditWindowViewModel viewModel)
        {
            var window = new ChatMessageEditWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void ShowDialog(ChatTagListWindowViewModel viewModel)
        {
            var window = new ChatTagListWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void ShowDialog(SearchInfoEditWindowViewModel viewModel)
        {
            var window = new SearchInfoEditWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void ShowDialog(UploadDirectoryInfoEditWindowViewModel viewModel)
        {
            var window = new UploadDirectoryInfoEditWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void ShowDialog(UploadItemsPreviewWindowViewModel viewModel)
        {
            var window = new UploadItemsPreviewWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public void ShowDialog(NameEditWindowViewModel viewModel)
        {
            var window = new NameEditWindow(viewModel);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        public MessageBoxResult ShowDialog(string message, MessageBoxButton button, MessageBoxImage image, MessageBoxResult defaultResult)
        {
            return MessageBox.Show(message, LanguagesManager.Instance.ConfirmWindow_Title, button, image, defaultResult);
        }
    }
}
