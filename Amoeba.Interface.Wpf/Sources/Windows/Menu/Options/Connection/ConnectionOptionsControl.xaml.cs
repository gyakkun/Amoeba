using System.Windows.Controls;
using Omnius.Wpf;

namespace Amoeba.Interface
{
    /// <summary>
    /// Interaction logic for ConnectionOptionsControl.xaml
    /// </summary>
    public partial class ConnectionOptionsControl : UserControl
    {
        public ConnectionOptionsControl()
        {

            dynamic root = new DynamicOptions();
            root.SelectedItem = new DynamicOptions();
            root.SelectedItem.Value = "Connection.Custom.LocationUris";

            this.DataContext = root;
            InitializeComponent();
        }
    }
}
