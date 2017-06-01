using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    class ProgressDialog
    {
        private MainWindowViewModel _viewModel;
        private int _value = 0;

        private ProgressDialog()
        {
            _viewModel = App.Current.MainWindow.DataContext as MainWindowViewModel;
        }

        public static ProgressDialog Instance { get; } = new ProgressDialog();

        public void Increment()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _value++;
                _viewModel.IsProgressDialogOpen.Value = true;
            });
        }

        public void Decrement()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _value--;
                _viewModel.IsProgressDialogOpen.Value = (_value != 0);
            });
        }
    }
}
