using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public enum SyntaxKind : byte
    {
        BadToken,
        EndOfFileToken,
        WhitespaceToken,
        PlusToken,
        PlusEqualsToken,
        MinusToken,
        MinusEqualsToken,
        StarToken,
        StarEqualsToken,
        SlashToken,
        SlashEqualsToken,
        PercentToken,
        PercentEqualsToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenSquareToken,
        CloseSquareToken,
        OpenBraceToken,
        CloseBraceToken,
        ColonToken,
        SemicolonToken,
        CommaToken,
        DotToken,
        AmpersandAmpersandToken,
        PipePipeToken,
        EqualsToken,
        EqualsEqualsToken,
        BangToken,
        BangEqualsToken,
        LessToken,
        LessOrEqualsToken,
        GreaterToken,
        GreaterOrEqualsToken,
        IdentifierToken,

        CompilationUnit,
        GlobalStatement,
        BlockStatement,
        ExpressionStatement,
        IfStatement,
        ElseClause,
        WhileStatement,
        VariableDeclaration,

        AssignmentExpression,
        UnaryExpression,
        BinaryExpression,
        ParenthesizedExpression,
        LiteralExpression,
        CallExpression,
        NameExpression,

        Parameter,
        TypeClause,

        FloatToken,
        StringToken,

        KeywordTrue,
        KeywordFalse,
        KeywordFor,
        KeywordIf,
        KeywordElse,
        KeywordOnPlay,
        KeywordFloat,
        KeywordVector3,
        KeywordBool,
        KeywordWhile,

        SkippedTextTrivia,
        LineBreakTrivia,
        WhitespaceTrivia,
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,

        FunctionDeclaration,

        // TO remove
        FunctionKeyword, 
    }
}
