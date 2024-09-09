using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundEventCondition : BoundExpression
    {
        public BoundEventCondition(SyntaxNode syntax, EventType sbType, BoundArgumentClause? argumentClause) : base(syntax)
        {
            EventType = sbType;
            ArgumentClause = argumentClause;
        }

        public override BoundNodeKind Kind => BoundNodeKind.EventCondition;
        public override TypeSymbol Type => TypeSymbol.Void;

        public EventType EventType { get; }
        public BoundArgumentClause? ArgumentClause { get; }
    }
}
