using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Amoeba.UnitTest
{
    [SetUpFixture]
    public class Setup
    {
        [OneTimeSetUp]
        public void Init()
        {
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

            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
        }
    }
}
