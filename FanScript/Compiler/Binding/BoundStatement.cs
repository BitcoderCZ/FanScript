using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal abstract class BoundStatement : BoundNode
    {
        protected BoundStatement(SyntaxNode syntax)
            : base(syntax)
        {
        }
    }
}
