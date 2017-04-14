using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Amoeba.Tests
{
    public abstract class TestSetupBase
    {
        private readonly ITestOutputHelper _output;

        public TestSetupBase(ITestOutputHelper output)
        {
            _output = output;

            Directory.SetCurrentDirectory(@"C:\Local\Projects\Alliance-Network\Amoeba\Amoeba.Tests\bin\Debug\netcoreapp1.1");
        }

        public void WriteLine(string message)
        {
            _output.WriteLine(message);
        }
    }
}
