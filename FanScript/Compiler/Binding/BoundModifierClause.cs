using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundModifierClause : BoundNode
    {
        public BoundModifierClause(SyntaxNode syntax, ImmutableArray<(SyntaxToken Token, Modifiers Modifiers)> tokens) : base(syntax)
        {
            Tokens = tokens;

            Enum = 0;
            foreach (var (token, mod) in Tokens)
                Enum |= mod;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ModifierClause;

        public ImmutableArray<(SyntaxToken Token, Modifiers Modifiers)> Tokens { get; }
        public Modifiers Enum { get; }

        public SyntaxToken GetTokenForModifier(Modifiers modifier)
        {
            foreach (var (token, mod) in Tokens)
                if (mod == modifier)
                    return token;

            throw new KeyNotFoundException($"Token for modifier \"{modifier}\".");
        }
    }
}
