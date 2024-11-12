using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;

namespace FanScript.Compiler.Lexing;

public sealed class Lexer
{
    private static readonly HashSet<char> AllowedChars = [];

    private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
    private readonly SyntaxTree _syntaxTree;
    private readonly SourceText _text;
    private readonly ImmutableArray<SyntaxTrivia>.Builder _triviaBuilder = ImmutableArray.CreateBuilder<SyntaxTrivia>();

    private int _position;
    private int _start;
    private SyntaxKind _kind;
    private object? _value;

    static Lexer()
    {
        for (char c = '0'; c <= '9'; c++)
        {
            AllowedChars.Add(c);
        }

        for (char c = 'a'; c <= 'f'; c++)
        {
            AllowedChars.Add(c);
        }

        for (char c = 'A'; c <= 'F'; c++)
        {
            AllowedChars.Add(c);
        }

        AllowedChars.Add('.');
        AllowedChars.Add('_');
        AllowedChars.Add('x');
        AllowedChars.Add('X');
    }

    public Lexer(SyntaxTree syntaxTree)
    {
        _syntaxTree = syntaxTree;
        _text = syntaxTree.Text;
    }

    public DiagnosticBag Diagnostics => _diagnostics;

    private char Current => Peek(0);

    private char Lookahead => Peek(1);

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

        string? tokenText = SyntaxFacts.GetText(tokenKind) ?? _text.ToString(tokenStart, tokenLength);
        return new SyntaxToken(_syntaxTree, tokenKind, tokenStart, tokenText, tokenValue, leadingTrivia, trailingTrivia);
    }

    private char Peek(int offset)
    {
        int index = _position + offset;

        return index >= _text.Length ? '\0' : _text[index];
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
                    {
                        done = true;
                    }

                    ReadLineBreak();
                    break;
                case ' ':
                case '\t':
                    ReadWhiteSpace();
                    break;
                default:
                    if (char.IsWhiteSpace(Current))
                    {
                        ReadWhiteSpace();
                    }
                    else
                    {
                        done = true;
                    }

                    break;
            }

            int length = _position - _start;
            if (length > 0)
            {
                SyntaxTrivia trivia = new SyntaxTrivia(_syntaxTree, _kind, _start, _text.ToString(_start, length));
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
                    {
                        done = true;
                    }
                    else
                    {
                        _position++;
                    }

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
                {
                    _kind = SyntaxKind.PlusToken;
                }

                break;
            case '-':
                _position++;
                if (Current == '-')
                {
                    _kind = SyntaxKind.MinusMinusToken;
                    _position++;
                }
                else if (Current == '=')
                {
                    _kind = SyntaxKind.MinusEqualsToken;
                    _position++;
                }
                else
                {
                    _kind = SyntaxKind.MinusToken;
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
            case '%':
                _position++;
                if (Current != '=')
                {
                    _kind = SyntaxKind.PercentToken;
                }
                else
                {
                    _kind = SyntaxKind.PercentEqualsToken;
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
            case '[':
                _kind = SyntaxKind.OpenSquareToken;
                _position++;
                break;
            case ']':
                _kind = SyntaxKind.CloseSquareToken;
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
            case ';':
                _kind = SyntaxKind.SemicolonToken;
                _position++;
                break;
            case ',':
                _kind = SyntaxKind.CommaToken;
                _position++;
                break;
            case '#':
                _kind = SyntaxKind.HashtagToken;
                _position++;
                break;
            case '&':
                _position++;
                if (Current != '&')
                {
                    _diagnostics.ReportBadCharacter(new TextLocation(_text, new TextSpan(_position, 1)), Current);
                }
                else
                {
                    _kind = SyntaxKind.AmpersandAmpersandToken;
                    _position++;
                }

                break;
            case '|':
                _position++;
                if (Current != '|')
                {
                    _diagnostics.ReportBadCharacter(new TextLocation(_text, new TextSpan(_position, 1)), Current);
                }
                else
                {
                    _kind = SyntaxKind.PipePipeToken;
                    _position++;
                }

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
            case '.' when !char.IsDigit(Peek(1)):
                _kind = SyntaxKind.DotToken;
                _position++;
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
                ReadNumber();
                break;
            case '_':
                ReadIdentifierOrKeyword();
                break;
            default:
                if (char.IsLetter(Current))
                {
                    ReadIdentifierOrKeyword();
                }
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
                case '\\':
                    if (Lookahead == '"' || Lookahead == '\\')
                    {
                        sb.Append(Lookahead);
                    }
                    else if (Lookahead == 'n')
                    {
                        sb.Append('\n');
                    }
                    else
                    {
                        _diagnostics.ReportInvalidEscapeSequance(new TextLocation(_text, new TextSpan(_position, 2)), "\\" + Lookahead);
                    }

                    _position += 2;
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

    private void ReadNumber()
    {
        while (AllowedChars.Contains(Current))
        {
            _position++;
        }

        int length = _position - _start;
        string text = _text.ToString(_start, length);

        float value = 0f;

        if (text.EndsWith("_"))
        {
            ReportInvalid();
        }
        else
        {
            text = text.Replace("_", string.Empty);

            if (text.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase))
            {
                if (uint.TryParse(text.AsSpan(2), NumberStyles.BinaryNumber, CultureInfo.InvariantCulture, out uint numb))
                {
                    value = numb;
                }
                else
                {
                    ReportInvalid();
                }
            }
            else if (text.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                if (uint.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint numb))
                {
                    value = numb;
                }
                else
                {
                    ReportInvalid();
                }
            }
            else if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                ReportInvalid();
            }
        }

        _value = value;
        _kind = SyntaxKind.FloatToken;

        void ReportInvalid()
        {
            TextSpan span = new TextSpan(_start, length);
            TextLocation location = new TextLocation(_text, span);
            _diagnostics.ReportInvalidNumber(location, text);
        }
    }

    private void ReadIdentifierOrKeyword()
    {
        while (char.IsLetterOrDigit(Current) || Current == '_')
        {
            _position++;
        }

        _kind = SyntaxFacts.GetKeywordKind(_text.ToString(_start, _position - _start));
    }
}
