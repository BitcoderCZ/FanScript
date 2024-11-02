using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundEventGotoStatement : BoundStatement
    {
        public BoundEventGotoStatement(SyntaxNode syntax, BoundLabel label, EventType eventType, BoundArgumentClause? argumentClause)
            : base(syntax)
        {
            Label = label;
            EventType = eventType;
            ArgumentClause = argumentClause;
        }

        public override BoundNodeKind Kind => BoundNodeKind.EventGotoStatement;

        public BoundLabel Label { get; }

        public EventType EventType { get; }

        public BoundArgumentClause? ArgumentClause { get; }
    }
}
