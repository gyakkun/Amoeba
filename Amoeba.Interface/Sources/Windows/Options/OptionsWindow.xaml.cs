using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Omnius.Configuration;

namespace Amoeba.Interface
{
	/// <summary>
	/// OptionsWindow.xaml の相互作用ロジック
	/// </summary>
	partial class OptionsWindow : RestorableWindow
	{
		public OptionsWindow(OptionsWindowViewModel viewModel)
		{
			this.DataContext = viewModel;

			if (this.DataContext is ISettings settings)
			{
				settings.Load();
			}

			InitializeComponent();
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
