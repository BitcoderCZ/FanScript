using System.Collections.Immutable;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundUnaryOperator
    {
        public static ImmutableArray<BoundUnaryOperator> Operators =
        [
            new BoundUnaryOperator(SyntaxKind.BangToken, BoundUnaryOperatorKind.LogicalNegation, TypeSymbol.Bool),

            new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, TypeSymbol.Float),
            new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, TypeSymbol.Float),

            new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, TypeSymbol.Vector3),
            new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, TypeSymbol.Vector3),

            new BoundUnaryOperator(SyntaxKind.PlusToken, BoundUnaryOperatorKind.Identity, TypeSymbol.Rotation),
            new BoundUnaryOperator(SyntaxKind.MinusToken, BoundUnaryOperatorKind.Negation, TypeSymbol.Rotation),
        ];

        private BoundUnaryOperator(SyntaxKind syntaxKind, BoundUnaryOperatorKind kind, TypeSymbol operandType)
            : this(syntaxKind, kind, operandType, operandType)
        {
        }

        private BoundUnaryOperator(SyntaxKind syntaxKind, BoundUnaryOperatorKind kind, TypeSymbol operandType, TypeSymbol resultType)
        {
            SyntaxKind = syntaxKind;
            Kind = kind;
            OperandType = operandType;
            Type = resultType;
        }

        public SyntaxKind SyntaxKind { get; }

        public BoundUnaryOperatorKind Kind { get; }

        public TypeSymbol OperandType { get; }

        public TypeSymbol Type { get; }

        public static BoundUnaryOperator? Bind(SyntaxKind syntaxKind, TypeSymbol? operandType)
        {
            foreach (BoundUnaryOperator op in Operators)
            {
                if (op.SyntaxKind == syntaxKind && op.OperandType == operandType)
                {
                    return op;
                }
            }

            return null;
        }
    }
}
