using System;
using CsLoxByteCodeVm.Debugging;
using CsLoxByteCodeVm.Values;
using CsLoxByteCodeVm.Code;
using CsLoxByteCodeVm.Vm;

namespace CsLoxByteCodeVm
{
    class Program
    {
        static int Main(string[] args)
        {
            Chunk chunk = new Chunk();

            // Add a constant
            int constant = chunk.AddConstant(new VmValue(1.2));
            chunk.WriteChunk(Chunk.OpCode.OP_CONSTANT, 123);
            chunk.WriteChunk(constant, 123);

            constant = chunk.AddConstant(new VmValue(3.4));
            chunk.WriteChunk(Chunk.OpCode.OP_CONSTANT, 123);
            chunk.WriteChunk(constant, 123);

            chunk.WriteChunk(Chunk.OpCode.OP_ADD, 123);

            constant = chunk.AddConstant(new VmValue(5.6));
            chunk.WriteChunk(Chunk.OpCode.OP_CONSTANT, 123);
            chunk.WriteChunk(constant, 123);

            chunk.WriteChunk(Chunk.OpCode.OP_DIVIDE, 123);
            chunk.WriteChunk(Chunk.OpCode.OP_NEGATE, 123);

            // Add a OP_RETURN
            chunk.WriteChunk(Chunk.OpCode.OP_RETURN, 123);

            Debug.DisassembleChunk(chunk, "test chunk");

            LoxVm vm = new LoxVm() { DebugTraceExecution = true };
            vm.Interpret(chunk);

            return 0;
        }
    }
}
