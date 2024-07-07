using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal abstract class BoundNode
    {
        protected BoundNode(SyntaxNode syntax)
        {
            Syntax = syntax;
        }

        public abstract BoundNodeKind Kind { get; }
        public SyntaxNode Syntax { get; }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                this.WriteTo(writer);
                return writer.ToString();
            }
        }
    }
}
