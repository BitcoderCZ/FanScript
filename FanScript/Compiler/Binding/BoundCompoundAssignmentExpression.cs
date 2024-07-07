using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundCompoundAssignmentExpression : BoundExpression
    {
        public BoundCompoundAssignmentExpression(SyntaxNode syntax, VariableSymbol variable, BoundBinaryOperator op, BoundExpression expression)
            : base(syntax)
        {
            Variable = variable;
            Op = op;
            Expression = expression;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentExpression;
        public override TypeSymbol? Type => Expression.Type;
        public VariableSymbol Variable { get; }
        public BoundBinaryOperator Op { get; }
        public BoundExpression Expression { get; }
    }
}
