using System.Collections.Immutable;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundArgumentClause : BoundNode
    {
        public BoundArgumentClause(SyntaxNode syntax, ImmutableArray<Modifiers> argModifiers, ImmutableArray<BoundExpression> arguments)
            : base(syntax)
        {
            if (argModifiers.Length != arguments.Length)
            {
                throw new ArgumentException(nameof(arguments), $"{nameof(arguments)}.Length must match {nameof(argModifiers)}.Length");
            }

            ArgModifiers = argModifiers;
            Arguments = arguments;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ArgumentClause;

        public ImmutableArray<Modifiers> ArgModifiers { get; }

        public ImmutableArray<BoundExpression> Arguments { get; }
    }
}
