using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
    static class ProcessExtensions
    {
        [DllImport("ntdll.dll")]
        private static extern uint NtSetInformationProcess(IntPtr processHandle, uint processInformationClass, ref uint processInformation, uint processInformationLength);

        private const uint ProcessInformationMemoryPriority = 0x27;

        public static void SetMemoryPriority(this Process process, int priority)
        {
            uint memoryPriority = (uint)priority;
            ProcessExtensions.NtSetInformationProcess(process.Handle, ProcessExtensions.ProcessInformationMemoryPriority, ref memoryPriority, sizeof(uint));
        }
    }

}
