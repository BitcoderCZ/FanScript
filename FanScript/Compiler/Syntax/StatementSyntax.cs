using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public abstract class StatementSyntax : SyntaxNode
    {
        private protected StatementSyntax(SyntaxTree syntaxTree)
            : base(syntaxTree)
        {
        }
    }
}
