using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundVariableExpression : BoundExpression
{
    public BoundVariableExpression(SyntaxNode syntax, VariableSymbol variable)
        : base(syntax)
    {
        Variable = variable;
    }

    public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;

    public override TypeSymbol Type => Variable.Type;

    public VariableSymbol Variable { get; }

    public override BoundConstant? ConstantValue => Variable.Constant;
}
