using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundConversionExpression : BoundExpression
{
    public BoundConversionExpression(SyntaxNode syntax, TypeSymbol type, BoundExpression expression)
        : base(syntax)
    {
        Type = type;
        Expression = expression;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ConversionExpression;

    public override TypeSymbol Type { get; }

    public BoundExpression Expression { get; }
}
