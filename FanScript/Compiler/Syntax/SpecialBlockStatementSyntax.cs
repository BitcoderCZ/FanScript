using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed partial class SpecialBlockStatementSyntax : StatementSyntax
    {
        public SpecialBlockStatementSyntax(SyntaxTree syntaxTree, SyntaxToken keywordToken, BlockStatementSyntax block)
            : base(syntaxTree)
        {
            KeywordToken = keywordToken;
            Block = block;
        }

        public override SyntaxKind Kind => SyntaxKind.SpecialBlockStatement;
        public SyntaxToken KeywordToken { get; }
        public BlockStatementSyntax Block { get; }
    }
}
