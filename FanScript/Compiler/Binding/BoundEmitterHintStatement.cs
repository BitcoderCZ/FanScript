using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundEmitterHintStatement : BoundStatement
    {
        public BoundEmitterHintStatement(SyntaxNode syntax, HintKind hint) : base(syntax)
        {
            Hint = hint;
        }

        public override BoundNodeKind Kind => BoundNodeKind.EmitterHintStatement;

        public HintKind Hint { get; }

        public enum HintKind
        {
            StatementBlockStart,
            StatementBlockEnd,
            HighlightStart,
            HighlightEnd,
        }
    }
}
