using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Amoeba.Tests
{
    public abstract class TestSetupBase
    {
        public TestSetupBase()
        {
            Directory.SetCurrentDirectory(@"C:\Local\Projects\Alliance-Network\Amoeba\Amoeba.Tests\bin\Debug\netcoreapp1.1");
        }
    }
}
