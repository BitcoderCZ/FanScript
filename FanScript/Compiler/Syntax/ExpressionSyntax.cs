using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public abstract class ExpressionSyntax : SyntaxNode
    {
        private protected ExpressionSyntax(SyntaxTree syntaxTree)
            : base(syntaxTree)
        {
        }
    }
}
