using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundRollbackGotoStatement : BoundGotoStatement
    {
        public BoundRollbackGotoStatement(SyntaxNode syntax, BoundLabel label)
            : base(syntax, label)
        {
        }

        public override BoundNodeKind Kind => BoundNodeKind.RollbackGotoStatement;
    }
}
