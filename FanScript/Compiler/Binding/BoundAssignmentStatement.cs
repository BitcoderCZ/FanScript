using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundAssignmentStatement : BoundStatement
    {
        public BoundAssignmentStatement(SyntaxNode syntax, VariableSymbol variable, BoundExpression expression)
            : base(syntax)
        {
            Variable = variable;
            Expression = expression;
        }

        public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;

        public VariableSymbol Variable { get; }

        public BoundExpression Expression { get; }
    }
}
