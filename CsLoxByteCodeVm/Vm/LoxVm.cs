using CsLoxByteCodeVm.Code;
using CsLoxByteCodeVm.Values;
using CsLoxByteCodeVm.Debugging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Vm
{
    class LoxVm
    {
        private Chunk _chunk;
        private int _ip;
        private Stack<VmValue> _stack;

        public bool DebugTraceExecution { get; set; }

        /// <summary>
        /// Interpret a code chunk
        /// </summary>
        /// <param name="chunk">The code chunk</param>
        /// <returns>The result</returns>
        public InterpretResult Interpret(Chunk chunk)
        {
            _chunk = chunk;
            _ip = 0;
            _stack = new Stack<VmValue>();
            return Run();
        }

        /// <summary>
        /// Run the loaded code chuck
        /// </summary>
        /// <returns>The result</returns>
        public InterpretResult Run()
        {
            while (true)
            {
                if (DebugTraceExecution)
                {
                    // Print stack
                    Console.Write("          ");
                    VmValue[] stack_array = _stack.ToArray();
                    for(int slot = 0; slot < stack_array.Length; slot++)
                    {
                        Console.Write("[ ");
                        Debug.PrintValue(stack_array[slot]);
                        Console.Write(" ]");
                    }
                    Console.WriteLine("");

                    // Print instruction
                    Debug.DisassembleInstruction(_chunk, _ip);
                }

                // Get the instrcution and increment the IP
                byte instruction = ReadByte();
                
                switch (instruction)
                {
                    case (byte)Chunk.OpCode.OP_CONSTANT:
                        VmValue constant = ReadConstant();
                        _stack.Push(constant);
                        break;

                    case (byte)Chunk.OpCode.OP_ADD:
                        BinaryOp(Chunk.OpCode.OP_ADD);
                            break;
                    case (byte)Chunk.OpCode.OP_SUBTRACT:
                        BinaryOp(Chunk.OpCode.OP_SUBTRACT);
                        break;
                    case (byte)Chunk.OpCode.OP_MULTIPLY:
                        BinaryOp(Chunk.OpCode.OP_MULTIPLY);
                        break;
                    case (byte)Chunk.OpCode.OP_DIVIDE:
                        BinaryOp(Chunk.OpCode.OP_DIVIDE);
                        break;

                    case (byte)Chunk.OpCode.OP_NEGATE:
                        _stack.Push(new VmValue(-_stack.Pop().Value));
                        break;

                    case (byte)Chunk.OpCode.OP_RETURN:
                        Debug.PrintValue(_stack.Pop());
                        Console.WriteLine();
                        return InterpretResult.OK;
                }
                
            }
        }

        /// <summary>
        /// Read the next byte and increment the IP
        /// </summary>
        /// <returns></returns>
        private byte ReadByte()
        {
            return _chunk.Code[_ip++];
        }

        /// <summary>
        /// Read a constant using the current byte as a index.  Increments the IP
        /// </summary>
        /// <returns>The constant</returns>
        private VmValue ReadConstant()
        {
            return _chunk.Constants.Values[ReadByte()];
        }

        /// <summary>
        /// Binary Operator
        /// </summary>
        /// <param name="op"></param>
        private void BinaryOp(Chunk.OpCode op)
        {
            //do
            {
                double b = _stack.Pop().Value;
                double a = _stack.Pop().Value;

                switch (op)
                {
                    case Chunk.OpCode.OP_ADD:
                        _stack.Push(new VmValue(a + b));
                        break;
                    case Chunk.OpCode.OP_SUBTRACT:
                        _stack.Push(new VmValue(a - b));
                        break;
                    case Chunk.OpCode.OP_MULTIPLY:
                        _stack.Push(new VmValue(a * b));
                        break;
                    case Chunk.OpCode.OP_DIVIDE:
                        _stack.Push(new VmValue(a / b));
                        break;

                }

            }
            //while (false);
        }

        public enum InterpretResult
        {
            OK,
            ComplileError,
            RuntimeError
        }


    }
}
