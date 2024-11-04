using System.Collections.Immutable;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Text;

namespace FanScript.Compiler
{
    public sealed class ScopeWSpan
    {
        private readonly ImmutableArray<VariableSymbol> _variables;
        private readonly ImmutableArray<ScopeWSpan> _children;

        public ScopeWSpan()
            : this([], default, null, [])
        {
        }

        public ScopeWSpan(IEnumerable<VariableSymbol> variables, TextSpan span, ScopeWSpan? parent)
            : this(variables, span, parent, [])
        {
        }

        public ScopeWSpan(IEnumerable<VariableSymbol> variables, TextSpan span, ScopeWSpan? parent, IEnumerable<ScopeWSpan> children)
        {
            _variables = variables.ToImmutableArray();
            _children = children.ToImmutableArray();
            Parent = parent;
            Span = span;

            foreach (var child in _children)
            {
                child.Parent = this;
            }
        }

        public ScopeWSpan? Parent { get; private set; }

        public TextSpan Span { get; private set; }

        public ImmutableArray<VariableSymbol> GetVariables()
            => _variables;

        public ImmutableArray<VariableSymbol> GetAllVariables()
        {
            ImmutableArray<VariableSymbol>.Builder builder = ImmutableArray.CreateBuilder<VariableSymbol>(_variables.Length);

            builder.AddRange(_variables);

            ScopeWSpan? current = Parent;

            while (current is not null)
            {
                builder.AddRange(current._variables);
                current = current.Parent;
            }

            return builder.ToImmutable();
        }

        public ScopeWSpan GetScopeAt(int position)
        {
            TextSpan span = new TextSpan(position, 1);

            foreach (var child in _children)
            {
                if (child.Span.OverlapsWith(span))
                {
                    return child.GetScopeAt(position);
                }
            }

            return this;
        }
    }
}
