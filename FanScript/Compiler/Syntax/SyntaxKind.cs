﻿namespace FanScript.Compiler.Syntax
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
        SpecialBlockStatement,
        ExpressionStatement,
        IfStatement,
        ElseClause,
        WhileStatement,
        BreakStatement,
        ContinueStatement,
        VariableDeclaration,
        AssignmentStatement,
        ArrayInitializerStatement,

        UnaryExpression,
        BinaryExpression,
        ParenthesizedExpression,
        LiteralExpression,
        CallExpression,
        NameExpression,
        ConstructorExpression,

        Parameter,
        TypeClause,

        FloatToken,
        StringToken,

        KeywordTrue,
        KeywordFalse,
        KeywordFor,
        KeywordIf,
        KeywordElse,
        KeywordBool,
        KeywordFloat,
        KeywordVector3,
        KeywordRotation,
        KeywordObject,
        KeywordArray,
        KeywordWhile,
        KeywordBreak,
        KeywordContinue,

        KeywordOnPlay,

        SkippedTextTrivia,
        LineBreakTrivia,
        WhitespaceTrivia,
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,

        FunctionDeclaration,

        KeywordFunction,
    }
}
