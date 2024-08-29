using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class ReturnStatementSyntax : StatementSyntax
    {
        internal ReturnStatementSyntax(SyntaxTree syntaxTree, SyntaxToken returnKeyword, ExpressionSyntax? expression)
            : base(syntaxTree)
        {
            ReturnKeyword = returnKeyword;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.ReturnStatement;
        public SyntaxToken ReturnKeyword { get; }
        public ExpressionSyntax? Expression { get; }
    }
}
