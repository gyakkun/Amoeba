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
        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register("Id", typeof(string), typeof(NameEditAction), new PropertyMetadata(null));

        public string Id
        {
            get { return (string)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        protected override async void Invoke(object parameter)
        {
            var args = parameter as InteractionRequestedEventArgs;
            var context = args.Context as Confirmation;
            string name = context.Content as string;
            var info = new NameEditInfo() { Name = name };

            var view = new NameEditDialogControl { DataContext = info };
            context.Confirmed = (bool)await DialogHost.Show(view, this.Id);
            if (context.Confirmed) context.Content = info.Name;

            args.Callback();
        }
    }
}
