namespace FanScript.Compiler.Syntax
{
    partial class ArrayInitializerStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
            yield return EqualsToken;
            yield return OpenSquareToken;
            foreach (SyntaxNode child in Elements.GetWithSeparators())
                yield return child;
            yield return CloseSquareToken;
        }
    }
    partial class AssignmentStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
            yield return AssignmentToken;
            yield return Expression;
        }
    }
    partial class BinaryExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }
    }
    partial class BlockStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenBraceToken;
            foreach (var child in Statements)
                yield return child;
            yield return CloseBraceToken;
        }
    }
    partial class BreakStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
        }
    }
    partial class CallExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            if (HasGenericParameter)
            {
                yield return LessThanToken;
                yield return GenericTypeClause;
                yield return GreaterThanToken;
            }
            yield return OpenParenthesisToken;
            foreach (SyntaxNode child in Arguments.GetWithSeparators())
                yield return child;
            yield return CloseParenthesisToken;
        }
    }
    partial class CompilationUnitSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (var child in Members)
                yield return child;
            yield return EndOfFileToken;
        }
    }
    partial class ContinueStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Keyword;
        }
    }
    //partial class DoWhileStatementSyntax
    //{
    //    public override IEnumerable<SyntaxNode> GetChildren()
    //    {
    //        yield return DoKeyword;
    //        yield return Body;
    //        yield return WhileKeyword;
    //        yield return Condition;
    //    }
    //}
    partial class ElseClauseSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ElseKeyword;
            yield return ElseStatement;
        }
    }
    partial class ExpressionStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Expression;
        }
    }
    //partial class ForStatementSyntax
    //{
    //    public override IEnumerable<SyntaxNode> GetChildren()
    //    {
    //        yield return Keyword;
    //        yield return Identifier;
    //        yield return EqualsToken;
    //        yield return LowerBound;
    //        yield return ToKeyword;
    //        yield return UpperBound;
    //        yield return Body;
    //    }
    //}
    partial class FunctionDeclarationSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return TypeClause;
            yield return Identifier;
            yield return OpenParenthesisToken;
            foreach (var child in Parameters.GetWithSeparators())
                yield return child;
            yield return CloseParenthesisToken;
            yield return Body;
        }
    }
    partial class GlobalStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Statement;
        }
    }
    partial class IfStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IfKeyword;
            yield return Condition;
            yield return ThenStatement;
            if (ElseClause is not null)
                yield return ElseClause;
        }
    }
    partial class LiteralExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LiteralToken;
        }
    }
    partial class NameExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return IdentifierToken;
        }
    }
    partial class ParameterSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Identifier;
            yield return Type;
        }
    }
    partial class ParenthesizedExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenthesisToken;
            yield return Expression;
            yield return CloseParenthesisToken;
        }
    }
    //partial class ReturnStatementSyntax
    //{
    //    public override IEnumerable<SyntaxNode> GetChildren()
    //    {
    //        yield return ReturnKeyword;
    //        if (Expression is not null)
    //            yield return Expression;
    //    }
    //}
    partial class TypeClauseSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return TypeToken;
            if (HasGenericParameter)
            {
                yield return LessToken;
                yield return InnerType;
                yield return GreaterToken;
            }
        }
    }
    partial class UnaryExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OperatorToken;
            yield return Operand;
        }
    }
    partial class VariableDeclarationSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            foreach (SyntaxNode modifier in Modifiers)
                yield return modifier;
            yield return TypeClause;
            if (OptionalAssignment is null)
                yield return Identifier;
            else
                yield return OptionalAssignment;
        }
    }
    partial class WhileStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return WhileKeyword;
            yield return Condition;
            yield return Body;
        }
    }
    partial class SpecialBlockStatementSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return KeywordToken;
            yield return Block;
        }
    }
    partial class ConstructorExpressionSyntax
    {
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return KeywordToken;
            yield return OpenParenthesisToken;
            yield return ExpressionX;
            yield return Comma0Token;
            yield return ExpressionY;
            yield return Comma1Token;
            yield return ExpressionZ;
            yield return CloseParenthesisToken;
        }
    }
}
