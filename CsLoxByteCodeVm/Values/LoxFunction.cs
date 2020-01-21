using CsLoxByteCodeVm.Code;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Values
{
    class LoxFunction : LoxObject
    {
        public int Arity { get; set; }
        public CodeChunk Chunk { get; set; }
        public LoxString Name { get; set; }

        public LoxFunction()
        {
            Name = new LoxString("");
            Arity = 0;
            Chunk = new CodeChunk();
        }

        public override void PrintObject()
        {
            if (Name == null)
            {
                // Top level code
                Console.Write("<script>");
                return;
            }

            Console.Write($"<fn {Name.Value}>");
        }

    }
}
