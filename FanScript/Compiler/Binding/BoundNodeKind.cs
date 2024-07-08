using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal enum BoundNodeKind
    { 
        // Statements
        BlockStatement,
        SpecialBlockStatement,
        NopStatement,
        VariableDeclaration,
        IfStatement,
        WhileStatement,
        ReturnStatement,
        GotoStatement,
        RollbackGotoStatement,
        ConditionalGotoStatement,
        LabelStatement,

        // Expressions
        ErrorExpression,
        LiteralExpression,
        VariableExpression,
        AssignmentExpression,
        UnaryExpression,
        BinaryExpression,
        CallExpression,
        ExpressionStatement,
        ConversionExpression,
        CompoundAssignmentExpression,

        SpecialBlockCondition,
    }
}
