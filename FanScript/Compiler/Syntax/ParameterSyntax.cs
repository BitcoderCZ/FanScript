using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class ParameterSyntax : SyntaxNode
    {
        internal ParameterSyntax(SyntaxTree syntaxTree, TypeClauseSyntax type, SyntaxToken identifier)
            : base(syntaxTree)
        {
            Type = type;
            Identifier = identifier;
        }

        public override SyntaxKind Kind => SyntaxKind.Parameter;
        public TypeClauseSyntax Type { get; }
        public SyntaxToken Identifier { get; }
    }
}
