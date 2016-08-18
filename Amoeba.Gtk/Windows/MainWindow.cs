using System;

namespace Amoeba
{
    public partial class MainWindow : Gtk.Window
    {
        private ServiceManager _serviceManager = Program.ServiceManager;

        public MainWindow()
            : base(Gtk.WindowType.Toplevel)
        {
            this.Build();
        }
    }
}
