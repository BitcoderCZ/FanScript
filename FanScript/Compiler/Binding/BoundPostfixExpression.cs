using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundPostfixExpression : BoundExpression
{
    public BoundPostfixExpression(SyntaxNode syntax, VariableSymbol variable, PostfixKind postfixKind)
        : base(syntax)
    {
        Variable = variable;
        PostfixKind = postfixKind;
    }

    public override BoundNodeKind Kind => BoundNodeKind.PostfixExpression;

    public override TypeSymbol Type => Variable.Type;

    public VariableSymbol Variable { get; }

    public PostfixKind PostfixKind { get; }
}
