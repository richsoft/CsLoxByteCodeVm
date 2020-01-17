using CsLoxByteCodeVm.Code;
using CsLoxByteCodeVm.Debugging;
using CsLoxByteCodeVm.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Compiler
{
    class LoxCompiler
    {
        private Parser _parser;
        private Scanner _scanner;
        private CodeChunk _compiling_chunk;
        private readonly ParseRule[] _parse_rules;

        public bool DebugPrintCode {get; set;}

        public LoxCompiler()
        {
            _parser = new Parser();

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
                new ParseRule(null, null, Precedence.PREC_NONE ),       // TOKEN_IDENTIFIER      
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
            Expression();
            Consume(Scanner.TokenType.TOKEN_EOF, "Expected end of expression.");

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
        private void Binary()
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
        private void Literal()
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
        /// Parse a number
        /// </summary>
        private void Number()
        {
            double value = double.Parse(_parser.Previous.Text);
            EmitConstant(VmValue.NumberValue(value));
        }

        /// <summary>
        /// Parse a string
        /// </summary>
        private void String()
        {
            // Remove the quotes
            string s = _parser.Previous.Text.Substring(1, _parser.Previous.Text.Length - 2);
            EmitConstant(VmValue.StringObject(s));
        }

        /// <summary>
        /// Parse grouping brackets
        /// </summary>
        private void Grouping()
        {
            // Compile the inner expression
            Expression();

            // Make sure there is a closing bracket
            Consume(Scanner.TokenType.TOKEN_RIGHT_PAREN, "Expect ')' after expression.");
        }

        /// <summary>
        /// Parse a unary expression
        /// </summary>
        private void Unary()
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
            Action prefix_rule = GetRule(_parser.Previous.Type).Prefix;
            if (prefix_rule == null)
            {
                Error("Expect expression.");
                return;
            }
            prefix_rule();

            // Infix
            while (precedence < GetRule(_parser.Current.Type).Precedence)
            {
                Advance();
                Action infix_rule = GetRule(_parser.Previous.Type).Infix;
                infix_rule();
            }
        }

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
        private void EmitConstant(VmValue value)
        {
            EmitBytes(CodeChunk.OpCode.OP_CONSTANT, MakeConstant(value));
        }

        private byte MakeConstant(VmValue value)
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
            public Action Prefix { get; set; }
            public Action Infix { get; set; }
            public Precedence Precedence { get; set; }

            public ParseRule() { }

            public ParseRule(Action prefix, Action infix, Precedence precendence)
            {
                Prefix = prefix;
                Infix = infix;
                Precedence = precendence;
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
