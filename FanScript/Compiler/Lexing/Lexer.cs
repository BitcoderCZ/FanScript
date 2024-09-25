using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace FanScript.Compiler.Lexing
{
    public sealed class Lexer
    {
        private readonly DiagnosticBag diagnostics = new DiagnosticBag();
        private readonly SyntaxTree syntaxTree;
        private readonly SourceText text;
        private int position;

        private int start;
        private SyntaxKind kind;
        private object? value;
        private ImmutableArray<SyntaxTrivia>.Builder triviaBuilder = ImmutableArray.CreateBuilder<SyntaxTrivia>();

        public Lexer(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
            text = syntaxTree.Text;
        }

        public DiagnosticBag Diagnostics => diagnostics;

        private char Current => Peek(0);

        private char Lookahead => Peek(1);

        private char Peek(int offset)
        {
            var index = position + offset;

            if (index >= text.Length)
                return '\0';

            return text[index];
        }

        public SyntaxToken Lex()
        {
            ReadTrivia(leading: true);

            ImmutableArray<SyntaxTrivia> leadingTrivia = triviaBuilder.ToImmutable();
            int tokenStart = position;

            ReadToken();

            SyntaxKind tokenKind = kind;
            object? tokenValue = value;
            int tokenLength = position - start;

            ReadTrivia(leading: false);

            ImmutableArray<SyntaxTrivia> trailingTrivia = triviaBuilder.ToImmutable();

            string? tokenText = SyntaxFacts.GetText(tokenKind);
            if (tokenText is null)
                tokenText = text.ToString(tokenStart, tokenLength);

            return new SyntaxToken(syntaxTree, tokenKind, tokenStart, tokenText, tokenValue, leadingTrivia, trailingTrivia);
        }

        private void ReadTrivia(bool leading)
        {
            triviaBuilder.Clear();

            bool done = false;

            while (!done)
            {
                start = position;
                kind = SyntaxKind.BadToken;
                value = null;

                switch (Current)
                {
                    case '\0':
                        done = true;
                        break;
                    case '/':
                        if (Lookahead == '/')
                            ReadSingleLineComment();
                        else if (Lookahead == '*')
                            ReadMultiLineComment();
                        else
                            done = true;
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

                int length = position - start;
                if (length > 0)
                {
                    SyntaxTrivia trivia = new SyntaxTrivia(syntaxTree, kind, start, text.ToString(start, length));
                    triviaBuilder.Add(trivia);
                }
            }
        }

        private void ReadLineBreak()
        {
            if (Current == '\r' && Lookahead == '\n')
                position += 2;
            else
                position++;

            kind = SyntaxKind.LineBreakTrivia;
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
                        {
                            if (!char.IsWhiteSpace(Current))
                                done = true;
                            else
                                position++;
                        }
                        break;
                }
            }

            kind = SyntaxKind.WhitespaceTrivia;
        }


        private void ReadSingleLineComment()
        {
            position += 2;
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
                        position++;
                        break;
                }
            }

            kind = SyntaxKind.SingleLineCommentTrivia;
        }

        private void ReadMultiLineComment()
        {
            position += 2;
            bool done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                        TextLocation location = new TextLocation(text, new TextSpan(start, 2));
                        diagnostics.ReportUnterminatedMultiLineComment(location);
                        done = true;
                        break;
                    case '*':
                        if (Lookahead == '/')
                        {
                            position++;
                            done = true;
                        }
                        position++;
                        break;
                    default:
                        position++;
                        break;
                }
            }

            kind = SyntaxKind.MultiLineCommentTrivia;
        }

        private void ReadToken()
        {
            start = position;
            kind = SyntaxKind.BadToken;
            value = null;

            switch (Current)
            {
                case '\0':
                    kind = SyntaxKind.EndOfFileToken;
                    break;
                case '+':
                    position++;
                    if (Current == '+')
                    {
                        kind = SyntaxKind.PlusPlusToken;
                        position++;
                    }
                    else if (Current == '=')
                    {
                        kind = SyntaxKind.PlusEqualsToken;
                        position++;
                    }
                    else
                        kind = SyntaxKind.PlusToken;
                    break;
                case '-':
                    position++;
                    if (Current == '-')
                    {
                        kind = SyntaxKind.MinusMinusToken;
                        position++;
                    }
                    else if (Current == '=')
                    {
                        kind = SyntaxKind.MinusEqualsToken;
                        position++;
                    }
                    else
                        kind = SyntaxKind.MinusToken;
                    break;
                case '*':
                    position++;
                    if (Current != '=')
                        kind = SyntaxKind.StarToken;
                    else
                    {
                        kind = SyntaxKind.StarEqualsToken;
                        position++;
                    }
                    break;
                case '/':
                    position++;
                    if (Current != '=')
                        kind = SyntaxKind.SlashToken;
                    else
                    {
                        kind = SyntaxKind.SlashEqualsToken;
                        position++;
                    }
                    break;
                case '%':
                    position++;
                    if (Current != '=')
                        kind = SyntaxKind.PercentToken;
                    else
                    {
                        kind = SyntaxKind.PercentEqualsToken;
                        position++;
                    }
                    break;
                case '(':
                    kind = SyntaxKind.OpenParenthesisToken;
                    position++;
                    break;
                case ')':
                    kind = SyntaxKind.CloseParenthesisToken;
                    position++;
                    break;
                case '[':
                    kind = SyntaxKind.OpenSquareToken;
                    position++;
                    break;
                case ']':
                    kind = SyntaxKind.CloseSquareToken;
                    position++;
                    break;
                case '{':
                    kind = SyntaxKind.OpenBraceToken;
                    position++;
                    break;
                case '}':
                    kind = SyntaxKind.CloseBraceToken;
                    position++;
                    break;
                case ':':
                    kind = SyntaxKind.ColonToken;
                    position++;
                    break;
                case ';':
                    kind = SyntaxKind.SemicolonToken;
                    position++;
                    break;
                case ',':
                    kind = SyntaxKind.CommaToken;
                    position++;
                    break;
                case '#':
                    kind = SyntaxKind.HashtagToken;
                    position++;
                    break;
                case '&':
                    position++;
                    if (Current != '&')
                        diagnostics.ReportBadCharacter(new TextLocation(text, new TextSpan(position, 1)), Current);
                    else
                    {
                        kind = SyntaxKind.AmpersandAmpersandToken;
                        position++;
                    }
                    break;
                case '|':
                    position++;
                    if (Current != '|')
                        diagnostics.ReportBadCharacter(new TextLocation(text, new TextSpan(position, 1)), Current);
                    else
                    {
                        kind = SyntaxKind.PipePipeToken;
                        position++;
                    }
                    break;
                case '=':
                    position++;
                    if (Current != '=')
                    {
                        kind = SyntaxKind.EqualsToken;
                    }
                    else
                    {
                        kind = SyntaxKind.EqualsEqualsToken;
                        position++;
                    }
                    break;
                case '!':
                    position++;
                    if (Current != '=')
                    {
                        kind = SyntaxKind.BangToken;
                    }
                    else
                    {
                        kind = SyntaxKind.BangEqualsToken;
                        position++;
                    }
                    break;
                case '<':
                    position++;
                    if (Current != '=')
                    {
                        kind = SyntaxKind.LessToken;
                    }
                    else
                    {
                        kind = SyntaxKind.LessOrEqualsToken;
                        position++;
                    }
                    break;
                case '>':
                    position++;
                    if (Current != '=')
                    {
                        kind = SyntaxKind.GreaterToken;
                    }
                    else
                    {
                        kind = SyntaxKind.GreaterOrEqualsToken;
                        position++;
                    }
                    break;
                case '.' when !char.IsDigit(Peek(1)):
                    kind = SyntaxKind.DotToken;
                    position++;
                    break;
                case '"':
                    readString();
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
                        TextSpan span = new TextSpan(position, 1);
                        TextLocation location = new TextLocation(text, span);
                        diagnostics.ReportBadCharacter(location, Current);
                        position++;
                    }
                    break;
            }
        }

        private void readString()
        {
            // Skip the current quote
            position++;

            var sb = new StringBuilder();
            bool done = false;

            while (!done)
            {
                switch (Current)
                {
                    case '\0':
                    case '\r':
                    case '\n':
                        var span = new TextSpan(start, 1);
                        var location = new TextLocation(text, span);
                        diagnostics.ReportUnterminatedString(location);
                        done = true;
                        break;
                    case '\\':
                        if (Lookahead == '"' || Lookahead == '\\')
                            sb.Append(Lookahead);
                        else if (Lookahead == 'n')
                            sb.Append('\n');
                        else
                            diagnostics.ReportInvalidEscapeSequance(new TextLocation(text,
                                new TextSpan(position, 2)), "\\" + Lookahead);

                        position += 2;
                        break;
                    case '"':
                        position++;
                        done = true;
                        break;
                    default:
                        sb.Append(Current);
                        position++;
                        break;
                }
            }

            kind = SyntaxKind.StringToken;
            value = sb.ToString();
        }

        private void readNumber()
        {
            while (char.IsDigit(Current) || Current == '.')
                position++;

            int length = position - start;
            string text = this.text.ToString(start, length);
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                TextSpan span = new TextSpan(start, length);
                TextLocation location = new TextLocation(this.text, span);
                diagnostics.ReportInvalidNumber(location, text, TypeSymbol.Float);
            }

            this.value = value;
            kind = SyntaxKind.FloatToken;
        }

        private void readIdentifierOrKeyword()
        {
            while (char.IsLetterOrDigit(Current) || Current == '_')
                position++;

            kind = SyntaxFacts.GetKeywordKind(text.ToString(start, position - start));
        }
    }
}
