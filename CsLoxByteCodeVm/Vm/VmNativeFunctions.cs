using CsLoxByteCodeVm.Values;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CsLoxByteCodeVm.Vm
{
    class VmNativeFunctions
    {
        private readonly VmMemoryManager _mem_manager;

        public VmNativeFunctions(VmMemoryManager mem_manager)
        {
            _mem_manager = mem_manager;
        }

        public LoxValue Clock(LoxValue[] args)
        {
            DateTime startime = Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return LoxValue.NumberValue((DateTime.UtcNow - startime).TotalSeconds);
        }

    }
}
