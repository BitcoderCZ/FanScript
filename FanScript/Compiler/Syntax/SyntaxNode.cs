using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public abstract class SyntaxNode
    {
        private protected SyntaxNode(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }

        public SyntaxTree SyntaxTree { get; }

        public SyntaxNode? Parent => SyntaxTree.GetParent(this);

        public abstract SyntaxKind Kind { get; }

        public virtual TextSpan Span
        {
            get
            {
                var first = GetChildren().First().Span;
                var last = GetChildren().Last().Span;
                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        public virtual TextSpan FullSpan
        {
            get
            {
                var first = GetChildren().First().FullSpan;
                var last = GetChildren().Last().FullSpan;
                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        public TextLocation Location => new TextLocation(SyntaxTree.Text, Span);

        public IEnumerable<SyntaxNode> AncestorsAndSelf()
        {
            var node = this;
            while (node is not null)
            {
                yield return node;
                node = node.Parent;
            }
        }

        public IEnumerable<SyntaxNode> Ancestors()
        {
            return AncestorsAndSelf().Skip(1);
        }

        public abstract IEnumerable<SyntaxNode> GetChildren();

        public SyntaxToken GetLastToken()
        {
            if (this is SyntaxToken token)
                return token;

            // A syntax node should always contain at least 1 token.
            return GetChildren().Last().GetLastToken();
        }

        public void WriteTo(TextWriter writer)
        {
            PrettyPrint(writer, this);
        }

        private static void PrettyPrint(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
        {
            bool isToConsole = writer == Console.Out;
            SyntaxToken token = node as SyntaxToken;

            if (token is not null)
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    if (isToConsole)
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                    writer.Write(indent);
                    writer.Write("├──");

                    if (isToConsole)
                        Console.ForegroundColor = ConsoleColor.DarkGreen;

                    writer.WriteLine($"L: {trivia.Kind}");
                }
            }

            var hasTrailingTrivia = token is not null && token.TrailingTrivia.Any();
            var tokenMarker = !hasTrailingTrivia && isLast ? "└──" : "├──";

            if (isToConsole)
                Console.ForegroundColor = ConsoleColor.DarkGray;

            writer.Write(indent);
            writer.Write(tokenMarker);

            if (isToConsole)
                Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;

            writer.Write(node.Kind);

            if (token is not null && token.Value is not null)
            {
                writer.Write(" ");
                writer.Write(token.Value);
            }

            if (isToConsole)
                Console.ResetColor();

            writer.WriteLine();

            if (token is not null)
            {
                foreach (var trivia in token.TrailingTrivia)
                {
                    var isLastTrailingTrivia = trivia == token.TrailingTrivia.Last();
                    var triviaMarker = isLast && isLastTrailingTrivia ? "└──" : "├──";

                    if (isToConsole)
                        Console.ForegroundColor = ConsoleColor.DarkGray;

                    writer.Write(indent);
                    writer.Write(triviaMarker);

                    if (isToConsole)
                        Console.ForegroundColor = ConsoleColor.DarkGreen;

                    writer.WriteLine($"T: {trivia.Kind}");
                }
            }

            indent += isLast ? "   " : "│  ";

            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
                PrettyPrint(writer, child, indent, child == lastChild);
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }
    }
}
