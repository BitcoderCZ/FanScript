﻿namespace FanScript.Compiler.Syntax
{
    public static class SyntaxFacts
    {
        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.BangToken:
                    return 6;

                default:
                    return 0;
            }
        }

        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                case SyntaxKind.PercentToken:
                    return 5;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 4;

                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.BangEqualsToken:
                case SyntaxKind.LessToken:
                case SyntaxKind.LessOrEqualsToken:
                case SyntaxKind.GreaterToken:
                case SyntaxKind.GreaterOrEqualsToken:
                    return 3;

                case SyntaxKind.AmpersandAmpersandToken:
                    return 2;

                case SyntaxKind.PipePipeToken:
                    return 1;

                default:
                    return 0;
            }
        }

        public static bool IsComment(this SyntaxKind kind)
        {
            return kind == SyntaxKind.SingleLineCommentTrivia ||
                   kind == SyntaxKind.MultiLineCommentTrivia;
        }

        internal static SyntaxKind GetKeywordKind(string text)
        {
            switch (text)
            {
                case "true":
                    return SyntaxKind.KeywordTrue;
                case "false":
                    return SyntaxKind.KeywordFalse;
                case "for":
                    return SyntaxKind.KeywordFor;
                case "if":
                    return SyntaxKind.KeywordIf;
                case "else":
                    return SyntaxKind.KeywordElse;
                case "bool":
                    return SyntaxKind.KeywordBool;
                case "float":
                    return SyntaxKind.KeywordFloat;
                case "vec3":
                    return SyntaxKind.KeywordVector3;
                case "rot":
                    return SyntaxKind.KeywordRotation;
                case "object":
                    return SyntaxKind.KeywordObject;
                case "array":
                    return SyntaxKind.KeywordArray;
                case "while":
                    return SyntaxKind.KeywordWhile;
                case "break":
                    return SyntaxKind.KeywordBreak;
                case "continue":
                    return SyntaxKind.KeywordContinue;
                case "onPlay":
                    return SyntaxKind.KeywordOnPlay;
                default:
                    return SyntaxKind.IdentifierToken;
            }
        }

        public static string? GetText(SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.PlusToken =>
                    "+",
                SyntaxKind.PlusEqualsToken =>
                    "+=",
                SyntaxKind.MinusToken =>
                    "-",
                SyntaxKind.MinusEqualsToken =>
                    "-=",
                SyntaxKind.StarToken =>
                    "*",
                SyntaxKind.StarEqualsToken =>
                    "*=",
                SyntaxKind.SlashToken =>
                    "/",
                SyntaxKind.SlashEqualsToken =>
                    "/=",
                SyntaxKind.PercentToken =>
                    "%",
                SyntaxKind.PercentEqualsToken =>
                    "%=",
                SyntaxKind.OpenParenthesisToken =>
                    "(",
                SyntaxKind.CloseParenthesisToken =>
                    ")",
                SyntaxKind.OpenBraceToken =>
                    "{",
                SyntaxKind.CloseBraceToken =>
                    "}",
                SyntaxKind.ColonToken =>
                    ":",
                SyntaxKind.SemicolonToken =>
                    ",",
                SyntaxKind.CommaToken =>
                    ",",
                SyntaxKind.BangToken =>
                    "!",
                SyntaxKind.EqualsToken =>
                    "=",
                SyntaxKind.LessToken =>
                    "<",
                SyntaxKind.LessOrEqualsToken =>
                    "<=",
                SyntaxKind.GreaterToken =>
                    ">",
                SyntaxKind.GreaterOrEqualsToken =>
                    ">=",
                SyntaxKind.AmpersandAmpersandToken =>
                    "&&",
                SyntaxKind.PipePipeToken =>
                    "||",
                SyntaxKind.EqualsEqualsToken =>
                    "==",
                SyntaxKind.BangEqualsToken =>
                    "!=",
                SyntaxKind.KeywordFalse =>
                    "false",
                SyntaxKind.KeywordTrue =>
                    "true",
                SyntaxKind.KeywordFor =>
                    "for",
                SyntaxKind.KeywordIf =>
                    "if",
                SyntaxKind.KeywordElse =>
                    "else",
                SyntaxKind.KeywordBool =>
                    "bool",
                SyntaxKind.KeywordFloat =>
                    "float",
                SyntaxKind.KeywordVector3 =>
                    "vec3",
                SyntaxKind.KeywordRotation =>
                    "rot",
                SyntaxKind.KeywordObject =>
                    "object",
                SyntaxKind.KeywordArray =>
                    "array",
                SyntaxKind.KeywordWhile =>
                    "while",
                SyntaxKind.KeywordBreak =>
                    "break",
                SyntaxKind.KeywordContinue =>
                    "continue",
                SyntaxKind.KeywordFunction =>
                    "function",
                SyntaxKind.KeywordOnPlay =>
                    "onPlay",
                _ =>
                    null
            };

        public static bool IsTrivia(this SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.SkippedTextTrivia:
                case SyntaxKind.LineBreakTrivia:
                case SyntaxKind.WhitespaceTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsKeyword(this SyntaxKind kind)
            => kind.ToString().StartsWith("Keyword");

        public static bool IsToken(this SyntaxKind kind)
            => !kind.IsTrivia() &&
                   (kind.IsKeyword() || kind.ToString().EndsWith("Token"));

        public static SyntaxKind GetBinaryOperatorOfAssignmentOperator(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusEqualsToken:
                    return SyntaxKind.PlusToken;
                case SyntaxKind.MinusEqualsToken:
                    return SyntaxKind.MinusToken;
                case SyntaxKind.StarEqualsToken:
                    return SyntaxKind.StarToken;
                case SyntaxKind.SlashEqualsToken:
                    return SyntaxKind.SlashToken;
                case SyntaxKind.PercentEqualsToken:
                    return SyntaxKind.PercentToken;
                default:
                    throw new Exception($"Unexpected syntax: '{kind}'");
            }
        }
    }
}
