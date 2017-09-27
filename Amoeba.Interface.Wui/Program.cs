using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Amoeba.Interface
{
    public class Program
    {
        private static IWebHost _host;

        public static void Main(string[] args)
        {
            Amoeba.Run();

            _host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

            _host.Run();
        }

        public static void Exit()
        {
            _host.StopAsync().Wait();
        }
    }
}
