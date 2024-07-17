using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundSpecialBlockCondition : BoundExpression
    {
        public BoundSpecialBlockCondition(SyntaxNode syntax, SpecialBlockType sbType, ImmutableArray<BoundExpression> arguments) : base(syntax)
        {
            SBType = sbType;
            Arguments = arguments;
        }

        public override BoundNodeKind Kind => BoundNodeKind.SpecialBlockCondition;
        public override TypeSymbol Type => TypeSymbol.Void;

        public SpecialBlockType SBType { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
    }
}
