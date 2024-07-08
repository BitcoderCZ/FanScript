using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundSpecialBlockStatement : BoundStatement
    {
        public BoundSpecialBlockStatement(SyntaxNode syntax, SyntaxKind keyword, BoundBlockStatement block)
            : base(syntax)
        {
            Keyword = keyword;
            Block = block;
        }

        public override BoundNodeKind Kind => BoundNodeKind.SpecialBlockStatement;
        public SyntaxKind Keyword { get; }
        public BoundBlockStatement Block { get; }
    }
}
