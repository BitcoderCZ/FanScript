using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;

namespace FanScript.LangServer.Utils
{
    internal static class SyntaxFinder
    {
        public static object? FindSyntax(this SyntaxTree tree, TextSpan span)
        {
            SyntaxNode current = tree.Root;

            if (!current.FullSpan.OverlapsWith(span))
                return null;

            while (true)
            {
                bool overlapingChild = false;
                foreach (SyntaxNode child in current.GetChildren())
                {
                    if (child is SyntaxToken token)
                    {
                        for (int i = 0; i < token.LeadingTrivia.Length; i++)
                        {
                            if (token.LeadingTrivia[i].Span.OverlapsWith(span))
                                return token.LeadingTrivia[i];
                        }

                        for (int i = 0; i < token.TrailingTrivia.Length; i++)
                        {
                            if (token.TrailingTrivia[i].Span.OverlapsWith(span))
                                return token.TrailingTrivia[i];
                        }
                    }

                    if (child.FullSpan.OverlapsWith(span))
                    {
                        current = child;
                        overlapingChild = true;
                        break;
                    }
                }

                if (!overlapingChild)
                    return current;
            }
        }
    }
}