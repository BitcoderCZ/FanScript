using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundPrefixStatement : BoundStatement
    {
        public BoundPrefixStatement(SyntaxNode syntax, VariableSymbol variable, PrefixKind prefixKind)
            : base(syntax)
        {
            Variable = variable;
            PrefixKind = prefixKind;
        }

        public override BoundNodeKind Kind => BoundNodeKind.PrefixStatement;

        public VariableSymbol Variable { get; }

        public PrefixKind PrefixKind { get; }
    }
}
