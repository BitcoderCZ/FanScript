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
        EventGotoStatement,
        ConditionalGotoStatement,
        LabelStatement,
        PostfixStatement,
        PrefixStatement,
        CallStatement,
        ExpressionStatement,
        EmitterHintStatement,

        // Expressions
        ErrorExpression,
        LiteralExpression,
        VariableExpression,
        UnaryExpression,
        BinaryExpression,
        CallExpression,
        ConversionExpression,
        ConstructorExpression,
        ArraySegmentExpression,
        NopExpression,
        PostfixExpression,
        PrefixExpression,
        AssignmentExpression,
        CompoundAssignmentExpression,

        // Clauses
        ArgumentClause,
        ModifierClause,
    }
}
