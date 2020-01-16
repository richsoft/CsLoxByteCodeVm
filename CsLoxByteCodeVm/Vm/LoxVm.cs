using CsLoxByteCodeVm.Code;
using CsLoxByteCodeVm.Values;
using CsLoxByteCodeVm.Debugging;
using CsLoxByteCodeVm.Compiler;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Vm
{
    class LoxVm
    {
        private CodeChunk _chunk;
        private int _ip;
        private readonly VmStack _stack;

        public bool DebugTraceExecution { get; set; }

        public LoxVm()
        {
            _stack = new VmStack();
        }

        /// <summary>
        /// Interpret a code chunk
        /// </summary>
        /// <param name="chunk">The code chunk</param>
        /// <returns>The result</returns>
        public InterpretResult Interpret(string source)
        {
            LoxCompiler compiler = new LoxCompiler();

            CodeChunk chunk = new CodeChunk();

            // Try and complie the source code
            if (!compiler.Compile(source, ref chunk)) {
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
                bool r;

                switch (instruction)
                {
                    case (byte)CodeChunk.OpCode.OP_CONSTANT:
                        VmValue constant = ReadConstant();
                        _stack.Push(constant);
                        break;

                    case (byte)CodeChunk.OpCode.OP_NIL: 
                        _stack.Push(VmValue.NilValue()); 
                        break;
                    case (byte)CodeChunk.OpCode.OP_TRUE: 
                        _stack.Push(VmValue.BooleanValue(true));
                        break;
                    case (byte)CodeChunk.OpCode.OP_FALSE: 
                        _stack.Push(VmValue.BooleanValue(false)); 
                        break;

                    case (byte)CodeChunk.OpCode.OP_EQUAL:
                        VmValue b = _stack.Pop();
                        VmValue a = _stack.Pop();
                        _stack.Push(VmValue.BooleanValue(ValuesEqual(a, b)));
                        break;

                    case (byte)CodeChunk.OpCode.OP_ADD:
                        r = BinaryOp(CodeChunk.OpCode.OP_ADD);
                        if (!r) return InterpretResult.RuntimeError;
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
                        _stack.Push(VmValue.BooleanValue(IsFalsy(_stack.Pop())));
                        break;
                    case (byte)CodeChunk.OpCode.OP_NEGATE:
                        if (!_stack.Peek(0).IsNumber())
                        {
                            RuntimeError("Operand must be a number.");
                            return InterpretResult.RuntimeError;
                        }
                        _stack.Push(VmValue.NumberValue(-_stack.Pop().AsNumber()));
                        break;

                    case (byte)CodeChunk.OpCode.OP_RETURN:
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
                        _stack.Push(VmValue.NumberValue(a + b));
                        break;
                    case CodeChunk.OpCode.OP_SUBTRACT:
                        _stack.Push(VmValue.NumberValue(a - b));
                        break;
                    case CodeChunk.OpCode.OP_MULTIPLY:
                        _stack.Push(VmValue.NumberValue(a * b));
                        break;
                    case CodeChunk.OpCode.OP_DIVIDE:
                        _stack.Push(VmValue.NumberValue(a / b));
                        break;
                    case CodeChunk.OpCode.OP_GREATER:
                        _stack.Push(VmValue.BooleanValue(a > b));
                        break;
                    case CodeChunk.OpCode.OP_LESS:
                        _stack.Push(VmValue.BooleanValue(a < b));
                        break;
                }

            }
            return true;
            //while (false);
        }

        /// <summary>
        /// Check if value is falsy (NIL or FALSE)
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if falsy</returns>
        private bool IsFalsy(VmValue value)
        {
            return value.IsNil() || (value.IsBoolean() && !value.AsBoolean());
        }

        /// <summary>
        /// tets equality of values
        /// </summary>
        /// <param name="a">Value a</param>
        /// <param name="b">Value b</param>
        /// <returns>True if a == b</returns>
        private bool ValuesEqual(VmValue a, VmValue b)
        {
            if (a.Type != b.Type) return false;

            switch (a.Type)
            {
                case Values.ValueType.VAL_BOOL: return a.AsBoolean() == b.AsBoolean();
                case Values.ValueType.VAL_NIL: return true;
                case Values.ValueType.VAL_NUMBER: return a.AsNumber() == b.AsNumber();
            }

            return false;

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
        public enum InterpretResult
        {
            OK,
            CompileError,
            RuntimeError
        }


    }
}
