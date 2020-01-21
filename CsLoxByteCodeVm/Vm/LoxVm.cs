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
        private const int MAX_FRAMES = 64;
        private const int MAX_STACK = (MAX_FRAMES * byte.MaxValue + 1);


        //private CodeChunk _chunk;
        //private int _ip;

        CallFrame[] _frames;
        int _frameCount;

        private readonly VmStack _stack;
        private readonly VmMemoryManager _mem_manager;
        private readonly VmNativeFunctions _native_functions;

        public bool DebugTraceExecution { get; set; }

        public LoxVm()
        {
            _frames = new CallFrame[MAX_FRAMES];
            _stack = new VmStack(MAX_STACK);
            _mem_manager = new VmMemoryManager();
            _native_functions = new VmNativeFunctions(_mem_manager);

            DefineNative("clock", _native_functions.Clock);
        }

        /// <summary>
        /// Interpret a code chunk
        /// </summary>
        /// <param name="chunk">The code chunk</param>
        /// <returns>The result</returns>
        public InterpretResult Interpret(string source)
        {
            LoxCompiler compiler = new LoxCompiler(_mem_manager);

            LoxFunction function = compiler.Compile(source);
            if (function == null) return InterpretResult.INTERPRET_COMPILE_ERROR;

            _stack.Push(LoxValue.FunctionObject(function));
            CallValue(LoxValue.FunctionObject(function), 0);

            return Run();

        }

        /// <summary>
        /// Run the loaded code chuck
        /// </summary>
        /// <returns>The result</returns>
        public InterpretResult Run()
        {
            CallFrame frame = _frames[_frameCount - 1];

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
                    Debug.DisassembleInstruction(frame.Function.Chunk, frame.Ip);
                }

                // Get the instrcution and increment the IP
                byte instruction = ReadByte(frame);

                switch ((CodeChunk.OpCode)instruction)
                {
                    case CodeChunk.OpCode.OP_CONSTANT:
                        {
                            LoxValue constant = ReadConstant(frame);
                            _stack.Push(constant);
                            break;
                        }
                    case CodeChunk.OpCode.OP_NIL:
                        {
                            _stack.Push(LoxValue.NilValue());
                            break;
                        }
                    case CodeChunk.OpCode.OP_TRUE:
                        {
                            _stack.Push(LoxValue.BooleanValue(true));
                            break;
                        }
                    case CodeChunk.OpCode.OP_FALSE:
                        {
                            _stack.Push(LoxValue.BooleanValue(false));
                            break;
                        }
                    case CodeChunk.OpCode.OP_POP:
                        {
                            _stack.Pop();
                            break;
                        }
                    case CodeChunk.OpCode.OP_GET_LOCAL:
                        {
                            byte slot = ReadByte(frame);
                            _stack.Push(_stack[frame.SlotStart + slot]);
                            break;
                        }
                    case CodeChunk.OpCode.OP_SET_LOCAL:
                        {
                            byte slot = ReadByte(frame);
                            _stack[frame.SlotStart + slot] = _stack.Peek(0);
                            break;
                        }
                    case CodeChunk.OpCode.OP_GET_GLOBAL:
                        {
                            LoxString name = ReadString(frame);
                            LoxValue value;
                            if (!_mem_manager.Globals.Get(name, out value))
                            {
                                RuntimeError($"Undefined variable '{name.Value}'.");
                                return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            }
                            _stack.Push(value);
                            break;
                        }
                    case CodeChunk.OpCode.OP_DEFINE_GLOBAL:
                        {
                            LoxString name = ReadString(frame);
                            _mem_manager.Globals.Set(name, _stack.Peek(0));
                            _stack.Pop();
                            break;
                        }
                    case CodeChunk.OpCode.OP_SET_GLOBAL:
                        {
                            LoxString name = ReadString(frame);
                            if (_mem_manager.Globals.Set(name, _stack.Peek(0)))
                            {
                                _mem_manager.Globals.Delete(name);
                                RuntimeError($"Undefined variable '{name.Value}'.");
                                return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            }
                            break;
                        }
                    case CodeChunk.OpCode.OP_EQUAL:
                        {
                            LoxValue b = _stack.Pop();
                            LoxValue a = _stack.Pop();
                            _stack.Push(LoxValue.BooleanValue(a.ValueEquals(b)));
                            break;
                        }
                    case CodeChunk.OpCode.OP_GREATER:
                        {
                            bool r = BinaryOp(CodeChunk.OpCode.OP_GREATER);
                            if (!r) return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            break;
                        }
                    case CodeChunk.OpCode.OP_LESS:
                        {
                            bool r = BinaryOp(CodeChunk.OpCode.OP_LESS);
                            if (!r) return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            break;
                        }
                    case CodeChunk.OpCode.OP_ADD:
                        {
                            if (_stack.Peek(0).IsString() && _stack.Peek(1).IsString())
                            {
                                Concatenate();
                            }
                            else if (_stack.Peek(0).IsNumber() && _stack.Peek(1).IsNumber())
                            {
                                double b = _stack.Pop().AsNumber();
                                double a = _stack.Pop().AsNumber();
                                _stack.Push(LoxValue.NumberValue(a + b));
                            }
                            else
                            {
                                RuntimeError("Operands must be two numbers or two strings.");
                                return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            }
                            break;
                        }
                    case CodeChunk.OpCode.OP_SUBTRACT:
                        {
                            bool r = BinaryOp(CodeChunk.OpCode.OP_SUBTRACT);
                            if (!r) return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            break;
                        }
                    case CodeChunk.OpCode.OP_MULTIPLY:
                        {
                            bool r = BinaryOp(CodeChunk.OpCode.OP_MULTIPLY);
                            if (!r) return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            break;
                        }
                    case CodeChunk.OpCode.OP_DIVIDE:
                        {
                            bool r = BinaryOp(CodeChunk.OpCode.OP_DIVIDE);
                            if (!r) return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            break;
                        }
                    case CodeChunk.OpCode.OP_NOT:
                        {
                            _stack.Push(LoxValue.BooleanValue(_stack.Pop().IsFalsey()));
                            break;
                        }
                    case CodeChunk.OpCode.OP_NEGATE:
                        {
                            if (!_stack.Peek(0).IsNumber())
                            {
                                RuntimeError("Operand must be a number.");
                                return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            }
                            _stack.Push(LoxValue.NumberValue(-_stack.Pop().AsNumber()));
                            break;
                        }

                    case CodeChunk.OpCode.OP_PRINT:
                        {
                            _stack.Pop().PrintValue();
                            Console.WriteLine();
                            break;
                        }
                    case CodeChunk.OpCode.OP_JUMP:
                        {
                            ushort offset = ReadShort(frame);
                            frame.Ip += offset;
                            break;
                        }
                    case CodeChunk.OpCode.OP_JUMP_IF_FALSE:
                        {
                            ushort offset = ReadShort(frame);
                            if (_stack.Peek(0).IsFalsey()) frame.Ip += offset;
                            break;
                        }
                    case CodeChunk.OpCode.OP_LOOP:
                        {
                            ushort offset = ReadShort(frame);
                            frame.Ip -= offset;
                            break;
                        }
                    case CodeChunk.OpCode.OP_CALL:
                        {
                            int arg_count = ReadByte(frame);
                            if (!CallValue(_stack.Peek(arg_count), arg_count))
                            {
                                return InterpretResult.INTERPRET_RUNTIME_ERROR;
                            }
                            frame = _frames[_frameCount - 1];
                            break;
                        }
                    case CodeChunk.OpCode.OP_RETURN:
                        {
                            LoxValue result = _stack.Pop();

                            _frameCount--;
                            // Program ended
                            if (_frameCount == 0)
                            {
                                _stack.Pop();
                                return InterpretResult.OK;
                            }

                            // Reset the stack and push the result
                            _stack.Top = frame.SlotStart;
                            _stack.Push(result);

                            frame = _frames[_frameCount - 1];
                            break;
                        }
                }

            }
        }

        /// <summary>
        /// Read the next byte and increment the IP
        /// </summary>
        /// <returns>The byte</returns>
        private byte ReadByte(CallFrame frame)
        {
            return frame.Function.Chunk.Code[frame.Ip++];
        }

        /// <summary>
        /// Read the next short (16bits) and increment the IP
        /// </summary>
        /// <returns></returns>
        private ushort ReadShort(CallFrame frame)
        {
            // Get the next two bytes and convert to a ushort
            frame.Ip += 2;
            return (ushort)(frame.Function.Chunk.Code[frame.Ip - 2] | frame.Function.Chunk.Code[frame.Ip - 1]);
        }

        /// <summary>
        /// Read a constant using the current byte as a index.  Increments the IP
        /// </summary>
        /// <returns>The constant</returns>
        private LoxValue ReadConstant(CallFrame frame)
        {
            return frame.Function.Chunk.Constants.Values[ReadByte(frame)];
        }

        /// <summary>
        /// Read a constant string using the current byte as the index
        /// </summary>
        /// <returns></returns>
        private LoxString ReadString(CallFrame frame)
        {
            return ((LoxString)ReadConstant(frame).AsObject());
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
        private void Concatenate()
        {
            string b = ((LoxString)_stack.Pop().AsObject()).AsString();
            string a = ((LoxString)_stack.Pop().AsObject()).AsString();

            string s = a + b;

            _stack.Push(LoxValue.StringObject(_mem_manager.AllocateString(s)));

        }

        /// <summary>
        /// Try and call a value
        /// </summary>
        /// <param name="callee">The callee</param>
        /// <param name="arg_count">The argument count</param>
        /// <returns>Treu if we can call</returns>
        private bool CallValue(LoxValue callee, int arg_count)
        {
            if (callee.IsObject())
            {
                switch (callee.AsObject().Type)
                {
                    case LoxObject.ObjectType.OBJ_FUNCTION:
                        {
                            return Call((LoxFunction)(callee.AsObject()), arg_count);
                        }
                    case LoxObject.ObjectType.OBJ_NATIVE:
                        {
                            LoxNativeFunction native = (LoxNativeFunction)(callee.AsObject());
                            // We need to get the items from the stack, as we can't just point at them like in C
                            LoxValue[] args = _stack.GetTop(arg_count);

                            LoxValue result = native.Function(args);

                            // Remove the arguments from the stack
                            _stack.Top -= arg_count + 1;
                            _stack.Push(result);
                            return true;

                        }

                    default:
                        // Not callable
                        break;
                }
            }

            RuntimeError("Can only call functions and classes.");
            return false;
        }

        /// <summary>
        /// Call a function by setting a new call frame
        /// </summary>
        /// <param name="function">The function</param>
        /// <param name="arg_count">The argument count</param>
        /// <returns>True if call success</returns>
        private bool Call(LoxFunction function, int arg_count)
        {
            if (arg_count != function.Arity)
            {
                RuntimeError("Expected {0} arguments but got {1}.", function.Arity, arg_count);
                return false;
            }

            if (_frameCount == MAX_FRAMES)
            {
                RuntimeError("Stack overflow.");
                return false;
            }

            _frames[_frameCount++] = new CallFrame()
            {
                Function = function,
                Ip = 0,
                SlotStart = _stack.Top - arg_count - 1
            };
            return true;
        }

        /// <summary>
        /// Display a runtime error
        /// </summary>
        /// <param name="format">The format</param>
        /// <param name="args">The arguments</param>
        private void RuntimeError(string format, params object[] args)
        {
            Console.Error.WriteLine(format, args);

            //CallFrame frame = _frames[_frameCount - 1];
            //byte instruction = frame.Function.Chunk.Code[frame.Ip];
            //int line = frame.Function.Chunk.Lines[frame.Ip];

            //Console.Error.WriteLine($"[line {line}] in script");

            for (int i = _frameCount - 1; i >= 0; i--)
            {
                CallFrame stack_frame = _frames[i];
                LoxFunction function = stack_frame.Function;

                // -1 because the IP is sitting on the next instruction
                byte instuct = function.Chunk.Code[stack_frame.Ip - 1];
                Console.Error.Write($"[line {function.Chunk.Lines[stack_frame.Ip - 1]}] in ");

                if (function.Name == null)
                {
                    Console.Error.WriteLine("script");
                }
                else
                {
                    Console.Error.WriteLine($"{function.Name.Value}()");
                }
            }

            _stack.Reset();
        }

        /// <summary>
        /// Define a native function
        /// </summary>
        /// <param name="name">The function name</param>
        /// <param name="function">The function to call</param>
        private void DefineNative(string name, Func<LoxValue[], LoxValue> function)
        {
            _stack.Push(LoxValue.StringObject(_mem_manager.AllocateString(name)));
            _stack.Push(LoxValue.NativeFunctionObject(new LoxNativeFunction(function)));

            _mem_manager.Globals.Set((LoxString)(_stack[0].AsObject()), _stack[1]);
            _stack.Pop();
            _stack.Pop();
        }

        public void Dispose()
        {

        }

        public enum InterpretResult
        {
            OK,
            INTERPRET_COMPILE_ERROR,
            INTERPRET_RUNTIME_ERROR
        }

        private class CallFrame
        {
            public LoxFunction Function { get; set; }
            public int Ip { get; set; }
            public int SlotStart { get; set; }

        }
    }
}
