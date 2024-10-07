namespace FanScript.Compiler.Binding
{
    internal enum BoundNodeKind
    {
        // Statements
        BlockStatement,
        EventStatement,
        NopStatement,
        VariableDeclarationStatement,
        AssignmentStatement,
        CompoundAssignmentStatement,
        IfStatement,
        WhileStatement,
        DoWhileStatement,
        ReturnStatement,
        GotoStatement,
        RollbackGotoStatement,
        ConditionalGotoStatement,
        LabelStatement,
        PostfixStatement,
        PrefixStatement,
        ExpressionStatement,

        // Expressions
        ErrorExpression,
        LiteralExpression,
        VariableExpression,
        UnaryExpression,
        BinaryExpression,
        CallExpression,
        ConversionExpression,
        ConstructorExpression,
        ArgumentClause,
        ArraySegmentExpression,
        StatementExpression,
        NopExpression,
        PostfixExpression,
        PrefixExpression,

        EventCondition,

        EmitterHint,
    }
}
