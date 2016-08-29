using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba
{
    class Program
    {
        private static ServiceManager _serviceManager;

        public static void Main(string[] args)
        {
            // Startup
            {
                var serviceManager = new ServiceManager();

                if (!serviceManager.Startup(args))
                {
                    serviceManager.Dispose();
                    serviceManager = null;

                    return;
                }
                else
                {
                    _serviceManager = serviceManager;
                }
            }
        }

        public static ServiceManager ServiceManager
        {
            get
            {
                return _serviceManager;
            }
        }
    }
}
