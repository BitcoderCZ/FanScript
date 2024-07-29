using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundSpecialBlockCondition : BoundExpression
    {
        public BoundSpecialBlockCondition(SyntaxNode syntax, SpecialBlockType sbType, BoundArgumentClause? argumentClause) : base(syntax)
        {
            SBType = sbType;
            ArgumentClause = argumentClause;
        }

        public override BoundNodeKind Kind => BoundNodeKind.SpecialBlockCondition;
        public override TypeSymbol Type => TypeSymbol.Void;

        public SpecialBlockType SBType { get; }
        public BoundArgumentClause? ArgumentClause { get; }
    }
}
