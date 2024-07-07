using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundIfStatement : BoundStatement
    {
        public BoundIfStatement(SyntaxNode syntax, BoundExpression condition, BoundStatement thenStatement, BoundStatement? elseStatement)
            : base(syntax)
        {
            Condition = condition;
            ThenStatement = thenStatement;
            ElseStatement = elseStatement;
        }

        public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
        public BoundExpression Condition { get; }
        public BoundStatement ThenStatement { get; }
        public BoundStatement? ElseStatement { get; }
    }
}
