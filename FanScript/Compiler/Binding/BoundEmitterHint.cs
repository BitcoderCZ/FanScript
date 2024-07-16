using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundEmitterHint : BoundStatement
    {
        public BoundEmitterHint(SyntaxNode syntax, HintKind hint) : base(syntax)
        {
            Hint = hint;
        }

        public override BoundNodeKind Kind => BoundNodeKind.EmitterHint;

        public HintKind Hint { get; }

        public enum HintKind
        {
            StatementBlockStart,
            StatementBlockEnd,
        }
    }
}
