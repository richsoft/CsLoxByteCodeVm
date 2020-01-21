using System;
using CsLoxByteCodeVm.Debugging;
using CsLoxByteCodeVm.Values;
using CsLoxByteCodeVm.Code;
using CsLoxByteCodeVm.Vm;
using System.IO;
using CsLoxByteCodeVm.Compiler;

namespace CsLoxByteCodeVm
{
    class Program
    {
        private static LoxVm vm;

        static int Main(string[] args)
        {
            string script_name = "test.lox";
            string script_path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Scripts", script_name);
            
            using (vm = new LoxVm() { DebugTraceExecution = false })
            {
                if (string.IsNullOrEmpty(script_name)) {
                    Repl();
                }
                else
                {
                    RunFile(script_path);
                }
                
            }

            return 0;
        }

        /// <summary>
        /// REPL routine.  Reads from console
        /// </summary>
        private static void Repl()
        {
            string line;
            while (true)
            {
                Console.Write("> ");
                line = Console.ReadLine();

                // Quit if empty string
                if (line.Length == 0)
                {
                    Console.WriteLine();
                    break;
                }


                vm.Interpret(line);

            }

        }

        /// <summary>
        /// Read file and run the code
        /// </summary>
        /// <param name="path">The path</param>
        private static void RunFile(string path)
        {
            string source = ReadFile(path);
            LoxVm.InterpretResult result = vm.Interpret(source);

            if (result == LoxVm.InterpretResult.INTERPRET_COMPILE_ERROR) Environment.Exit(65);
            if (result == LoxVm.InterpretResult.INTERPRET_RUNTIME_ERROR) Environment.Exit(70);

        }

        /// <summary>
        /// Read a file into a string
        /// </summary>
        /// <param name="path">The file path</param>
        /// <returns>The file contents</returns>
        private static string ReadFile(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException)
            {
                Console.Error.WriteLine($"Could not read file {path}");
                Environment.Exit(74);
            }

            return null;
        }
    }
}
