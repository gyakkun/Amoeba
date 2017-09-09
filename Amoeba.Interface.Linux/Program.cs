using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Amoeba.Interface
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Amoeba.Run();

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
