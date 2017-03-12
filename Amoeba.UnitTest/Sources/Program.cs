using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using Omnius.Base;

namespace Amoeba.UnitTest
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Program.Init();

            Console.WriteLine("Finish!");
            Console.ReadKey();
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
        }

        private static void LogEvent(object sender, LogEventArgs e)
        {
            Debug.WriteLine($"Log: {e.MessageLevel.ToString()}: {e.Message}");
        }
    }
}
