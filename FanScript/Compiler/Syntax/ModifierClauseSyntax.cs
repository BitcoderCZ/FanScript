using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class ModifierClauseSyntax : SyntaxNode
    {
        public ModifierClauseSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers, ExpressionSyntax expression) : base(syntaxTree)
        {
            Modifiers = modifiers;
            Expression = expression;
        }

        public override SyntaxKind Kind => throw new NotImplementedException();

        public ImmutableArray<SyntaxToken> Modifiers { get; }
        public ExpressionSyntax Expression { get; }
    }
}
