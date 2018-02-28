using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amoeba.Daemon
{
    class Options
    {
        [Option('c', "config")]
        public string ConfigFilePath { get; set; }
    }
}
