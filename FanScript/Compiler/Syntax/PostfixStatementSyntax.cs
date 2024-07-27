using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class PostfixStatementSyntax : StatementSyntax
    {
        internal PostfixStatementSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken operatorToken) : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            OperatorToken = operatorToken;
        }

        public override SyntaxKind Kind => SyntaxKind.PostfixStatement;

        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken OperatorToken { get; }
    }
}
