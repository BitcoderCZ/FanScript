using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Lexing;
using FanScript.Compiler.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public sealed class SyntaxTree
    {
        private Dictionary<SyntaxNode, SyntaxNode?>? _parents;

        private delegate void ParseHandler(SyntaxTree syntaxTree,
                                           out CompilationUnitSyntax root,
                                           out ImmutableArray<Diagnostic> diagnostics);

        private SyntaxTree(SourceText text, ParseHandler handler)
        {
            Text = text;

            handler(this, out var root, out var diagnostics);

            Diagnostics = diagnostics;
            Root = root;
        }

        public SourceText Text { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public CompilationUnitSyntax Root { get; }

        public static SyntaxTree Load(string file)
            => Parse(SourceText.From(File.ReadAllText(file), file));

        private static void Parse(SyntaxTree syntaxTree, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> diagnostics)
        {
            Parser parser = new Parser(syntaxTree);
            root = parser.ParseCompilationUnit();
            diagnostics = parser.Diagnostics.ToImmutableArray();
        }

        public static SyntaxTree Parse(string text)
            => Parse(SourceText.From(text));

        public static SyntaxTree Parse(SourceText text)
            => new SyntaxTree(text, Parse);

        public static ImmutableArray<SyntaxToken> ParseTokens(string text, bool includeEndOfFile = false)
            => ParseTokens(SourceText.From(text), includeEndOfFile);

        public static ImmutableArray<SyntaxToken> ParseTokens(string text, out ImmutableArray<Diagnostic> diagnostics, bool includeEndOfFile = false)
            => ParseTokens(SourceText.From(text), out diagnostics, includeEndOfFile);

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, bool includeEndOfFile = false)
            => ParseTokens(text, out _, includeEndOfFile);

        public static ImmutableArray<SyntaxToken> ParseTokens(SourceText text, out ImmutableArray<Diagnostic> diagnostics, bool includeEndOfFile = false)
        {
            List<SyntaxToken> tokens = new List<SyntaxToken>();

            void ParseTokens(SyntaxTree st, out CompilationUnitSyntax root, out ImmutableArray<Diagnostic> d)
            {
                Lexer lexer = new Lexer(st);
                while (true)
                {
                    SyntaxToken token = lexer.Lex();

                    if (token.Kind != SyntaxKind.EndOfFileToken || includeEndOfFile)
                        tokens.Add(token);

                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        root = new CompilationUnitSyntax(st, ImmutableArray<MemberSyntax>.Empty, token);
                        break;
                    }
                }

                d = lexer.Diagnostics.ToImmutableArray();
            }

            SyntaxTree syntaxTree = new SyntaxTree(text, ParseTokens);
            diagnostics = syntaxTree.Diagnostics.ToImmutableArray();
            return tokens.ToImmutableArray();
        }

        internal SyntaxNode? GetParent(SyntaxNode syntaxNode)
        {
            if (_parents is null)
            {
                Dictionary<SyntaxNode, SyntaxNode> parents = CreateParentsDictionary(Root);
                Interlocked.CompareExchange(ref _parents, parents, null);
            }

            return _parents[syntaxNode];
        }

        private Dictionary<SyntaxNode, SyntaxNode?> CreateParentsDictionary(CompilationUnitSyntax root)
        {
            Dictionary<SyntaxNode, SyntaxNode> result = new Dictionary<SyntaxNode, SyntaxNode?>
            {
                { root, null }
            };
            CreateParentsDictionary(result, root);
            return result;
        }

        private void CreateParentsDictionary(Dictionary<SyntaxNode, SyntaxNode?> result, SyntaxNode node)
        {
            foreach (SyntaxNode child in node.GetChildren())
            {
                result.Add(child, node);
                CreateParentsDictionary(result, child);
            }
        }
    }
}
