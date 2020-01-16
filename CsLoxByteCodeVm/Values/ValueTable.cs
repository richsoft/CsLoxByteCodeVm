using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Values
{
    class ValueTable
    {
        public List<VmValue> Values { get; set; }

        public ValueTable()
        {
            Values = new List<VmValue>();
        }

    }
}
