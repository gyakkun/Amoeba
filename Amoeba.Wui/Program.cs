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
                ServiceManager serviceManager = null;

                try
                {
                    serviceManager = new ServiceManager();

                    if (!serviceManager.Startup(args))
                    {
                        serviceManager.Dispose();
                        return;
                    }
                }
                catch (Exception)
                {
                    if (serviceManager != null)
                    {
                        serviceManager.Dispose();
                    }

                    return;
                }

                _serviceManager = serviceManager;
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
