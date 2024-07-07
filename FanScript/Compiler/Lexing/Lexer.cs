using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Lexing
{
    public sealed class Lexer
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private int _position;

        private int _start;
        private SyntaxKind _kind;
        private object? _value;
        private ImmutableArray<SyntaxTrivia>.Builder _triviaBuilder = ImmutableArray.CreateBuilder<SyntaxTrivia>();

        public Lexer(SyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
            _text = syntaxTree.Text;
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private char Current => Peek(0);

        private char Lookahead => Peek(1);

        private char Peek(int offset)
        {
            var index = _position + offset;

            if (index >= _text.Length)
                return '\0';

            return _text[index];
        }

        public SyntaxToken Lex()
        {
            ReadTrivia(leading: true);

            ImmutableArray<SyntaxTrivia> leadingTrivia = _triviaBuilder.ToImmutable();
            int tokenStart = _position;

            ReadToken();

            SyntaxKind tokenKind = _kind;
            object? tokenValue = _value;
            int tokenLength = _position - _start;

            ReadTrivia(leading: false);

            ImmutableArray<SyntaxTrivia> trailingTrivia = _triviaBuilder.ToImmutable();

            string? tokenText = SyntaxFacts.GetText(tokenKind);
            if (tokenText is null)
                tokenText = _text.ToString(tokenStart, tokenLength);

            return new SyntaxToken(_syntaxTree, tokenKind, tokenStart, tokenText, tokenValue, leadingTrivia, trailingTrivia);
        }

        private void ReadTrivia(bool leading)
        {
            _triviaBuilder.Clear();

            bool done = false;

            while (!done)
            {
                _start = _position;
                _kind = SyntaxKind.BadToken;
                _value = null;

                switch (Current)
                {
                    case '\0':
                        done = true;
                        break;
                    case '/':
                        if (Lookahead == '/')
                        {
                            ReadSingleLineComment();
                        }
                        else if (Lookahead == '*')
                        {
                            ReadMultiLineComment();
                        }
                        else
                        {
                            done = true;
                        }
                        break;
                    case '\n':
                    case '\r':
                        if (!leading)
                            done = true;
                        ReadLineBreak();
                        break;
                    case ' ':
                    case '\t':
                        ReadWhiteSpace();
                        break;
                    default:
                        if (char.IsWhiteSpace(Current))
                            ReadWhiteSpace();
                        else
                            done = true;
                        break;
                }

                int length = _position - _start;
                if (length > 0)
                {
                    string text = _text.ToString(_start, length);
                    SyntaxTrivia trivia = new SyntaxTrivia(_syntaxTree, _kind, _start, text);
                    _triviaBuilder.Add(trivia);
                }
            }
        }

        private void ReadLineBreak()
        {
            if (Current == '\r' && Lookahead == '\n')
            {
                _position += 2;
            }
            else
            {
                _position++;
            }

            _kind = SyntaxKind.LineBreakTrivia;
        }

        private void ReadWhiteSpace()
        {
            bool done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        done = true;
                        break;
                    default:
                        if (!char.IsWhiteSpace(Current))
                            done = true;
                        else
                            _position++;
                        break;
                }
            }

            _kind = SyntaxKind.WhitespaceTrivia;
        }


        private void ReadSingleLineComment()
        {
            _position += 2;
            bool done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        done = true;
                        break;
                    default:
                        _position++;
                        break;
                }
            }

            _kind = SyntaxKind.SingleLineCommentTrivia;
        }

        private void ReadMultiLineComment()
        {
            _position += 2;
            bool done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                        TextLocation location = new TextLocation(_text, new TextSpan(_start, 2));
                        _diagnostics.ReportUnterminatedMultiLineComment(location);
                        done = true;
                        break;
                    case '*':
                        if (Lookahead == '/')
                        {
                            _position++;
                            done = true;
                        }
                        _position++;
                        break;
                    default:
                        _position++;
                        break;
                }
            }

            _kind = SyntaxKind.MultiLineCommentTrivia;
        }

        private void ReadToken()
        {
            _start = _position;
            _kind = SyntaxKind.BadToken;
            _value = null;

            switch (Current)
            {
                case '\0':
                    _kind = SyntaxKind.EndOfFileToken;
                    break;
                case '+':
                    _position++;
                    if (Current == '+')
                    {
                        _kind = SyntaxKind.PlusPlusToken;
                        _position++;
                    }
                    else if (Current == '=')
                    {
                        _kind = SyntaxKind.PlusEqualsToken;
                        _position++;
                    }
                    else
                        _kind = SyntaxKind.PlusToken;
                    break;
                case '-':
                    _position++;
                    if (Current != '=')
                    {
                        _kind = SyntaxKind.MinusToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.MinusEqualsToken;
                        _position++;
                    }
                    break;
                case '*':
                    _position++;
                    if (Current != '=')
                    {
                        _kind = SyntaxKind.StarToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.StarEqualsToken;
                        _position++;
                    }
                    break;
                case '/':
                    _position++;
                    if (Current != '=')
                    {
                        _kind = SyntaxKind.SlashToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.SlashEqualsToken;
                        _position++;
                    }
                    break;
                case '(':
                    _kind = SyntaxKind.OpenParenthesisToken;
                    _position++;
                    break;
                case ')':
                    _kind = SyntaxKind.CloseParenthesisToken;
                    _position++;
                    break;
                case '{':
                    _kind = SyntaxKind.OpenBraceToken;
                    _position++;
                    break;
                case '}':
                    _kind = SyntaxKind.CloseBraceToken;
                    _position++;
                    break;
                case ':':
                    _kind = SyntaxKind.ColonToken;
                    _position++;
                    break;
                case ',':
                    _kind = SyntaxKind.CommaToken;
                    _position++;
                    break;
                case '=':
                    _position++;
                    if (Current != '=')
                    {
                        _kind = SyntaxKind.EqualsToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.EqualsEqualsToken;
                        _position++;
                    }
                    break;
                case '!':
                    _position++;
                    if (Current != '=')
                    {
                        _kind = SyntaxKind.BangToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.BangEqualsToken;
                        _position++;
                    }
                    break;
                case '<':
                    _position++;
                    if (Current != '=')
                    {
                        _kind = SyntaxKind.LessToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.LessOrEqualsToken;
                        _position++;
                    }
                    break;
                case '>':
                    _position++;
                    if (Current != '=')
                    {
                        _kind = SyntaxKind.GreaterToken;
                    }
                    else
                    {
                        _kind = SyntaxKind.GreaterOrEqualsToken;
                        _position++;
                    }
                    break;
                case '"':
                    ReadString();
                    break;
                case '.':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    readNumber();
                    break;
                case '_':
                    readIdentifierOrKeyword();
                    break;
                default:
                    if (char.IsLetter(Current))
                        readIdentifierOrKeyword();
                    else
                    {
                        TextSpan span = new TextSpan(_position, 1);
                        TextLocation location = new TextLocation(_text, span);
                        _diagnostics.ReportBadCharacter(location, Current);
                        _position++;
                    }
                    break;
            }
        }

        private void ReadString()
        {
            // Skip the current quote
            _position++;

            var sb = new StringBuilder();
            bool done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        var span = new TextSpan(_start, 1);
                        var location = new TextLocation(_text, span);
                        _diagnostics.ReportUnterminatedString(location);
                        done = true;
                        break;
                    case '/':
                        if (Lookahead == '"')
                        {
                            sb.Append(Current);
                            _position += 2;
                        }
                        else if (Lookahead == '\\')
                        {
                            sb.Append(Current);
                            _position++;
                        }
                        else
                            _diagnostics.ReportInvalidEscapeSequance(new TextLocation(_text,
                                new TextSpan(_position, 2)), "\\" + Lookahead);
                        break;
                    case '"':
                        _position++;
                        done = true;
                        break;
                    default:
                        sb.Append(Current);
                        _position++;
                        break;
                }
            }

            _kind = SyntaxKind.StringToken;
            _value = sb.ToString();
        }

        private void readNumber()
        {
            while (char.IsDigit(Current) || Current == '.')
                _position++;

            int length = _position - _start;
            string text = _text.ToString(_start, length);
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                TextSpan span = new TextSpan(_start, length);
                TextLocation location = new TextLocation(_text, span);
                _diagnostics.ReportInvalidNumber(location, text, TypeSymbol.Float);
            }

            _value = value;
            _kind = SyntaxKind.FloatToken;
        }

        private void readIdentifierOrKeyword()
        {
            while (char.IsLetterOrDigit(Current) || Current == '_')
                _position++;

            _kind = SyntaxFacts.GetKeywordKind(_text.ToString(_start, _position - _start));
        }
    }
}
