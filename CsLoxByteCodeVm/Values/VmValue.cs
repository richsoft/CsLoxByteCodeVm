using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Values
{
    class VmValue
    {
        public double Value { get; set; }

        public VmValue() { }

        public VmValue(double value)
        {
            Value = value;
        }
    }
}
