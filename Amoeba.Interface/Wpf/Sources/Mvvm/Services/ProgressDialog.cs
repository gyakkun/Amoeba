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
            App.Current.Dispatcher.Invoke(() =>
            {
                lock (_lockObject)
                {
                    var viewModel = App.Current.MainWindow.DataContext as MainWindowViewModel;

                    _value++;
                    viewModel.IsProgressDialogOpen.Value = true;
                }
            });
        }

        public void Decrement()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                lock (_lockObject)
                {
                    var viewModel = App.Current.MainWindow.DataContext as MainWindowViewModel;

                    _value--;
                    viewModel.IsProgressDialogOpen.Value = (_value != 0);
                }
            });
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
