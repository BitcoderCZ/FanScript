using System.Collections.Immutable;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public BoundCallExpression(SyntaxNode syntax, FunctionSymbol function, BoundArgumentClause argumentClause, TypeSymbol returnType, TypeSymbol? genericType)
            : base(syntax)
        {
            Function = function;
            ArgumentClause = argumentClause;
            ReturnType = returnType;
            GenericType = genericType;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;

        public override TypeSymbol Type => ReturnType;

        public FunctionSymbol Function { get; }

        public BoundArgumentClause ArgumentClause { get; }

        public TypeSymbol ReturnType { get; }

        public TypeSymbol? GenericType { get; }

        public ImmutableArray<Modifiers> ArgModifiers => ArgumentClause.ArgModifiers;

        public ImmutableArray<BoundExpression> Arguments => ArgumentClause.Arguments;
    }
}
