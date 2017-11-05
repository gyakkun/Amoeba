using System.IO;
using System.Reflection;
using Xunit.Abstractions;

namespace Amoeba.Tests
{
    public abstract class TestsBase
    {
        private readonly ITestOutputHelper _output;

        public TestsBase(ITestOutputHelper output)
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
