using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundNopExpression : BoundExpression
    {
        public BoundNopExpression(SyntaxNode syntax) : base(syntax)
        {
        }

        public override TypeSymbol Type => TypeSymbol.Error;
        public override BoundNodeKind Kind => BoundNodeKind.NopExpression;
    }
}
