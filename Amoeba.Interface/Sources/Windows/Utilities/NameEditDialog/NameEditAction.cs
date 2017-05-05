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
    public class NameEditAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty IdentifierProperty =
            DependencyProperty.Register("Identifier", typeof(string), typeof(NameEditAction), new PropertyMetadata(null));

        public string Identifier
        {
            get { return (string)GetValue(IdentifierProperty); }
            set { SetValue(IdentifierProperty, value); }
        }

        protected override async void Invoke(object parameter)
        {
            var args = parameter as InteractionRequestedEventArgs;
            var context = args.Context as Confirmation;

            var viewModel = new NameEditDialogViewModel();
            viewModel.Name.Value = (string)context.Content;

            var view = new NameEditDialogControl { DataContext = viewModel };
            context.Confirmed = (bool)await DialogHost.Show(view, this.Identifier);

            context.Content = viewModel.Name.Value;

            args.Callback();
        }
    }
}
