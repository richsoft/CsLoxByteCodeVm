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

            if (offset >= chunk.Code.Count) return offset;

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
            switch ((CodeChunk.OpCode)instruction)
            {
                case CodeChunk.OpCode.OP_CONSTANT:
                    return ConstantInstruction("OP_CONSTANT", chunk, offset);
                case CodeChunk.OpCode.OP_NIL:
                    return SimpleInstruction("OP_NILL", offset);
                case CodeChunk.OpCode.OP_TRUE:
                    return SimpleInstruction("OP_TRUE", offset);
                case CodeChunk.OpCode.OP_FALSE:
                    return SimpleInstruction("OP_FALSE", offset);
                case CodeChunk.OpCode.OP_POP:
                    return SimpleInstruction("OP_POP", offset);
                case CodeChunk.OpCode.OP_GET_LOCAL:
                    return ByteInstruction("OP_GET_LOCAL", chunk, offset);
                case CodeChunk.OpCode.OP_SET_LOCAL:
                    return ByteInstruction("OP_SET_LOCAL", chunk, offset);
                case CodeChunk.OpCode.OP_GET_GLOBAL:
                    return ConstantInstruction("OP_GET_GLOBAL", chunk, offset);
                case CodeChunk.OpCode.OP_DEFINE_GLOBAL:
                    return ConstantInstruction("OP_DEFINE_GLOBAL", chunk, offset);
                case CodeChunk.OpCode.OP_SET_GLOBAL:
                    return ConstantInstruction("OP_SET_GLOBAL", chunk, offset);
                case CodeChunk.OpCode.OP_EQUAL:
                    return SimpleInstruction("OP_EQUAL", offset);
                case CodeChunk.OpCode.OP_GREATER:
                    return SimpleInstruction("OP_GREATER", offset);
                case CodeChunk.OpCode.OP_LESS:
                    return SimpleInstruction("OP_LESS", offset);
                case CodeChunk.OpCode.OP_ADD:
                    return SimpleInstruction("OP_ADD", offset);
                case CodeChunk.OpCode.OP_SUBTRACT:
                    return SimpleInstruction("OP_SUBTRACT", offset);
                case CodeChunk.OpCode.OP_MULTIPLY:
                    return SimpleInstruction("OP_MULTIPLY", offset);
                case CodeChunk.OpCode.OP_DIVIDE:
                    return SimpleInstruction("OP_DIVIDE", offset);
                case CodeChunk.OpCode.OP_NOT:
                    return SimpleInstruction("OP_NOT", offset);
                case CodeChunk.OpCode.OP_NEGATE:
                    return SimpleInstruction("OP_NEGATE", offset);
                case CodeChunk.OpCode.OP_PRINT:
                    return SimpleInstruction("OP_PRINT", offset);
                case CodeChunk.OpCode.OP_RETURN:
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

        private static int ByteInstruction(string name, CodeChunk chunk, int offset)
        {
            byte slot = chunk.Code[offset + 1];
            Console.WriteLine($"{name,-16} {slot,4}");
            return offset + 2;
        }
    }
}
