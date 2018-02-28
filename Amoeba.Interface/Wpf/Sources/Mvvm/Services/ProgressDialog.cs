namespace Amoeba.Interface
{
    class ProgressDialog
    {
        private int _value = 0;
        private readonly object _lockObject = new object();

        private ProgressDialog()
        {

        }

        public static ProgressDialog Instance { get; } = new ProgressDialog();

        public void Increment()
        {
            if (App.Current.Dispatcher.CheckAccess())
            {
                Function();
            }
            else
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Function();
                });
            }

            void Function()
            {
                lock (_lockObject)
                {
                    var viewModel = App.Current.MainWindow.DataContext as MainWindowViewModel;

                    _value++;
                    viewModel.IsProgressDialogOpen.Value = (_value > 0);
                }
            }
        }

        public void Decrement()
        {
            if (App.Current.Dispatcher.CheckAccess())
            {
                Function();
            }
            else
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Function();
                });
            }

            void Function()
            {
                lock (_lockObject)
                {
                    var viewModel = App.Current.MainWindow.DataContext as MainWindowViewModel;

                    _value--;
                    viewModel.IsProgressDialogOpen.Value = (_value > 0);
                }
            }
        }

        public int Value
        {
            get
            {
                lock (_lockObject)
                {
                    return _value;
                }
            }
        }
    }
}
