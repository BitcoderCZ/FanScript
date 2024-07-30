using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundArraySegmentExpression : BoundExpression
    {
        public BoundArraySegmentExpression(SyntaxNode syntax, TypeSymbol type, ImmutableArray<BoundExpression> elements) : base(syntax)
        {
            Type = TypeSymbol.CreateGenericInstance(TypeSymbol.ArraySegment, type);
            Elements = elements;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ArraySegmentExpression;

        public override TypeSymbol Type { get; }
        public ImmutableArray<BoundExpression> Elements { get; }
    }
}
