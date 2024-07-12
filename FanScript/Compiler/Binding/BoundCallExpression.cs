using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public BoundCallExpression(SyntaxNode syntax, FunctionSymbol function, ImmutableArray<BoundExpression> arguments, TypeSymbol returnType, TypeSymbol? genericType)
            : base(syntax)
        {
            Function = function;
            Arguments = arguments;
            ReturnType = returnType;
            GenericType = genericType;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => ReturnType;
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public TypeSymbol ReturnType { get; }
        public TypeSymbol? GenericType { get; }
    }
}
