using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public BoundCallExpression(SyntaxNode syntax, FunctionSymbol function, ImmutableArray<Modifiers> argModifiers, ImmutableArray<BoundExpression> arguments, TypeSymbol returnType, TypeSymbol? genericType)
            : base(syntax)
        {
            if (argModifiers.Length != arguments.Length)
                throw new ArgumentException(nameof(arguments), $"{nameof(arguments)}.Length must match {nameof(argModifiers)}.Length");

            Function = function;
            ArgModifiers = argModifiers;
            Arguments = arguments;
            ReturnType = returnType;
            GenericType = genericType;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => ReturnType;
        public FunctionSymbol Function { get; }
        public ImmutableArray<Modifiers> ArgModifiers { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public TypeSymbol ReturnType { get; }
        public TypeSymbol? GenericType { get; }
    }
}
