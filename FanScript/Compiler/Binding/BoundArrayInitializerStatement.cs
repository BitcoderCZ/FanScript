using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundArrayInitializerStatement : BoundStatement
    {
        public BoundArrayInitializerStatement(SyntaxNode syntax, VariableSymbol variable, ImmutableArray<BoundExpression> elements) : base(syntax)
        {
            Variable = variable;
            Elements = elements;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ArrayInitializerStatement;

        public VariableSymbol Variable { get; }
        public ImmutableArray<BoundExpression> Elements { get; }
    }
}
