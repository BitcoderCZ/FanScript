using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class PropertyExpressionSyntax : ExpressionSyntax
    {
        public PropertyExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken dotToken, ExpressionSyntax expression) : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            DotToken = dotToken;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.PropertyExpression;

        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken DotToken { get; }
        public ExpressionSyntax Expression { get; }
    }
}
