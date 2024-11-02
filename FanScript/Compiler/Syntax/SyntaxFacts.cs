using FanScript.Compiler.Exceptions;

namespace FanScript.Compiler.Syntax
{
    public static class SyntaxFacts
    {
        public static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.BangToken => 6,
                _ => 0,
            };

        public static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.PercentToken => 5,
                SyntaxKind.PlusToken or SyntaxKind.MinusToken => 4,
                SyntaxKind.EqualsEqualsToken or SyntaxKind.BangEqualsToken or SyntaxKind.LessToken or SyntaxKind.LessOrEqualsToken or SyntaxKind.GreaterToken or SyntaxKind.GreaterOrEqualsToken => 3,
                SyntaxKind.AmpersandAmpersandToken => 2,
                SyntaxKind.PipePipeToken => 1,
                _ => 0,
            };

        public static bool IsComment(this SyntaxKind kind)
             => kind switch
             {
                 SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia => true,
                 _ => false,
             };

        public static SyntaxKind GetKeywordKind(string text)
            => text switch
            {
                "true" => SyntaxKind.KeywordTrue,
                "false" => SyntaxKind.KeywordFalse,
                "for" => SyntaxKind.KeywordFor,
                "if" => SyntaxKind.KeywordIf,
                "else" => SyntaxKind.KeywordElse,
                "while" => SyntaxKind.KeywordWhile,
                "do" => SyntaxKind.KeywordDo,
                "break" => SyntaxKind.KeywordBreak,
                "continue" => SyntaxKind.KeywordContinue,
                "on" => SyntaxKind.KeywordOn,
                "null" => SyntaxKind.KeywordNull,
                "func" => SyntaxKind.KeywordFunction,
                "return" => SyntaxKind.KeywordReturn,
                "readonly" => SyntaxKind.ReadOnlyModifier,
                "const" => SyntaxKind.ConstantModifier,
                "ref" => SyntaxKind.RefModifier,
                "out" => SyntaxKind.OutModifier,
                "global" => SyntaxKind.GlobalModifier,
                "saved" => SyntaxKind.SavedModifier,
                "inline" => SyntaxKind.InlineModifier,
                _ => SyntaxKind.IdentifierToken,
            };

        public static IEnumerable<SyntaxKind> GetUnaryOperatorKinds()
        {
            SyntaxKind[] kinds = Enum.GetValues<SyntaxKind>();
            foreach (var kind in kinds)
            {
                if (GetUnaryOperatorPrecedence(kind) > 0)
                {
                    yield return kind;
                }
            }
        }

        public static IEnumerable<SyntaxKind> GetBinaryOperatorKinds()
        {
            SyntaxKind[] kinds = Enum.GetValues<SyntaxKind>();
            foreach (var kind in kinds)
            {
                if (GetBinaryOperatorPrecedence(kind) > 0)
                {
                    yield return kind;
                }
            }
        }

        public static string? GetText(this SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.PlusToken =>
                    "+",
                SyntaxKind.PlusPlusToken =>
                    "++",
                SyntaxKind.PlusEqualsToken =>
                    "+=",
                SyntaxKind.MinusToken =>
                    "-",
                SyntaxKind.MinusMinusToken =>
                    "--",
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
                SyntaxKind.OpenSquareToken =>
                    "[",
                SyntaxKind.CloseSquareToken =>
                    "]",
                SyntaxKind.OpenBraceToken =>
                    "{",
                SyntaxKind.CloseBraceToken =>
                    "}",
                SyntaxKind.ColonToken =>
                    ":",
                SyntaxKind.SemicolonToken =>
                    ";",
                SyntaxKind.DotToken =>
                    ".",
                SyntaxKind.CommaToken =>
                    ",",
                SyntaxKind.HashtagToken =>
                    "#",
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
                SyntaxKind.KeywordWhile =>
                    "while",
                SyntaxKind.KeywordDo =>
                    "do",
                SyntaxKind.KeywordBreak =>
                    "break",
                SyntaxKind.KeywordContinue =>
                    "continue",
                SyntaxKind.KeywordOn =>
                    "on",
                SyntaxKind.KeywordNull =>
                    "null",
                SyntaxKind.KeywordFunction =>
                    "func",
                SyntaxKind.KeywordReturn =>
                    "return",
                SyntaxKind.ReadOnlyModifier =>
                    "readonly",
                SyntaxKind.ConstantModifier =>
                    "const",
                SyntaxKind.RefModifier =>
                    "ref",
                SyntaxKind.OutModifier =>
                    "out",
                SyntaxKind.GlobalModifier =>
                    "global",
                SyntaxKind.SavedModifier =>
                    "saved",
                SyntaxKind.InlineModifier =>
                    "inline",
                _ =>
                    null,
            };

        public static bool IsTrivia(this SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.SkippedTextTrivia or SyntaxKind.LineBreakTrivia or SyntaxKind.WhitespaceTrivia or SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia => true,
                _ => false,
            };

        public static bool IsKeyword(this SyntaxKind kind)
            => kind.ToString().StartsWith("Keyword");

        public static bool IsModifier(this SyntaxKind kind)
            => kind.ToString().EndsWith("Modifier");

        public static bool IsToken(this SyntaxKind kind)
            => !kind.IsTrivia() &&
                   (kind.IsKeyword() || kind.IsModifier() || kind.ToString().EndsWith("Token"));

        public static SyntaxKind GetBinaryOperatorOfAssignmentOperator(SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.PlusEqualsToken => SyntaxKind.PlusToken,
                SyntaxKind.MinusEqualsToken => SyntaxKind.MinusToken,
                SyntaxKind.StarEqualsToken => SyntaxKind.StarToken,
                SyntaxKind.SlashEqualsToken => SyntaxKind.SlashToken,
                SyntaxKind.PercentEqualsToken => SyntaxKind.PercentToken,
                _ => throw new UnknownEnumValueException<SyntaxKind>(kind),
            };
    }
}
