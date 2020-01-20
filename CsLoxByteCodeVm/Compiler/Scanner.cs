using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Compiler
{
    class Scanner
    {
        private int _start;
        private int _current;
        private int _line;
        private string _source;

        public char CurrentChar => _source[_current];
        public string CurrentString => _source.Substring(_start, _current - _start);

        public Scanner(string source)
        {
            _source = source;
            _start = 0;
            _current = 0;
            _line = 1;
        }

        /// <summary>
        /// Return the next token
        /// </summary>
        /// <returns>The token</returns>
        public Token ScanToken()
        {
            SkipWhiteSpace();

            _start = _current;

            // Id at the end return a EOF token
            if (IsAtEnd())
            {
                return MakeToken(TokenType.TOKEN_EOF);
            }

            char c = Advance();
            if (IsAlpha(c)) return Identifier();
            if (IsDigit(c)) return Number();

            switch (c)
            {
                case '(': return MakeToken(TokenType.TOKEN_LEFT_PAREN);
                case ')': return MakeToken(TokenType.TOKEN_RIGHT_PAREN);
                case '{': return MakeToken(TokenType.TOKEN_LEFT_BRACE);
                case '}': return MakeToken(TokenType.TOKEN_RIGHT_BRACE);
                case ';': return MakeToken(TokenType.TOKEN_SEMICOLON);
                case ',': return MakeToken(TokenType.TOKEN_COMMA);
                case '.': return MakeToken(TokenType.TOKEN_DOT);
                case '-': return MakeToken(TokenType.TOKEN_MINUS);
                case '+': return MakeToken(TokenType.TOKEN_PLUS);
                case '/': return MakeToken(TokenType.TOKEN_SLASH);
                case '*': return MakeToken(TokenType.TOKEN_STAR);
                case '!':
                    return MakeToken(Match('=') ? TokenType.TOKEN_BANG_EQUAL : TokenType.TOKEN_BANG);
                case '=':
                    return MakeToken(Match('=') ? TokenType.TOKEN_EQUAL_EQUAL : TokenType.TOKEN_EQUAL);
                case '<':
                    return MakeToken(Match('=') ? TokenType.TOKEN_LESS_EQUAL : TokenType.TOKEN_LESS);
                case '>':
                    return MakeToken(Match('=') ? TokenType.TOKEN_GREATER_EQUAL : TokenType.TOKEN_GREATER);
                case '"':
                    return String();
                default:
                    break;
            }

            // Pass the errors to the compiler as ERROR tokens
            return ErrorToken("Unexpected character.");
        }

        /// <summary>
        /// Consume and return the next character
        /// </summary>
        /// <returns>The character</returns>
        private char Advance()
        {
            _current++;
            return _source[_current - 1];
        }

        /// <summary>
        /// Peek at the current character, without consuming it
        /// </summary>
        /// <returns>The character</returns>
        private char Peek()
        {
            if (_current >= _source.Length) return '\0';
            return _source[_current];
        }

        /// <summary>
        /// Peek at the next character, without consuming it
        /// </summary>
        /// <returns>The character</returns>
        private char PeekNext()
        {
            if ((_current + 1) >= _source.Length) return '\0';
            return _source[_current + 1];
        }

        /// <summary>
        /// Consume next charcter if it matches an expected character
        /// </summary>
        /// <param name="expected">The expected character</param>
        /// <returns>True if matched</returns>
        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_current] != expected) return false;

            _current++;
            return true;
        }

        /// <summary>
        /// Skip any whitespace
        /// </summary>
        private void SkipWhiteSpace()
        {
            while (true)
            {
                char c = Peek();
                switch (c)
                {
                    case ' ':
                    case '\r':
                    case '\t':
                        // Whitespace
                        Advance();
                        break;
                    case '\n':
                        // Newlines
                        _line++;
                        Advance();
                        break;
                    case '/':
                        // Comments
                        if (PeekNext() == '/')
                        {
                            // A comment goes to the end of the line
                            while (Peek() != '\n' && !IsAtEnd())
                            {
                                Advance();
                            }
                        }
                        else
                        {
                            return;
                        }
                        break;

                    default:
                        return;
                }
            }
        }

        /// <summary>
        /// Read and create a string token
        /// </summary>
        /// <returns>The string token</returns>
        private Token String()
        {
            while (Peek() != '"' && !IsAtEnd())
            {
                // Check for line breaks
                if (Peek() == '\n') _line++;

                Advance();
            }

            // If we are at the end, the closing quotes are missing
            if (IsAtEnd())
            {
                return ErrorToken("Unterminated string.");
            }

            // The closing quote
            Advance();

            return MakeToken(TokenType.TOKEN_STRING);
        }

        /// <summary>
        /// Read a create a number token
        /// </summary>
        /// <returns>The number token</returns>
        private Token Number()
        {
            while (IsDigit(Peek())) Advance();

            // Look for the decimal
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                // Consume the ','
                Advance();

                // Read the digits after the decimal
                while (IsDigit(Peek())) Advance();
            }

            return MakeToken(TokenType.TOKEN_NUMBER);
        }

        /// <summary>
        /// Read a create an identifier token
        /// </summary>
        /// <returns>The token</returns>
        private Token Identifier()
        {
            while (IsAlpha(Peek()) || IsDigit(Peek())) Advance();

            return MakeToken(IdentifierType());
        }

        /// <summary>
        /// Get the current idenifier type (using a trie)
        /// </summary>
        /// <returns>The token type</returns>
        private TokenType IdentifierType()
        {

            switch (_source[_start])
            {
                case 'a': return CheckKeyword(1, 2, "nd", TokenType.TOKEN_AND);
                case 'c': return CheckKeyword(1, 4, "lass", TokenType.TOKEN_CLASS);
                case 'e': return CheckKeyword(1, 3, "lse", TokenType.TOKEN_ELSE);
                case 'f':
                    if (_current - _start > 0)
                    {
                        switch (_source[_start + 1])
                        {
                            case 'a': return CheckKeyword(2, 3, "lse", TokenType.TOKEN_FALSE);
                            case 'o': return CheckKeyword(2, 1, "r", TokenType.TOKEN_FOR);
                            case 'u': return CheckKeyword(2, 1, "n", TokenType.TOKEN_FUN);
                        }
                    }
                    break;
                case 'i': return CheckKeyword(1, 1, "f", TokenType.TOKEN_IF);
                case 'n': return CheckKeyword(1, 2, "il", TokenType.TOKEN_NIL);
                case 'o': return CheckKeyword(1, 1, "r", TokenType.TOKEN_OR);
                case 'p': return CheckKeyword(1, 4, "rint", TokenType.TOKEN_PRINT);
                case 'r': return CheckKeyword(1, 5, "eturn", TokenType.TOKEN_RETURN);
                case 's': return CheckKeyword(1, 4, "uper", TokenType.TOKEN_SUPER);
                case 't':
                    if (_current - _start > 0)
                    {
                        switch (_source[_start + 1])
                        {
                            case 'h': return CheckKeyword(2, 2, "is", TokenType.TOKEN_THIS);
                            case 'r': return CheckKeyword(2, 2, "ue", TokenType.TOKEN_TRUE);
                        }
                    }
                    break;
                case 'v': return CheckKeyword(1, 2, "ar", TokenType.TOKEN_VAR);
                case 'w': return CheckKeyword(1, 4, "hile", TokenType.TOKEN_WHILE);
            }


            return TokenType.TOKEN_IDENTIFIER;
        }

        /// <summary>
        /// Check if keyword matches, and return the token type
        /// </summary>
        /// <param name="start">The start</param>
        /// <param name="length">The length</param>
        /// <param name="rest">The remainer of the keyword</param>
        /// <param name="type">The token type if matched</param>
        /// <returns>The token type</returns>
        private TokenType CheckKeyword(int start, int length, string rest, TokenType type)
        {
            if (_current - _start == start + length &&
                    string.Equals(_source.Substring(_start + start, length), rest))
            {

                return type;
            }

            return TokenType.TOKEN_IDENTIFIER;
        }

        /// <summary>
        /// Check if a the end of the source
        /// </summary>
        /// <returns>True if at the end</returns>
        private bool IsAtEnd()
        {
            return _current >= _source.Length;
        }

        /// <summary>
        /// Check if a character is a digit
        /// </summary>
        /// <param name="c">The character</param>
        /// <returns>True if a digit</returns>
        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Check if a character is a letter or underscore
        /// </summary>
        /// <param name="c">The charcacter</param>
        /// <returns>True if character is a letter</returns>
        private bool IsAlpha(char c)
        {
            return
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';
        }

        /// <summary>
        /// Create a token of the given type
        /// </summary>
        /// <param name="type">The token type</param>
        /// <returns>The new token</returns>
        private Token MakeToken(TokenType type)
        {
            Token tkn = new Token()
            {
                Type = type,
                Text = _source.Substring(_start, (_current - _start)),
                Line = _line
            };

            return tkn;
        }

        /// <summary>
        /// Create an error token with a message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns>The token</returns>
        private Token ErrorToken(string message)
        {
            return new Token()
            {
                Type = TokenType.TOKEN_ERROR,
                Text = message,
                Line = _line
            };
        }

        public class Token
        {
            public TokenType Type { get; set; }
            // In clox, the text is stored as a pointer into the source and length
            public string Text { get; set; }
            public int Line { get; set; }

        }

        public enum TokenType
        {
            // Single-character tokens.                         
            TOKEN_LEFT_PAREN, TOKEN_RIGHT_PAREN,
            TOKEN_LEFT_BRACE, TOKEN_RIGHT_BRACE,
            TOKEN_COMMA, TOKEN_DOT, TOKEN_MINUS, TOKEN_PLUS,
            TOKEN_SEMICOLON, TOKEN_SLASH, TOKEN_STAR,

            // One or two character tokens.                     
            TOKEN_BANG, TOKEN_BANG_EQUAL,
            TOKEN_EQUAL, TOKEN_EQUAL_EQUAL,
            TOKEN_GREATER, TOKEN_GREATER_EQUAL,
            TOKEN_LESS, TOKEN_LESS_EQUAL,

            // Literals.                                        
            TOKEN_IDENTIFIER, TOKEN_STRING, TOKEN_NUMBER,

            // Keywords.                                        
            TOKEN_AND, TOKEN_CLASS, TOKEN_ELSE, TOKEN_FALSE,
            TOKEN_FOR, TOKEN_FUN, TOKEN_IF, TOKEN_NIL, TOKEN_OR,
            TOKEN_PRINT, TOKEN_RETURN, TOKEN_SUPER, TOKEN_THIS,
            TOKEN_TRUE, TOKEN_VAR, TOKEN_WHILE,

            TOKEN_ERROR,
            TOKEN_EOF
        }
    }
}
