using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Binding
{
    internal sealed class BoundPostfixStatement : BoundStatement
    {
        public BoundPostfixStatement(SyntaxNode syntax, VariableSymbol variable, BoundPostfixKind postfixKind) : base(syntax)
        {
            Variable = variable;
            PostfixKind = postfixKind;
        }

        public override BoundNodeKind Kind => BoundNodeKind.PostfixStatement;

        public VariableSymbol Variable { get; }
        public BoundPostfixKind PostfixKind { get; }
    }
}
