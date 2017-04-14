using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using MaterialDesignThemes.Wpf;
using Prism.Interactivity.InteractionRequest;

namespace Amoeba.Interface
{
    public enum ConfirmDialogType
    {
        Delete,
    }

    public class ConfirmAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register("Id", typeof(string), typeof(ConfirmAction), new PropertyMetadata(null));

        public string Id
        {
            get { return (string)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        protected override async void Invoke(object parameter)
        {
            var args = parameter as InteractionRequestedEventArgs;
            var context = args.Context as Confirmation;
            string message = null;

            if (context.Content is ConfirmDialogType type)
            {
                if (type == ConfirmDialogType.Delete)
                {
                    message = LanguagesManager.Instance.ConfirmDialog_DeleteMessage;
                }
            }

            var view = new ConfirmDialogControl { DataContext = message };
            context.Confirmed = (bool)await DialogHost.Show(view, this.Id);

            args.Callback();
        }
    }
}
