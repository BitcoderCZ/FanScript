using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public abstract class MemberSyntax : SyntaxNode
    {
        private protected MemberSyntax(SyntaxTree syntaxTree)
            : base(syntaxTree)
        {
        }
    }
}
