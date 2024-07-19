using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class ArgumentClauseSyntax : SyntaxNode
    {
        internal ArgumentClauseSyntax(SyntaxTree syntaxTree, SyntaxToken openParenthesisToken, ImmutableArray<ImmutableArray<SyntaxToken>> argumentModifiers, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParenthesisToken)
            : base(syntaxTree)
        {
            OpenParenthesisToken = openParenthesisToken;
            ArgumentModifiers = argumentModifiers;
            Arguments = arguments;
            CloseParenthesisToken = closeParenthesisToken;
        }

        public override SyntaxKind Kind => SyntaxKind.ArgumentClause;

        public SyntaxToken OpenParenthesisToken { get; }
        public ImmutableArray<ImmutableArray<SyntaxToken>> ArgumentModifiers { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenthesisToken { get; }
    }
}
