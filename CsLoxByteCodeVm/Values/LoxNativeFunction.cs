using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Values
{
    class LoxNativeFunction : LoxObject
    {
        public Func<LoxValue[], LoxValue> Function { get; }

        public LoxNativeFunction(Func<LoxValue[], LoxValue> function) : base(ObjectType.OBJ_NATIVE)
        {
            Function = function;
        }

        public override void PrintObject()
        {
            Console.Write("<native fn>");
        }
    }
}
