using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Amoeba.UnitTests
{
    public abstract class TestSetupBase
    {
        private readonly ITestOutputHelper _output;

        public TestSetupBase(ITestOutputHelper output)
        {
            _output = output;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public void WriteLine(string message)
        {
            _output.WriteLine(message);
        }
    }
}
