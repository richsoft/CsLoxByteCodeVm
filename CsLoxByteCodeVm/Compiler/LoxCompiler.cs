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
        private readonly ParseRule[] _parse_rules;
        private readonly VmMemoryManager _mem_manager;

        private Scanner _scanner;
        private Compiler _current;
        //private CodeChunk _compiling_chunk;


        public bool DebugPrintCode { get; set; }

        public LoxCompiler(VmMemoryManager mem_manager)
        {
            _mem_manager = mem_manager;
            _parser = new Parser();
            _current = null;

            // Parse rules
            _parse_rules = new[] {
                new ParseRule(Grouping, Call, Precedence.PREC_CALL),       // TOKEN_LEFT_PAREN
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
                new ParseRule(null, And, Precedence.PREC_AND ),       // TOKEN_AND             
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_CLASS           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_ELSE            
                new ParseRule(Literal, null, Precedence.PREC_NONE ),       // TOKEN_FALSE           
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_FOR             
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_FUN             
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_IF              
                new ParseRule(Literal, null, Precedence.PREC_NONE ),       // TOKEN_NIL             
                new ParseRule(null, Or, Precedence.PREC_OR ),       // TOKEN_OR              
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
        public LoxFunction Compile(string source)
        {
            _scanner = new Scanner(source);
            //_compiling_chunk = chunk;
            _parser.HadError = false;
            _parser.PanicMode = false;

            InitCompiler(Compiler.FunctionType.TYPE_SCRIPT);

            Advance();
            while (!Match(Scanner.TokenType.TOKEN_EOF))
            {
                Declaration();
            }

            LoxFunction function = EndCompiler();
            return _parser.HadError ? null : function;
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

        private void InitCompiler(Compiler.FunctionType type)
        {
            LoxString name = null;

            if (type != Compiler.FunctionType.TYPE_SCRIPT)
            {
                name = _mem_manager.AllocateString(_parser.Previous.Text);
            }

            _current = new Compiler(type, name, _current);
        }

        /// <summary>
        /// End compiling
        /// </summary>
        private LoxFunction EndCompiler()
        {
            EmitReturn();
            LoxFunction function = _current.Function;

            if (DebugPrintCode)
            {
                if (!_parser.HadError)
                {
                    Debug.DisassembleChunk(CurrentChunk(), function?.Name?.Value ?? "<script>");
                }
            }

            // Set the current compiler back to the enclosing one
            _current = _current.Enclosing;
            return function;
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
        /// Parse a function call
        /// </summary>
        /// <param name="can_assign"></param>
        private void Call(bool can_assign)
        {
            byte arg_count = ArgumentList();
            EmitByte(CodeChunk.OpCode.OP_CALL, arg_count)
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
        /// Compile function
        /// </summary>
        /// <param name="type">The function type</param>
        private void Function(Compiler.FunctionType type)
        {
            InitCompiler(type);

            BeginScope();

            // Compile the parameter list
            Consume(Scanner.TokenType.TOKEN_LEFT_PAREN, "Expect '(' after function name.");
            if (!Check(Scanner.TokenType.TOKEN_RIGHT_PAREN))
            {
                do
                {
                    _current.Function.Arity++;
                    if (_current.Function.Arity > 255)
                    {
                        ErrorAtCurrent("Cannot have more then 255 parameters");
                    }

                    byte param_constant = ParseVariable("Expect parameter name.");
                    DefineVariable(param_constant);
                }
                while (Match(Scanner.TokenType.TOKEN_COMMA));
            }
            Consume(Scanner.TokenType.TOKEN_RIGHT_PAREN, "Expect ')' after parameters.");

            // The body
            Consume(Scanner.TokenType.TOKEN_LEFT_BRACE, "Expect '{' before function body.");
            Block();

            // Create the function object
            LoxFunction function = EndCompiler();
            EmitBytes(CodeChunk.OpCode.OP_CONSTANT, MakeConstant(LoxValue.FunctionObject(function)));



        }

        /// <summary>
        /// Parse a declaration
        /// </summary>
        private void Declaration()
        {
            if (Match(Scanner.TokenType.TOKEN_FUN))
            {
                FunDeclaration();
            }
            else if (Match(Scanner.TokenType.TOKEN_VAR))
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
        /// Compile function declaration
        /// </summary>
        public void FunDeclaration()
        {
            uint global = ParseVariable("Expect function name.");
            MarkInitialised();
            Function(Compiler.FunctionType.TYPE_FUNCTION);
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
            else if (Match(Scanner.TokenType.TOKEN_FOR))
            {
                ForStatement();
            }
            else if (Match(Scanner.TokenType.TOKEN_IF))
            {
                IfStatement();
            }
            else if (Match(Scanner.TokenType.TOKEN_WHILE))
            {
                WhileStatement();
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
        /// Parse an if statement
        /// </summary>
        private void IfStatement()
        {
            Consume(Scanner.TokenType.TOKEN_LEFT_PAREN, "Expect '(' after 'if'.");
            // Compile the condition expression
            Expression();
            Consume(Scanner.TokenType.TOKEN_RIGHT_PAREN, "Expect ')' after condition.");

            // Jump over the true
            int then_jump = EmitJump(CodeChunk.OpCode.OP_JUMP_IF_FALSE);
            EmitByte(CodeChunk.OpCode.OP_POP);
            Statement();

            // Jump over the else
            int else_jump = EmitJump(CodeChunk.OpCode.OP_JUMP);

            // Backpatch the jump for false
            PatchJump(then_jump);
            EmitByte(CodeChunk.OpCode.OP_POP);

            if (Match(Scanner.TokenType.TOKEN_ELSE)) Statement();
            // Back patch so true can jump over else
            PatchJump(else_jump);
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
        /// Compile a for statement
        /// </summary>
        private void ForStatement()
        {
            BeginScope();
            Consume(Scanner.TokenType.TOKEN_LEFT_PAREN, "Expect '(' after 'for'.");
            
            // Initaliser
            if (Match(Scanner.TokenType.TOKEN_SEMICOLON))
            {
                // No initialiser
            }
            else if (Match(Scanner.TokenType.TOKEN_VAR))
            {
                VarDeclaration();
            }
            else
            {
                ExpressionStatement();
            }

            int loop_start = CurrentChunk().Code.Count;

            // Condition expression
            int exit_jump = -1;
            if (!Match(Scanner.TokenType.TOKEN_SEMICOLON))
            {
                // There is a condition
                Expression();
                Consume(Scanner.TokenType.TOKEN_SEMICOLON, "Expect ';' after loop condition.");

                // Jump out of loop if the condition is false
                exit_jump = EmitJump(CodeChunk.OpCode.OP_JUMP_IF_FALSE);
                EmitByte(CodeChunk.OpCode.OP_POP); // Condition
            }

            // Increment
            // Compiled first, and then jumped over
            // Each loop jumps back to the incrementer and then back into the body
            if (!Match(Scanner.TokenType.TOKEN_RIGHT_PAREN)) {
                int body_jump = EmitJump(CodeChunk.OpCode.OP_JUMP);

                int increment_start = CurrentChunk().Code.Count;
                Expression();
                EmitByte(CodeChunk.OpCode.OP_POP);
                Consume(Scanner.TokenType.TOKEN_RIGHT_PAREN, "Expect ')' after for clauses.");

                EmitLoop(loop_start);
                loop_start = increment_start;
                PatchJump(body_jump);
            }

            Statement();

            EmitLoop(loop_start);

            if (exit_jump != -1)
            {
                // There was condition so patch the jump out of the loop
                PatchJump(exit_jump);
                EmitByte(CodeChunk.OpCode.OP_POP);  // Condition;
            }

            EndScope();

        }

        /// <summary>
        /// Compile a while statement
        /// </summary>
        private void WhileStatement()
        {
            int loop_start = CurrentChunk().Code.Count;

            Consume(Scanner.TokenType.TOKEN_LEFT_PAREN, "Expect '(' after 'while.");
            Expression();
            Consume(Scanner.TokenType.TOKEN_RIGHT_BRACE, "Expect ')' after 'condition.");

            int exit_jump = EmitJump(CodeChunk.OpCode.OP_JUMP_IF_FALSE);

            EmitByte(CodeChunk.OpCode.OP_POP);
            Statement();

            EmitLoop(loop_start);

            PatchJump(exit_jump);
            EmitByte(CodeChunk.OpCode.OP_POP);
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
        /// Compile an AND
        /// </summary>
        /// <param name="can_assign"></param>
        private void And(bool can_assign)
        {
            int end_jump = EmitJump(CodeChunk.OpCode.OP_JUMP_IF_FALSE);

            EmitByte(CodeChunk.OpCode.OP_POP);
            ParsePrecedence(Precedence.PREC_AND);

            PatchJump(end_jump);
        }

        /// <summary>
        /// Compile an OR
        /// </summary>
        /// <param name="can_assign"></param>
        private void Or(bool can_assign)
        {
            int else_jump = EmitJump(CodeChunk.OpCode.OP_JUMP_IF_FALSE);
            int end_jump = EmitJump(CodeChunk.OpCode.OP_JUMP);

            PatchJump(else_jump);
            EmitByte(CodeChunk.OpCode.OP_POP);

            ParsePrecedence(Precedence.PREC_OR);
            PatchJump(end_jump);
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
        private byte ParseVariable(string error_message)
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
            if (_current.ScopeDepth == 0) return;
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
        /// Parse a argument list
        /// </summary>
        /// <returns></returns>
        private byte ArgumentList()
        {
            byte arg_count = 0;
            if (!Check(Scanner.TokenType.TOKEN_RIGHT_PAREN))
            {
                do
                {
                    Expression();

                    if (argCount == 255)
                    {
                        error("Cannot have more than 255 arguments.");
                    }

                    arg_count++;
                }
                while (Match(Scanner.TokenType.TOKEN_COMMA));
            }

            Consume(Scanner.TokenType.TOKEN_RIGHT_PAREN, "Expect ')' after arguments.");

            return arg_count;
        }

        /// <summary>
        /// Create a constant from a identifer
        /// </summary>
        /// <param name="token">The token</param>
        /// <returns>The constant index</returns>
        private byte IdentifierConstant(Scanner.Token token)
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
            return string.Equals(a?.Text, b?.Text);
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
        /// Emit a jump instruction, using placeholder jump operand
        /// </summary>
        /// <param name="instruction">The jump instruction to emit</param>
        /// <returns>The offset of the jump</returns>
        private int EmitJump(CodeChunk.OpCode instruction)
        {
            EmitByte(instruction);
            EmitByte((byte)0xff);
            EmitByte((byte)0xff);
            return CurrentChunk().Code.Count - 2;
        }

        /// <summary>
        /// Emit a loop instruction
        /// </summary>
        /// <param name="loop_start"></param>
        private void EmitLoop(int loop_start)
        {
            EmitByte(CodeChunk.OpCode.OP_LOOP);

            int offset = CurrentChunk().Code.Count - loop_start + 2;
            if (offset > ushort.MaxValue) Error("Loop body too large.");

            EmitByte((byte)((offset >> 8) & 0xff));
            EmitByte((byte)(offset & 0xff));
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

        /// <summary>
        /// Backpatch a jump instruction
        /// </summary>
        /// <param name="offset">The offset of the jump</param>
        private void PatchJump(int offset)
        {
            // -2 to adjust for the bytecode for the jump offset
            int jump = CurrentChunk().Code.Count - offset - 2;

            if (jump > ushort.MaxValue)
            {
                Error("Too much code to jump over.");
            }

            // Get high and low bytes
            CurrentChunk().Code[offset] = (byte)((jump >> 8) & 0xff);
            CurrentChunk().Code[offset + 1] = (byte)(jump & 0xff);

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
            return _current.Function.Chunk;
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
            public Compiler Enclosing;
            public LoxFunction Function { get; set; }
            public FunctionType Type { get; set; }

            public Local[] Locals { get; set; }
            public int LocalCount { get; set; }
            public int ScopeDepth { get; set; }

            public bool InLocalScope => ScopeDepth > 0;
            public bool InGlobalScope => ScopeDepth == 0;

            public const int MAX_LOCALS = (byte.MaxValue + 1);

            public Compiler(FunctionType type, LoxString name = null, Compiler enclosing = null)
            {
                Enclosing = enclosing;
                Locals = new Local[MAX_LOCALS];
                LocalCount = 0;
                ScopeDepth = 0;
                Function = new LoxFunction();
                Type = type;

                if (type != FunctionType.TYPE_SCRIPT)
                {
                    Function.Name = name;
                }

                Locals[LocalCount++] = new Local(null)
                {
                    Depth = 0
                };
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

            public enum FunctionType
            {
                TYPE_FUNCTION,
                TYPE_SCRIPT
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
