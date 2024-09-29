using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundPostfixStatement : BoundStatement
    {
        public BoundPostfixStatement(SyntaxNode syntax, VariableSymbol variable, BoundPostfixKind postfixKind) : base(syntax)
        {
            Variable = variable;
            PostfixKind = postfixKind;
        }

        public override BoundNodeKind Kind => BoundNodeKind.PostfixStatement;

        public VariableSymbol Variable { get; }
        public BoundPostfixKind PostfixKind { get; }
    }
}
