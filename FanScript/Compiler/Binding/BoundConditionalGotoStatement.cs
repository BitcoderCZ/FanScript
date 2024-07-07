using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundConditionalGotoStatement : BoundStatement
    {
        public BoundConditionalGotoStatement(SyntaxNode syntax, BoundLabel label, BoundExpression condition, bool jumpIfTrue = true)
            : base(syntax)
        {
            Label = label;
            Condition = condition;
            JumpIfTrue = jumpIfTrue;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;
        public BoundLabel Label { get; }
        public BoundExpression Condition { get; }
        public bool JumpIfTrue { get; }
    }
}
