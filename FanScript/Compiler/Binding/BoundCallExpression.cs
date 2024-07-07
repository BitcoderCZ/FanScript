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
    internal sealed class BoundCallExpression : BoundExpression
    {
        public BoundCallExpression(SyntaxNode syntax, FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
            : base(syntax)
        {
            Function = function;
            Arguments = arguments;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.Type;
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
    }
}
