using CsLoxByteCodeVm.Values;
using CsLoxByteCodeVm.Code;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Debugging
{
    class Debug
    {
        /// <summary>
        /// Disassemble a chunk of code, and display on the console
        /// </summary>
        /// <param name="chunk">The chunk to disassemble</param>
        /// <param name="name">The name</param>
        public static void DisassembleChunk(Chunk chunk, string name)
        {
            Console.WriteLine($"== {name} ==");

            for (int offset = 0; offset < chunk.Code.Count; )
            {
                offset = DisassembleInstruction(chunk, offset);
            }
        }

        /// <summary>
        /// Disassemble a single instruction
        /// </summary>
        /// <param name="chunk">The code chunk</param>
        /// <param name="offset">The code offset</param>
        /// <returns>The new offset</returns>
        public static int DisassembleInstruction(Chunk chunk, int offset)
        {
            Console.Write($"{offset:0000} " );

            // Line number
            if (offset > 0 && chunk.Lines[offset] == chunk.Lines[offset - 1])
            {
                // Same line as previous
                Console.Write("   | ");
            }
            else
            {
                Console.Write($"{chunk.Lines[offset]:0000} ");
            }

            // Instruction
            byte instruction = chunk.Code[offset];
            switch (instruction)
            {
                case (byte)Chunk.OpCode.OP_CONSTANT:
                    return ConstantInstruction("OP_CONSTANT", chunk, offset);
                case (byte)Chunk.OpCode.OP_ADD:
                    return SimpleInstruction("OP_ADD", offset);
                case (byte)Chunk.OpCode.OP_SUBTRACT:
                    return SimpleInstruction("OP_SUBTRACT", offset);
                case (byte)Chunk.OpCode.OP_MULTIPLY:
                    return SimpleInstruction("OP_MULTIPLY", offset);
                case (byte)Chunk.OpCode.OP_DIVIDE:
                    return SimpleInstruction("OP_DIVIDE", offset);
                case (byte)Chunk.OpCode.OP_NEGATE:
                    return SimpleInstruction("OP_NEGATE", offset);
                case (byte)Chunk.OpCode.OP_RETURN:
                    return SimpleInstruction("OP_RETURN", offset);
                default:
                    Console.WriteLine($"Unknown opcode {instruction}");
                    return offset + 1;
            }
        }

        /// <summary>
        /// Print a simple instruction
        /// </summary>
        /// <param name="name">The opcode name</param>
        /// <param name="offset">The code offset</param>
        /// <returns>The new offset</returns>
        private static int SimpleInstruction(string name, int offset)
        {
            Console.WriteLine($"{name}");
            return offset + 1;
        }

        /// <summary>
        /// Print a constant instruction
        /// </summary>
        /// <param name="name">The opcode name</param>
        /// <param name="chunk">The code chunk</param>
        /// <param name="offset">The code offset</param>
        /// <returns>The new offset</returns>
        private static int ConstantInstruction(string name, Chunk chunk, int offset)
        {
            byte constant = chunk.Code[offset + 1];
            Console.Write($"{name,-16} {constant,4} '");
            PrintValue(chunk.Constants.Values[constant]);
            Console.WriteLine("'");

            return offset + 2;
        }

        /// <summary>
        /// Print a value
        /// </summary>
        /// <param name="value">The value</param>
        public static void PrintValue(VmValue value)
        {
            Console.Write($"{value.Value}");
        }
    }
}
