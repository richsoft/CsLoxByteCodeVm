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
        public static void DisassembleChunk(CodeChunk chunk, string name)
        {
            Console.WriteLine($"== {name} ==");

            for (int offset = 0; offset < chunk.Code.Count;)
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
        public static int DisassembleInstruction(CodeChunk chunk, int offset)
        {
            Console.Write($"{offset:0000} ");

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
                case (byte)CodeChunk.OpCode.OP_CONSTANT:
                    return ConstantInstruction("OP_CONSTANT", chunk, offset);
                case (byte)CodeChunk.OpCode.OP_NIL:
                    return SimpleInstruction("OP_NILL", offset);
                case (byte)CodeChunk.OpCode.OP_TRUE:
                    return SimpleInstruction("OP_TRUE", offset);
                case (byte)CodeChunk.OpCode.OP_FALSE:
                    return SimpleInstruction("OP_FALSE", offset);
                case (byte)CodeChunk.OpCode.OP_EQUAL:
                    return SimpleInstruction("OP_EQUAL", offset);
                case (byte)CodeChunk.OpCode.OP_GREATER:
                    return SimpleInstruction("OP_GREATER", offset);
                case (byte)CodeChunk.OpCode.OP_LESS:
                    return SimpleInstruction("OP_LESS", offset);
                case (byte)CodeChunk.OpCode.OP_ADD:
                    return SimpleInstruction("OP_ADD", offset);
                case (byte)CodeChunk.OpCode.OP_SUBTRACT:
                    return SimpleInstruction("OP_SUBTRACT", offset);
                case (byte)CodeChunk.OpCode.OP_MULTIPLY:
                    return SimpleInstruction("OP_MULTIPLY", offset);
                case (byte)CodeChunk.OpCode.OP_DIVIDE:
                    return SimpleInstruction("OP_DIVIDE", offset);
                case (byte)CodeChunk.OpCode.OP_NOT:
                    return SimpleInstruction("OP_NOT", offset);
                case (byte)CodeChunk.OpCode.OP_NEGATE:
                    return SimpleInstruction("OP_NEGATE", offset);
                case (byte)CodeChunk.OpCode.OP_RETURN:
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
        private static int ConstantInstruction(string name, CodeChunk chunk, int offset)
        {
            byte constant = chunk.Code[offset + 1];
            Console.Write($"{name,-16} {constant,4} '");
            chunk.Constants.Values[constant].PrintValue();
            Console.WriteLine("'");

            return offset + 2;
        }
    }
}
