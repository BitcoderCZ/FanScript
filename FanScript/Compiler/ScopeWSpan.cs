using FanScript.Compiler.Binding;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler
{
    public sealed class ScopeWSpan
    {
        private readonly ImmutableArray<VariableSymbol> variables;
        private readonly ImmutableArray<ScopeWSpan> children;

        public ScopeWSpan? Parent { get; private set; }
        public TextSpan Span { get; private set; }

        public ScopeWSpan()
            : this(Enumerable.Empty<VariableSymbol>(), new TextSpan(), null, Enumerable.Empty<ScopeWSpan>())
        {
        }
        public ScopeWSpan(IEnumerable<VariableSymbol> variables, TextSpan span, ScopeWSpan? parent)
            : this(variables, span, null, Enumerable.Empty<ScopeWSpan>())
        { }
        public ScopeWSpan(IEnumerable<VariableSymbol> variables, TextSpan span, ScopeWSpan? parent, IEnumerable<ScopeWSpan> children)
        {
            this.variables = variables.ToImmutableArray();
            this.children = children.ToImmutableArray();
            Parent = parent;
            Span = span;

            foreach (var child in this.children)
                child.Parent = this;
        }

        public ImmutableArray<VariableSymbol> GetVariables()
            => variables;
        public ImmutableArray<VariableSymbol> GetAllVariables()
        {
            ImmutableArray<VariableSymbol>.Builder builder = ImmutableArray.CreateBuilder<VariableSymbol>(variables.Length);

            builder.AddRange(variables);

            ScopeWSpan? current = Parent;

            while (current is not null)
            {
                builder.AddRange(current.variables);
                current = current.Parent;
            }

            return builder.ToImmutable();
        }

        public ScopeWSpan GetScopeAt(int position)
        {
            TextSpan span = new TextSpan(position, 1);

            foreach (var child in children)
                if (child.Span.OverlapsWith(span))
                    return child.GetScopeAt(position);

            return this;
        }
    }
}
