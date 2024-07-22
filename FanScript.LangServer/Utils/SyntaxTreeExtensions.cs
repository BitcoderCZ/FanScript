using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System;

namespace FanScript.LangServer.Utils
{
    internal static class SyntaxTreeExtensions
    {
        public static SyntaxNode? FindNode(this SyntaxTree tree, int location)
            => tree.FindNode(span => location >= span.Start && location < span.End);
        public static SyntaxNode? FindNode(this SyntaxTree tree, TextSpan span)
            => tree.FindNode(span.OverlapsWith);
        public static SyntaxNode? FindNode(this SyntaxTree tree, Func<TextSpan, bool> overlapsWith)
        {
            SyntaxNode current = tree.Root;

            if (!overlapsWith(current.Span))
                return null;

            while (true)
            {
                bool overlapingChild = false;
                foreach (SyntaxNode child in current.GetChildren())
                    if (overlapsWith(child.Span))
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
