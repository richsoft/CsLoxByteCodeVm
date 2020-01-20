using CsLoxByteCodeVm.Code;
using CsLoxByteCodeVm.Values;
using CsLoxByteCodeVm.Debugging;
using CsLoxByteCodeVm.Compiler;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Vm
{
    class LoxVm : IDisposable
    {
        private CodeChunk _chunk;
        private int _ip;
        private readonly VmStack _stack;
        private readonly VmMemoryManager _mem_manager;

        public bool DebugTraceExecution { get; set; }

        public LoxVm()
        {
            _stack = new VmStack();
            _mem_manager = new VmMemoryManager();
        }

        /// <summary>
        /// Interpret a code chunk
        /// </summary>
        /// <param name="chunk">The code chunk</param>
        /// <returns>The result</returns>
        public InterpretResult Interpret(string source)
        {
            LoxCompiler compiler = new LoxCompiler(_mem_manager);

            CodeChunk chunk = new CodeChunk();

            // Try and complie the source code
            if (!compiler.Compile(source, ref chunk))
            {
                return InterpretResult.CompileError;
            }

            _stack.Reset();
            _chunk = chunk;
            _ip = 0;

            InterpretResult result = Run();
            return result;

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
                    LoxValue[] stack_array = _stack.ToArray();
                    for (int slot = 0; slot < stack_array.Length; slot++)
                    {
                        Console.Write("[ ");
                        stack_array[slot].PrintValue();
                        Console.Write(" ]");
                    }
                    Console.WriteLine("");

                    // Print instruction
                    Debug.DisassembleInstruction(_chunk, _ip);
                }

                // Get the instrcution and increment the IP
                byte instruction = ReadByte();
                bool r;

                switch (instruction)
                {
                    case (byte)CodeChunk.OpCode.OP_CONSTANT:
                        LoxValue constant = ReadConstant();
                        _stack.Push(constant);
                        break;

                    case (byte)CodeChunk.OpCode.OP_NIL:
                        _stack.Push(LoxValue.NilValue());
                        break;
                    case (byte)CodeChunk.OpCode.OP_TRUE:
                        _stack.Push(LoxValue.BooleanValue(true));
                        break;
                    case (byte)CodeChunk.OpCode.OP_FALSE:
                        _stack.Push(LoxValue.BooleanValue(false));
                        break;
                    case (byte)CodeChunk.OpCode.OP_POP:
                        _stack.Pop(); 
                        break;
                    case (byte)CodeChunk.OpCode.OP_EQUAL:
                        LoxValue b = _stack.Pop();
                        LoxValue a = _stack.Pop();
                        _stack.Push(LoxValue.BooleanValue(a.ValueEquals(b)));
                        break;

                    case (byte)CodeChunk.OpCode.OP_GREATER:
                        r = BinaryOp(CodeChunk.OpCode.OP_GREATER);
                        if (!r) return InterpretResult.RuntimeError;
                        break;
                    case (byte)CodeChunk.OpCode.OP_LESS:
                        r = BinaryOp(CodeChunk.OpCode.OP_LESS);
                        if (!r) return InterpretResult.RuntimeError;
                        break;

                    case (byte)CodeChunk.OpCode.OP_ADD:
                        if (_stack.Peek(0).IsString() && _stack.Peek(1).IsString())
                        {
                            Concatenate();
                        }
                        else if (_stack.Peek(0).IsNumber() && _stack.Peek(1).IsNumber())
                        {
                            double b_num = _stack.Pop().AsNumber();
                            double a_num = _stack.Pop().AsNumber();
                            _stack.Push(LoxValue.NumberValue(a_num + b_num));
                        }
                        else
                        {
                            RuntimeError("Operands must be two numbers or two strings.");
                            return InterpretResult.RuntimeError;
                        }
                        break;
                    case (byte)CodeChunk.OpCode.OP_SUBTRACT:
                        r = BinaryOp(CodeChunk.OpCode.OP_SUBTRACT);
                        if (!r) return InterpretResult.RuntimeError;
                        break;
                    case (byte)CodeChunk.OpCode.OP_MULTIPLY:
                        r = BinaryOp(CodeChunk.OpCode.OP_MULTIPLY);
                        if (!r) return InterpretResult.RuntimeError;
                        break;
                    case (byte)CodeChunk.OpCode.OP_DIVIDE:
                        r = BinaryOp(CodeChunk.OpCode.OP_DIVIDE);
                        if (!r) return InterpretResult.RuntimeError;
                        break;
                    case (byte)CodeChunk.OpCode.OP_NOT:
                        _stack.Push(LoxValue.BooleanValue(_stack.Pop().IsFalsy()));
                        break;
                    case (byte)CodeChunk.OpCode.OP_NEGATE:
                        if (!_stack.Peek(0).IsNumber())
                        {
                            RuntimeError("Operand must be a number.");
                            return InterpretResult.RuntimeError;
                        }
                        _stack.Push(LoxValue.NumberValue(-_stack.Pop().AsNumber()));
                        break;

                    case (byte)CodeChunk.OpCode.OP_PRINT:
                        _stack.Pop().PrintValue();
                        Console.WriteLine();
                        break;

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
        private LoxValue ReadConstant()
        {
            return _chunk.Constants.Values[ReadByte()];
        }

        /// <summary>
        /// Binary Operator
        /// </summary>
        /// <param name="op"></param>
        private bool BinaryOp(CodeChunk.OpCode op)
        {
            //do
            {
                if (!_stack.Peek(0).IsNumber() || !_stack.Peek(1).IsNumber())
                {
                    RuntimeError("Operands must be numbers.");
                    return false;
                }

                double b = _stack.Pop().AsNumber();
                double a = _stack.Pop().AsNumber();

                switch (op)
                {
                    case CodeChunk.OpCode.OP_ADD:
                        _stack.Push(LoxValue.NumberValue(a + b));
                        break;
                    case CodeChunk.OpCode.OP_SUBTRACT:
                        _stack.Push(LoxValue.NumberValue(a - b));
                        break;
                    case CodeChunk.OpCode.OP_MULTIPLY:
                        _stack.Push(LoxValue.NumberValue(a * b));
                        break;
                    case CodeChunk.OpCode.OP_DIVIDE:
                        _stack.Push(LoxValue.NumberValue(a / b));
                        break;
                    case CodeChunk.OpCode.OP_GREATER:
                        _stack.Push(LoxValue.BooleanValue(a > b));
                        break;
                    case CodeChunk.OpCode.OP_LESS:
                        _stack.Push(LoxValue.BooleanValue(a < b));
                        break;
                }

            }
            return true;
            //while (false);
        }

        /// <summary>
        /// Concatenate strings
        /// </summary>
        public void Concatenate()
        {
            string b = ((LoxString)_stack.Pop().AsObject()).AsString();
            string a = ((LoxString)_stack.Pop().AsObject()).AsString();

            string s = a + b;

            _stack.Push(LoxValue.StringObject(_mem_manager.AllocateString(s)));

        }

        /// <summary>
        /// Display a runtime error
        /// </summary>
        /// <param name="format">The format</param>
        /// <param name="args">The arguments</param>
        private void RuntimeError(string format, params string[] args)
        {
            Console.Error.WriteLine(string.Format(format, args));

            int line = _chunk.Lines[_ip];
            Console.Error.WriteLine($"[line {line}] in script");

            _stack.Reset();
        }

        public void Dispose()
        {
            
        }

        public enum InterpretResult
        {
            OK,
            CompileError,
            RuntimeError
        }


    }
}
