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
        CallStatement,
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
        NopExpression,
        PostfixExpression,
        PrefixExpression,
        AssignmentExpression,
        CompoundAssignmentExpression,

        EventCondition,

        EmitterHint,
    }
}
