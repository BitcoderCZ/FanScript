namespace FanScript.Compiler.Binding
{
    internal enum BoundNodeKind
    {
        // Statements
        BlockStatement,
        SpecialBlockStatement,
        NopStatement,
        VariableDeclarationStatement,
        AssignmentStatement,
        CompoundAssignmentStatement,
        IfStatement,
        WhileStatement,
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

        SpecialBlockCondition,

        EmitterHint,
    }
}
