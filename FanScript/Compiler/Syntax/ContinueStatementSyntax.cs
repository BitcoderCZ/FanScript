using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    internal sealed partial class ContinueStatementSyntax : StatementSyntax
    {
        internal ContinueStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keyword)
            : base(syntaxTree)
        {
            Keyword = keyword;
        }

        public override SyntaxKind Kind => SyntaxKind.ContinueStatement;
        public SyntaxToken Keyword { get; }
    }
}
