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

        // Expressions
        ErrorExpression,
        LiteralExpression,
        VariableExpression,
        UnaryExpression,
        BinaryExpression,
        CallExpression,
        ExpressionStatement,
        ConversionExpression,
        ConstructorExpression,
        ArgumentClause,
        ArraySegmentExpression,

        EventCondition,

        EmitterHint,
    }
}
