﻿namespace FanScript.Compiler.Syntax
{
    public enum SyntaxKind : byte
    {
        BadToken,
        EndOfFileToken,
        PlusToken,
        PlusPlusToken,
        PlusEqualsToken,
        MinusToken,
        MinusMinusToken,
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
        HashtagToken,
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
        EventStatement,
        ExpressionStatement,
        IfStatement,
        WhileStatement,
        DoWhileStatement,
        BreakStatement,
        ContinueStatement,
        VariableDeclarationStatement,
        AssignmentStatement,
        PostfixExpression,
        ReturnStatement,
        BuildCommandStatement,

        UnaryExpression,
        BinaryExpression,
        ParenthesizedExpression,
        LiteralExpression,
        CallExpression,
        NameExpression,
        ConstructorExpression,
        PropertyExpression,
        VariableDeclarationExpression,
        ArraySegmentExpression,

        Parameter,
        TypeClause,
        ArgumentClause,
        ElseClause,
        AssignableClause,
        ModifierClause,

        FloatToken,
        StringToken,

        KeywordTrue,
        KeywordFalse,
        KeywordFor,
        KeywordIf,
        KeywordElse,
        KeywordWhile,
        KeywordDo,
        KeywordBreak,
        KeywordContinue,
        KeywordOn,
        KeywordNull,

        SkippedTextTrivia,
        LineBreakTrivia,
        WhitespaceTrivia,
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,

        FunctionDeclaration,

        KeywordFunction,
        KeywordReturn,

        ReadOnlyModifier,
        ConstantModifier,
        RefModifier,
        OutModifier,
        GlobalModifier,
        SavedModifier,
        InlineModifier,
    }
}
