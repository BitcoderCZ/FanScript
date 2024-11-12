using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal sealed class BoundVariableDeclarationStatement : BoundStatement
{
    public BoundVariableDeclarationStatement(SyntaxNode syntax, VariableSymbol variable, BoundStatement? optionalAssignment)
        : base(syntax)
    {
        Variable = variable;
        OptionalAssignment = optionalAssignment;
    }

    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;

    public VariableSymbol Variable { get; }

    public BoundStatement? OptionalAssignment { get; }
}
