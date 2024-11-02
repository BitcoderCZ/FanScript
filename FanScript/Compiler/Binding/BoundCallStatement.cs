using System.Collections.Immutable;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundCallStatement : BoundStatement
    {
        public BoundCallStatement(SyntaxNode syntax, FunctionSymbol function, BoundArgumentClause argumentClause, TypeSymbol returnType, TypeSymbol? genericType, VariableSymbol? resultVariable)
            : base(syntax)
        {
            Function = function;
            ArgumentClause = argumentClause;
            ReturnType = returnType;
            GenericType = genericType;
            ResultVariable = resultVariable;
        }

        public override BoundNodeKind Kind => BoundNodeKind.CallStatement;

        public FunctionSymbol Function { get; }

        public BoundArgumentClause ArgumentClause { get; }

        public TypeSymbol ReturnType { get; }

        public TypeSymbol? GenericType { get; }

        /// <summary>
        /// Variable to which the result should be assigned to, if the type of <see cref="Function"/> != <see cref="TypeSymbol.Void"/> and <see cref="ResultVariable"/> is not <see langword="null"/>
        /// </summary>
        public VariableSymbol? ResultVariable { get; }

        public ImmutableArray<Modifiers> ArgModifiers => ArgumentClause.ArgModifiers;

        public ImmutableArray<BoundExpression> Arguments => ArgumentClause.Arguments;
    }
}
