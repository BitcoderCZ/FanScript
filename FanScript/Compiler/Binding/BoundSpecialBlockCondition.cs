using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundSpecialBlockCondition : BoundExpression
    {
        public BoundSpecialBlockCondition(SyntaxNode syntax, SyntaxKind keyword) : base(syntax)
        {
            Keyword = keyword;
        }

        public override BoundNodeKind Kind => BoundNodeKind.SpecialBlockCondition;
        public override TypeSymbol? Type => TypeSymbol.Void;

        public SyntaxKind Keyword { get; }
    }
}
