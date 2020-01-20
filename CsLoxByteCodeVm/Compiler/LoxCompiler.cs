using CsLoxByteCodeVm.Code;
using CsLoxByteCodeVm.Debugging;
using CsLoxByteCodeVm.Values;
using CsLoxByteCodeVm.Vm;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Compiler
{
    class LoxCompiler
    {
        private readonly Parser _parser;
        private readonly Compiler _current;
        private readonly ParseRule[] _parse_rules;
        private readonly VmMemoryManager _mem_manager;

        private Scanner _scanner;
        private CodeChunk _compiling_chunk;


        public bool DebugPrintCode { get; set; }

        public LoxCompiler(VmMemoryManager mem_manager)
        {
            _mem_manager = mem_manager;
            _parser = new Parser();
            _current = new Compiler();

            // Parse rules
            _parse_rules = new[] {
                new ParseRule(Grouping, null, Precedence.PREC_NONE),       // TOKEN_LEFT_PAREN
                new ParseRule( null, null, Precedence.PREC_NONE ),       // TOKEN_RIGHT_PAREN     
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_LEFT_BRACE
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_RIGHT_BRACE     
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_COMMA           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_DOT             
                new ParseRule(Unary, Binary, Precedence.PREC_TERM ),       // TOKEN_MINUS           
                new ParseRule(null, Binary, Precedence.PREC_TERM ),       // TOKEN_PLUS            
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_SEMICOLON       
                new ParseRule(null, Binary, Precedence.PREC_FACTOR ),     // TOKEN_SLASH           
                new ParseRule(null, Binary, Precedence.PREC_FACTOR ),     // TOKEN_STAR            
                new ParseRule(Unary, null, Precedence.PREC_NONE ),       // TOKEN_BANG            
                new ParseRule(null, Binary, Precedence.PREC_EQUALITY ),       // TOKEN_BANG_EQUAL      
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_EQUAL           
                new ParseRule(null, Binary, Precedence.PREC_EQUALITY ),       // TOKEN_EQUAL_EQUAL     
                new ParseRule(null, Binary, Precedence.PREC_COMPARISON ),       // TOKEN_GREATER         
                new ParseRule(null, Binary, Precedence.PREC_COMPARISON ),       // TOKEN_GREATER_EQUAL   
                new ParseRule(null, Binary, Precedence.PREC_COMPARISON ),       // TOKEN_LESS            
                new ParseRule(null, Binary, Precedence.PREC_COMPARISON ),       // TOKEN_LESS_EQUAL      
                new ParseRule(Variable, null, Precedence.PREC_NONE ),       // TOKEN_IDENTIFIER      
                new ParseRule(String, null, Precedence.PREC_NONE ),       // TOKEN_STRING          
                new ParseRule(Number, null, Precedence.PREC_NONE ),       // TOKEN_NUMBER          
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_AND             
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_CLASS           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_ELSE            
                new ParseRule(Literal, null, Precedence.PREC_NONE ),       // TOKEN_FALSE           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_FOR             
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_FUN             
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_IF              
                new ParseRule(Literal, null, Precedence.PREC_NONE ),       // TOKEN_NIL             
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_OR              
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_PRINT           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_RETURN          
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_SUPER           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_THIS            
                new ParseRule(Literal, null, Precedence.PREC_NONE ),       // TOKEN_TRUE            
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_VAR             
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_WHILE           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_ERROR           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_EOF            
            };
        }

        /// <summary>
        /// Compile the source
        /// </summary>
        /// <param name="source"></param>
        public bool Compile(string source, ref CodeChunk chunk)
        {
            _scanner = new Scanner(source);
            _compiling_chunk = chunk;
            _parser.HadError = false;
            _parser.PanicMode = false;

            Advance();
            while (!Match(Scanner.TokenType.TOKEN_EOF))
            {
                Declaration();
            }

            EndCompiler();
            return !_parser.HadError;
        }

        /// <summary>
        /// Get the next token
        /// </summary>
        private void Advance()
        {
            _parser.Previous = _parser.Current;

            // Skip past errors
            while (true)
            {
                _parser.Current = _scanner.ScanToken();
                if (_parser.Current.Type != Scanner.TokenType.TOKEN_ERROR) break;

                ErrorAtCurrent(_parser.Current.Text);
            }
        }

        /// <summary>
        /// Read the next token, and check it of the expected type
        /// </summary>
        /// <param name="type">The expected type</param>
        /// <param name="message">Error message if not the correct type</param>
        private void Consume(Scanner.TokenType type, string message)
        {
            if (_parser.Current.Type == type)
            {
                Advance();
                return;
            }

            ErrorAtCurrent(message);
        }

        /// <summary>
        /// Check if the current token is of the given type and consume it
        /// </summary>
        /// <param name="type">The token type</param>
        /// <returns>True of the correct type</returns>
        private bool Match(Scanner.TokenType type)
        {
            if (!Check(type)) return false;
            Advance();
            return true;
        }

        /// <summary>
        /// Check if the current token is of a type
        /// </summary>
        /// <param name="type">The token type</param>
        /// <returns>True if the right type</returns>
        private bool Check(Scanner.TokenType type)
        {
            return _parser.Current.Type == type;
        }

        /// <summary>
        /// End compiling
        /// </summary>
        private void EndCompiler()
        {
            if (DebugPrintCode)
            {
                if (!_parser.HadError)
                {
                    Debug.DisassembleChunk(CurrentChunk(), "code");
                }
            }
            EmitReturn();
        }

        /// <summary>
        /// Parse a binary expression
        /// </summary>
        private void Binary(bool can_assign)
        {
            // Remeber the operator
            Scanner.TokenType operator_type = _parser.Previous.Type;

            // Compile the right operand
            ParseRule rule = GetRule(operator_type);
            ParsePrecedence(rule.Precedence + 1);

            // Emit the operator instruction
            switch (operator_type)
            {
                case Scanner.TokenType.TOKEN_BANG_EQUAL: EmitBytes(CodeChunk.OpCode.OP_EQUAL, CodeChunk.OpCode.OP_NOT); break;
                case Scanner.TokenType.TOKEN_EQUAL_EQUAL: EmitByte(CodeChunk.OpCode.OP_EQUAL); break;
                case Scanner.TokenType.TOKEN_GREATER: EmitByte(CodeChunk.OpCode.OP_GREATER); break;
                case Scanner.TokenType.TOKEN_GREATER_EQUAL: EmitBytes(CodeChunk.OpCode.OP_LESS, CodeChunk.OpCode.OP_NOT); break;
                case Scanner.TokenType.TOKEN_LESS: EmitByte(CodeChunk.OpCode.OP_LESS); break;
                case Scanner.TokenType.TOKEN_LESS_EQUAL: EmitBytes(CodeChunk.OpCode.OP_GREATER, CodeChunk.OpCode.OP_NOT); break;
                case Scanner.TokenType.TOKEN_PLUS: EmitByte(CodeChunk.OpCode.OP_ADD); break;
                case Scanner.TokenType.TOKEN_MINUS: EmitByte(CodeChunk.OpCode.OP_SUBTRACT); break;
                case Scanner.TokenType.TOKEN_STAR: EmitByte(CodeChunk.OpCode.OP_MULTIPLY); break;
                case Scanner.TokenType.TOKEN_SLASH: EmitByte(CodeChunk.OpCode.OP_DIVIDE); break;
                default:
                    return; // Unreachable.    
            }
        }

        /// <summary>
        /// Parse a literal
        /// </summary>
        private void Literal(bool can_assign)
        {
            switch (_parser.Previous.Type)
            {
                case Scanner.TokenType.TOKEN_FALSE: EmitByte(CodeChunk.OpCode.OP_FALSE); break;
                case Scanner.TokenType.TOKEN_NIL: EmitByte(CodeChunk.OpCode.OP_NIL); break;
                case Scanner.TokenType.TOKEN_TRUE: EmitByte(CodeChunk.OpCode.OP_TRUE); break;
                default:
                    return; //Unreachable
            }
        }

        /// <summary>
        /// Parse an expression
        /// </summary>
        private void Expression()
        {
            // Parse expression, using the lowest precedence level
            ParsePrecedence(Precedence.PREC_ASSIGNMENT);
        }

        /// <summary>
        /// Begin a new scope
        /// </summary>
        public void BeginScope()
        {
            _current.ScopeDepth++;
        }

        /// <summary>
        /// end the current scope
        /// </summary>
        public void EndScope()
        {
            _current.ScopeDepth--;

            // Loop back through the scope array
            while (_current.LocalCount > 0 &&
                _current.Locals[_current.LocalCount - 1].Depth > _current.ScopeDepth)
            {
                EmitByte(CodeChunk.OpCode.OP_POP);
                _current.LocalCount--;
            }
        }


        /// <summary>
        /// Compile a block
        /// </summary>
        private void Block()
        {
            // Parse declarations until the closing brace
            while (!Check(Scanner.TokenType.TOKEN_RIGHT_BRACE) && !Check(Scanner.TokenType.TOKEN_EOF))
            {
                Declaration();
            }

            Consume(Scanner.TokenType.TOKEN_RIGHT_BRACE, "Expect '}' after block.");

        }

        /// <summary>
        /// Parse a declaration
        /// </summary>
        private void Declaration()
        {
            if (Match(Scanner.TokenType.TOKEN_VAR))
            {
                VarDeclaration();
            }
            else
            {
                Statement();
            }

            if (_parser.PanicMode) Synchronize();
        }

        /// <summary>
        /// Compile a variable declaration
        /// </summary>
        private void VarDeclaration()
        {
            uint global = ParseVariable("Expect variable name");

            // If there is an initialiser then compile it
            if (Match(Scanner.TokenType.TOKEN_EQUAL))
            {
                Expression();
            }
            else
            {
                // If not initialiser, set to Nil
                EmitByte(CodeChunk.OpCode.OP_NIL);
            }
            Consume(Scanner.TokenType.TOKEN_SEMICOLON, "Expect ';' after variable declaration.");

            DefineVariable(global);

        }

        /// <summary>
        /// Parse a statement
        /// </summary>
        private void Statement()
        {
            if (Match(Scanner.TokenType.TOKEN_PRINT))
            {
                PrintStatement();
            }
            else if (Match(Scanner.TokenType.TOKEN_LEFT_BRACE))
            {
                BeginScope();
                Block();
                EndScope();
            }
            else
            {
                ExpressionStatement();
            }
        }

        /// <summary>
        /// Parse a expression statement
        /// </summary>
        private void ExpressionStatement()
        {
            Expression();
            Consume(Scanner.TokenType.TOKEN_SEMICOLON, "Expected ';' after expression.");
            EmitByte(CodeChunk.OpCode.OP_POP);
        }

        /// <summary>
        /// Compile a print statement
        /// </summary>
        private void PrintStatement()
        {
            Expression();
            Consume(Scanner.TokenType.TOKEN_SEMICOLON, "Expected ';' after value.");
            EmitByte(CodeChunk.OpCode.OP_PRINT);
        }

        /// <summary>
        /// Syncronise after an error
        /// </summary>
        private void Synchronize()
        {
            _parser.PanicMode = false;

            while (_parser.Current.Type != Scanner.TokenType.TOKEN_EOF)
            {
                if (_parser.Previous.Type == Scanner.TokenType.TOKEN_SEMICOLON) return;

                // Find a statement boundary
                switch (_parser.Current.Type)
                {
                    case Scanner.TokenType.TOKEN_CLASS:
                    case Scanner.TokenType.TOKEN_FUN:
                    case Scanner.TokenType.TOKEN_VAR:
                    case Scanner.TokenType.TOKEN_FOR:
                    case Scanner.TokenType.TOKEN_IF:
                    case Scanner.TokenType.TOKEN_WHILE:
                    case Scanner.TokenType.TOKEN_PRINT:
                    case Scanner.TokenType.TOKEN_RETURN:
                        return;

                    default:
                        // Do nothing
                        break;
                }

                Advance();
            }
        }

        /// <summary>
        /// Compile a number
        /// </summary>
        private void Number(bool can_assign)
        {
            double value = double.Parse(_parser.Previous.Text);
            EmitConstant(LoxValue.NumberValue(value));
        }

        /// <summary>
        /// Compile a string
        /// </summary>
        private void String(bool can_assign)
        {
            // Remove the quotes
            string text = _parser.Previous.Text[1..^1];

            // Create a string object, making sure it is stored by the memory manager
            LoxString s = _mem_manager.AllocateString(text);

            EmitConstant(LoxValue.StringObject(s));
        }

        /// <summary>
        /// Parse a variable
        /// </summary>
        private void Variable(bool can_assign)
        {
            NamedVariable(_parser.Previous, can_assign);
        }

        /// <summary>
        /// Compile a named variable
        /// </summary>
        /// <param name="token">The identifer token</param>
        /// <param name="can_assign">True if allowed to assign</param>
        private void NamedVariable(Scanner.Token name, bool can_assign)
        {
            CodeChunk.OpCode get_op;
            CodeChunk.OpCode set_op;

            int arg = ResolveLocal(name);
            if (arg != -1)
            {
                get_op = CodeChunk.OpCode.OP_GET_LOCAL;
                set_op = CodeChunk.OpCode.OP_SET_LOCAL;
            }
            else
            {
                arg = (int)IdentifierConstant(name);
                get_op = CodeChunk.OpCode.OP_GET_GLOBAL;
                set_op = CodeChunk.OpCode.OP_SET_GLOBAL;
            }

            if (can_assign && Match(Scanner.TokenType.TOKEN_EQUAL))
            {
                // Assignment
                Expression();
                EmitBytes(set_op, (byte)arg);
            }
            else
            {
                // Access
                EmitBytes(get_op, (byte)arg);
            }
        }

        /// <summary>
        /// Parse grouping brackets
        /// </summary>
        private void Grouping(bool can_assign)
        {
            // Compile the inner expression
            Expression();

            // Make sure there is a closing bracket
            Consume(Scanner.TokenType.TOKEN_RIGHT_PAREN, "Expect ')' after expression.");
        }

        /// <summary>
        /// Parse a unary expression
        /// </summary>
        private void Unary(bool can_assign)
        {
            Scanner.TokenType operator_type = _parser.Previous.Type;

            // Compile the operand
            ParsePrecedence(Precedence.PREC_UNARY);

            // Emit the operator instruction
            switch (operator_type)
            {
                case Scanner.TokenType.TOKEN_BANG: EmitByte(CodeChunk.OpCode.OP_NOT); break;
                case Scanner.TokenType.TOKEN_MINUS: EmitByte(CodeChunk.OpCode.OP_NEGATE); break;
                default:
                    // Shouldn't reach here!
                    return;
            }
        }

        /// <summary>
        /// Parse based on precendence (high or equal)
        /// </summary>
        /// <param name="precedence">The precedence level</param>
        private void ParsePrecedence(Precedence precedence)
        {
            Advance();

            // Prefix
            Action<bool> prefix_rule = GetRule(_parser.Previous.Type).Prefix;
            if (prefix_rule == null)
            {
                Error("Expect expression.");
                return;
            }
            bool can_assign = precedence <= Precedence.PREC_ASSIGNMENT;
            prefix_rule(can_assign);

            // Infix
            while (precedence < GetRule(_parser.Current.Type).Precedence)
            {
                Advance();
                Action<bool> infix_rule = GetRule(_parser.Previous.Type).Infix;
                infix_rule(can_assign);
            }

            if (can_assign && Match(Scanner.TokenType.TOKEN_EQUAL))
            {
                Error("Invalid assignment target.");
            }
        }

        /// <summary>
        /// Parse a variable name, returning its constant index
        /// </summary>
        /// <param name="error_message"></param>
        /// <returns>The constant index</returns>
        private uint ParseVariable(string error_message)
        {
            Consume(Scanner.TokenType.TOKEN_IDENTIFIER, error_message);

            DeclareVariable();

            // We only need to look up global varibales by name
            // If scope is deeper than 0, return.
            if (_current.InLocalScope) return 0;

            return IdentifierConstant(_parser.Previous);
        }

        /// <summary>
        /// Mark the current variable initialised
        /// </summary>
        private void MarkInitialised()
        {
            _current.Locals[_current.LocalCount - 1].Depth = _current.ScopeDepth;
        }

        /// <summary>
        /// Compile a global variable declaration
        /// </summary>
        /// <param name="global"></param>
        private void DefineVariable(uint global)
        {
            if (_current.InLocalScope)
            {
                MarkInitialised();
                return;
            }

            EmitBytes(CodeChunk.OpCode.OP_DEFINE_GLOBAL, (byte)global);
        }

        /// <summary>
        /// Create a constant from a identifer
        /// </summary>
        /// <param name="token">The token</param>
        /// <returns>The constant index</returns>
        private uint IdentifierConstant(Scanner.Token token)
        {
            return MakeConstant(LoxValue.StringObject(_mem_manager.AllocateString(token.Text)));
        }

        /// <summary>
        /// Check if identifer tokens are equal
        /// </summary>
        /// <param name="a">The first token</param>
        /// <param name="b">The second token</param>
        /// <returns>True ii tokens are both identifiers, and the name is the same</returns>
        private bool IdentifiersEqual(Scanner.Token a, Scanner.Token b)
        {
            return string.Equals(a.Text, b.Text);
        }

        /// <summary>
        /// Resolve a local variable
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <returns>The locals index</returns>
        private int ResolveLocal(Scanner.Token name)
        {
            for (int i = _current.LocalCount - 1; i >= 0; i--)
            {
                Compiler.Local local = _current.Locals[i];
                if (IdentifiersEqual(name, local.Name))
                {
                    if (!local.IsInitialised)
                    {
                        Error("Cannot read local variable in its own initialiser.");
                    }
                    return i;
                }
            }

            // Not found
            return -1;
        }

        /// <summary>
        /// Add a local variable
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if successful</returns>
        public void AddLocal(Scanner.Token name)
        {
            if (_current.LocalCount == Compiler.MAX_LOCALS)
            {
                Error("Too many local variables in function.");
                return;
            }

            Compiler.Local local = new Compiler.Local(name);

            _current.Locals[_current.LocalCount++] = local;

        }


        /// <summary>
        /// Declare a local variable
        /// </summary>
        private void DeclareVariable()
        {
            if (_current.InGlobalScope) return;

            Scanner.Token name = _parser.Previous;

            // Make sure we are not redeclaring again
            // Search form the end of the array
            for (int i = _current.LocalCount - 1; i >= 0; i--)
            {
                Compiler.Local local = _current.Locals[i];
                // Stop if this is not in our current scope
                if (local.Depth != -1 && local.Depth < _current.ScopeDepth)
                {
                    break;
                }

                if (IdentifiersEqual(name, local.Name))
                {
                    Error("Variable with this name already declared in this scope.");
                }
            }

            AddLocal(name);

        }

        /// <summary>
        /// Get the parsing rule for a token type
        /// </summary>
        /// <param name="type">The token type</param>
        /// <returns>The rule</returns>
        private ParseRule GetRule(Scanner.TokenType type)
        {
            return _parse_rules[(int)type];
        }

        /// <summary>
        /// Emit byte code
        /// </summary>
        /// <param name="b">The byte</param>
        private void EmitByte(byte b)
        {
            CurrentChunk().WriteChunk(b, _parser.Previous.Line);
        }

        /// <summary>
        /// Emit a opcode
        /// </summary>
        /// <param name="op_code">The op code</param>
        private void EmitByte(CodeChunk.OpCode op_code)
        {
            EmitByte((byte)op_code);
        }

        /// <summary>
        /// Emit byte code (2 bytes)
        /// </summary>
        /// <param name="b1">Byte 1</param>
        /// <param name="b2">Byte 2</param>
        private void EmitBytes(byte b1, byte b2)
        {
            EmitByte(b1);
            EmitByte(b2);
        }

        /// <summary>
        /// Emit opcode and operand (2 bytes)
        /// </summary>
        /// <param name="op_code">Opcode</param>
        /// <param name="b1">Byte 2</param>
        private void EmitBytes(CodeChunk.OpCode op_code, byte b1)
        {
            EmitByte(op_code);
            EmitByte(b1);
        }

        /// <summary>
        /// Emit 2 opcodes (2 bytes)
        /// </summary>
        /// <param name="op_code1">Opcode 1</param>
        /// <param name="op_code2">Opcode 2</param>
        private void EmitBytes(CodeChunk.OpCode op_code1, CodeChunk.OpCode op_code2)
        {
            EmitByte(op_code1);
            EmitByte(op_code2);
        }

        /// <summary>
        /// Emit an OP_RETURN
        /// </summary>
        private void EmitReturn()
        {
            EmitByte(CodeChunk.OpCode.OP_RETURN);
        }

        /// <summary>
        /// Emit a constant OP_CONSTANT+VALUE
        /// </summary>
        /// <param name="value"></param>
        private void EmitConstant(LoxValue value)
        {
            EmitBytes(CodeChunk.OpCode.OP_CONSTANT, MakeConstant(value));
        }

        private byte MakeConstant(LoxValue value)
        {
            // Add the value to the constant table
            int constant = CurrentChunk().AddConstant(value);
            if (constant > byte.MaxValue)
            {
                Error("Too many constants in one chunk.");
                return 0;
            }

            return (byte)constant;
        }


        /// <summary>
        /// Report an error at the current token
        /// </summary>
        /// <param name="message">The error message</param>
        private void ErrorAtCurrent(string message)
        {
            ErrorAt(_parser.Current, message);
        }

        /// <summary>
        /// Report an error at the previous token
        /// </summary>
        /// <param name="message">The error message</param>
        private void Error(string message)
        {
            ErrorAt(_parser.Previous, message);
        }

        /// <summary>
        /// Report an error at a token
        /// </summary>
        /// <param name="token">The token</param>
        /// <param name="message">The error message</param>
        private void ErrorAt(Scanner.Token token, string message)
        {
            // If already panicing, then don't show the error
            if (_parser.PanicMode) return;

            // PANIC!!!
            _parser.PanicMode = true;

            Console.Error.Write($"[line {token.Line}] Error");

            if (token.Type == Scanner.TokenType.TOKEN_EOF)
            {
                Console.Error.Write(" at the end.");
            }
            else if (token.Type == Scanner.TokenType.TOKEN_ERROR)
            {
                // Nothing
            }
            else
            {
                Console.Error.Write($" at {token.Text}");
            }

            Console.Error.WriteLine($": {message}");
            _parser.HadError = true;
        }

        /// <summary>
        /// Get the current code chunk
        /// </summary>
        /// <returns>The current chunk</returns>
        private CodeChunk CurrentChunk()
        {
            return _compiling_chunk;
        }

        private class Parser
        {
            public Scanner.Token Current { get; set; }
            public Scanner.Token Previous { get; set; }
            public bool HadError { get; set; }
            public bool PanicMode { get; set; }
        }

        private class ParseRule
        {
            public Action<bool> Prefix { get; set; }
            public Action<bool> Infix { get; set; }
            public Precedence Precedence { get; set; }

            public ParseRule() { }

            public ParseRule(Action<bool> prefix, Action<bool> infix, Precedence precendence)
            {
                Prefix = prefix;
                Infix = infix;
                Precedence = precendence;
            }
        }

        private class Compiler
        {
            public Local[] Locals { get; set; }
            public int LocalCount { get; set; }
            public int ScopeDepth { get; set; }

            public bool InLocalScope => ScopeDepth > 0;
            public bool InGlobalScope => ScopeDepth == 0;

            public const int MAX_LOCALS = (byte.MaxValue + 1);

            public Compiler()
            {
                Locals = new Local[MAX_LOCALS];
                LocalCount = 0;
                ScopeDepth = 0;
            }

            public class Local
            {
                public Scanner.Token Name { get; set; }
                public int Depth { get; set; }

                public bool IsInitialised => Depth > -1;

                public Local(Scanner.Token name)
                {
                    Name = name;
                }
            }
        }

        private enum Precedence
        {
            PREC_NONE,
            PREC_ASSIGNMENT,  // =        
            PREC_OR,          // or       
            PREC_AND,         // and      
            PREC_EQUALITY,    // == !=    
            PREC_COMPARISON,  // < > <= >=
            PREC_TERM,        // + -      
            PREC_FACTOR,      // * /      
            PREC_UNARY,       // ! -      
            PREC_CALL,        // . ()     
            PREC_PRIMARY
        }
    }
}
