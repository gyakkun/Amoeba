using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using Omnius.Base;

namespace Amoeba.Test
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Program.Init();

            ThreadPool.SetMinThreads(1024, 1024);

            var test = new Test_CoreManager();
            test.Setup();
            test.Test_SendReceive();
            test.Shutdown();

            Console.WriteLine("Finish!");
            Console.Read();
        }

        private static void Init()
        {
            Log.LogEvent += LogEvent;

            {
                var osInfo = System.Environment.OSVersion;

                // Windows Vista以上。
                if (osInfo.Platform == PlatformID.Win32NT && osInfo.Version >= new Version(6, 0))
                {
                    // SHA256Cngをデフォルトで使うように設定する。
                    CryptoConfig.AddAlgorithm(typeof(SHA256Cng),
                        "SHA256",
                        "SHA256Cng",
                        "System.Security.Cryptography.SHA256",
                        "System.Security.Cryptography.SHA256Cng");
                }
                else
                {
                    // SHA256Managedをデフォルトで使うように設定する。
                    CryptoConfig.AddAlgorithm(typeof(SHA256Managed),
                        "SHA256",
                        "SHA256Managed",
                        "System.Security.Cryptography.SHA256",
                        "System.Security.Cryptography.SHA256Managed");
                }
            }

            Thread.GetDomain().UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                var exception = e.ExceptionObject as Exception;
                if (exception == null) return;

                Log.Error(exception);
            };
        }

        private static void LogEvent(object sender, LogEventArgs e)
        {
            Debug.WriteLine(string.Format("Time:\t\t{0}\r\n" +
                "Level:\t\t{1}\r\n" +
                "{2}",
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), e.MessageLevel, e.Message));
        }
    }
}
