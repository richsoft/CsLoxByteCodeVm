using CsLoxByteCodeVm.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Code
{
    class Chunk
    {
        public List<byte> Code { get; }
        public List<int> Lines { get; }
        public ValueTable Constants { get; }


        public Chunk()
        {
            Code = new List<byte>();
            Lines = new List<int>();
            Constants = new ValueTable();
        }

        /// <summary>
        /// Add an opcode
        /// </summary>
        /// <param name="op_code">The op code</param>
        public void WriteChunk(OpCode op_code, int line)
        {
            WriteChunk((byte)op_code, line);
        }

        /// <summary>
        /// Add a literal
        /// </summary>
        /// <param name="literal">The literal value</param>
        public void WriteChunk(int literal, int line)
        {
            Code.Add((byte)literal);
            Lines.Add(line);
        }

        /// <summary>
        /// Add a constant
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The index of the constant</returns>
        public int AddConstant(VmValue value)
        {
            Constants.Values.Add(value);
            return Constants.Values.Count - 1;
        }

        public enum OpCode
        {
            OP_CONSTANT,
            OP_ADD,
            OP_SUBTRACT,
            OP_MULTIPLY,
            OP_DIVIDE,
            OP_NEGATE,
            OP_RETURN
        }


    }
}
