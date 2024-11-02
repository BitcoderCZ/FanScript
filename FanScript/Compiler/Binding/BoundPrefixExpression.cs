using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundPrefixExpression : BoundExpression
    {
        public BoundPrefixExpression(SyntaxNode syntax, VariableSymbol variable, PrefixKind prefixKind)
            : base(syntax)
        {
            Variable = variable;
            PrefixKind = prefixKind;
        }

        public override BoundNodeKind Kind => BoundNodeKind.PrefixExpression;

        public override TypeSymbol Type => Variable.Type;

        public VariableSymbol Variable { get; }

        public PrefixKind PrefixKind { get; }
    }
}
