using FanScript.Compiler.Binding;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System;

namespace FanScript.LangServer.Utils
{
    internal static class NodeFinder
    {
        public static SyntaxNode? FindNode(this SyntaxTree tree, TextSpan span)
        {
            SyntaxNode current = tree.Root;

            if (!current.Span.OverlapsWith(span))
                return null;

            while (true)
            {
                bool overlapingChild = false;
                foreach (SyntaxNode child in current.GetChildren())
                    if (child.Span.OverlapsWith(span))
                    {
                        current = child;
                        overlapingChild = true;
                        break;
                    }

                if (!overlapingChild)
                    return current;
            }
        }
    }
}
