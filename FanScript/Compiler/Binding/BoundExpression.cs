using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal abstract class BoundExpression : BoundNode
{
    protected BoundExpression(SyntaxNode syntax)
        : base(syntax)
    {
    }

    public abstract TypeSymbol Type { get; }

    public virtual BoundConstant? ConstantValue => null;
}
