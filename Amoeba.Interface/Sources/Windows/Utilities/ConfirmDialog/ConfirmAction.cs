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
        public static readonly DependencyProperty IdentifierProperty =
            DependencyProperty.Register("Identifier", typeof(string), typeof(ConfirmAction), new PropertyMetadata(null));

        public string Identifier
        {
            get { return (string)GetValue(IdentifierProperty); }
            set { SetValue(IdentifierProperty, value); }
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
            context.Confirmed = (bool)await DialogHost.Show(view, this.Identifier);

            args.Callback();
        }
    }
}
