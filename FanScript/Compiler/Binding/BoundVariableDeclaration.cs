using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundVariableDeclaration : BoundStatement
    {
        public BoundVariableDeclaration(SyntaxNode syntax, VariableSymbol variable, BoundAssignmentExpression? optionalAssignment)
            : base(syntax)
        {
            Variable = variable;
            OptionalAssignment = optionalAssignment;
        }

        public override BoundNodeKind Kind => BoundNodeKind.VariableDeclaration;

        public VariableSymbol Variable { get; }
        public BoundAssignmentExpression? OptionalAssignment { get; }
    }
}
