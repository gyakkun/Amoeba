using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var setup = new SetupManager())
            {
                if (!setup.Run()) return;

                App app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
