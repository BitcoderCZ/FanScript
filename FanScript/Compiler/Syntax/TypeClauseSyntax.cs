using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class TypeClauseSyntax : SyntaxNode
    {
        internal TypeClauseSyntax(SyntaxTree syntaxTree, SyntaxToken identifier)
            : base(syntaxTree)
        {
            Identifier = identifier;
        }

        public override SyntaxKind Kind => SyntaxKind.TypeClause;
        public SyntaxToken Identifier { get; }
    }
}
