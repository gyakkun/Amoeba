using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Omnius.Configuration;
using Prism.Events;

namespace Amoeba.Interface
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	partial class MainWindow : RestorableWindow
	{
		public MainWindow()
		{
			this.DataContext = new MainWindowViewModel();

			if (this.DataContext is ISettings settings)
			{
				settings.Load();
			}

			InitializeComponent();

			this.Icon = AmoebaEnvironment.Icons.AmoebaIcon;

			MainWindowMessenger.ShowEvent.GetEvent<PubSubEvent<OptionsWindowViewModel>>()
				.Subscribe(viewModel =>
				{
					var window = new OptionsWindow(viewModel);
					window.ShowDialog();
				});
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (MessageBoxResult.No == MessageBox.Show(this, "終了しますか？", "Amoeba",
				MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes))
			{
				e.Cancel = true;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			if (this.DataContext is ISettings settings)
			{
				settings.Save();
			}

			if (this.DataContext is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}
}
