using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Syntax
{
    public abstract class AssignableClauseSyntax : SyntaxNode
    {
        public AssignableClauseSyntax(SyntaxTree syntaxTree) : base(syntaxTree)
        {
        }

        public override SyntaxKind Kind => SyntaxKind.AssignableClause;
    }

    public sealed partial class AssignableVariableClauseSyntax : AssignableClauseSyntax
    {
        public AssignableVariableClauseSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken) : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
        }

        public SyntaxToken IdentifierToken { get; }
    }

    public sealed partial class AssignablePropertyClauseSyntax : AssignableClauseSyntax
    {
        public AssignablePropertyClauseSyntax(SyntaxTree syntaxTree, SyntaxToken variableToken, SyntaxToken dotToken, SyntaxToken propertyToken) : base(syntaxTree)
        {
            VariableToken = variableToken;
            DotToken = dotToken;
            IdentifierToken = propertyToken;
        }

        public SyntaxToken VariableToken { get; }
        public SyntaxToken DotToken { get; }
        public SyntaxToken IdentifierToken { get; }
    }
}
