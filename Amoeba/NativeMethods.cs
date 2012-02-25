using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Amoeba
{
    [FlagsAttribute]
    enum ExecutionState : uint
    {
        Null = 0,
        SystemRequired = 1,
        DisplayRequired = 2,
        Continuous = 0x80000000,
    }

    static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public extern static ExecutionState SetThreadExecutionState(ExecutionState esFlags);
    }
}
