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
        public ModifierClauseSyntax(SyntaxTree syntaxTree, ImmutableArray<SyntaxToken> modifiers) : base(syntaxTree)
        {
            Modifiers = modifiers;
        }

        public override SyntaxKind Kind => SyntaxKind.ModifierClause;

        public ImmutableArray<SyntaxToken> Modifiers { get; }
    }
}
