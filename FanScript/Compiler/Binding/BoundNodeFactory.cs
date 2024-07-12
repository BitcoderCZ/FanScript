using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics;

namespace FanScript.Compiler.Binding
{
    internal static class BoundNodeFactory
    {
        public static BoundBlockStatement Block(SyntaxNode syntax, params BoundStatement[] statements)
            => new BoundBlockStatement(syntax, ImmutableArray.Create(statements));

        public static BoundVariableDeclaration VariableDeclaration(SyntaxNode syntax, VariableSymbol symbol, BoundAssignmentStatement? optionalAssignment)
            => new BoundVariableDeclaration(syntax, symbol, optionalAssignment);

        public static BoundVariableDeclaration VariableDeclaration(SyntaxNode syntax, string name, BoundAssignmentStatement? optionalAssignment)
            => VariableDeclarationInternal(syntax, name, optionalAssignment, isReadOnly: false);

        public static BoundVariableDeclaration ConstantDeclaration(SyntaxNode syntax, string name, BoundAssignmentStatement? optionalAssignment)
            => VariableDeclarationInternal(syntax, name, optionalAssignment, isReadOnly: true);

        private static BoundVariableDeclaration VariableDeclarationInternal(SyntaxNode syntax, string name, BoundAssignmentStatement? optionalAssignment, bool isReadOnly)
        {
            throw new NotImplementedException();
            //var local = new LocalVariableSymbol(name, isReadOnly, initializer.Type, initializer.ConstantValue);
            //return new BoundVariableDeclaration(syntax, local, optionalAssignment);
        }

        public static BoundWhileStatement While(SyntaxNode syntax, BoundExpression condition, BoundStatement body, BoundLabel breakLabel, BoundLabel continueLabel)
            => new BoundWhileStatement(syntax, condition, body, breakLabel, continueLabel);

        public static BoundGotoStatement Goto(SyntaxNode syntax, BoundLabel label)
            => new BoundGotoStatement(syntax, label);

        public static BoundRollbackGotoStatement RollbackGoto(SyntaxNode syntax, BoundLabel label)
            => new BoundRollbackGotoStatement(syntax, label);

        public static BoundConditionalGotoStatement GotoTrue(SyntaxNode syntax, BoundLabel label, BoundExpression condition)
            => new BoundConditionalGotoStatement(syntax, label, condition, jumpIfTrue: true);

        public static BoundConditionalGotoStatement GotoFalse(SyntaxNode syntax, BoundLabel label, BoundExpression condition)
            => new BoundConditionalGotoStatement(syntax, label, condition, jumpIfTrue: false);

        public static BoundLabelStatement Label(SyntaxNode syntax, BoundLabel label)
            => new BoundLabelStatement(syntax, label);

        public static BoundNopStatement Nop(SyntaxNode syntax)
            => new BoundNopStatement(syntax);

        public static BoundAssignmentStatement Assignment(SyntaxNode syntax, VariableSymbol variable, BoundExpression expression)
            => new BoundAssignmentStatement(syntax, variable, expression);

        public static BoundBinaryExpression Binary(SyntaxNode syntax, BoundExpression left, SyntaxKind kind, BoundExpression right)
        {
            BoundBinaryOperator op = BoundBinaryOperator.Bind(kind, left.Type, right.Type)!;
            return Binary(syntax, left, op, right);
        }

        public static BoundBinaryExpression Binary(SyntaxNode syntax, BoundExpression left, BoundBinaryOperator op, BoundExpression right)
            => new BoundBinaryExpression(syntax, left, op, right);

        public static BoundBinaryExpression Add(SyntaxNode syntax, BoundExpression left, BoundExpression right)
            => Binary(syntax, left, SyntaxKind.PlusToken, right);

        public static BoundBinaryExpression LessOrEqual(SyntaxNode syntax, BoundExpression left, BoundExpression right)
            => Binary(syntax, left, SyntaxKind.LessOrEqualsToken, right);

        public static BoundStatement Increment(SyntaxNode syntax, BoundVariableExpression variable)
        {
            BoundBinaryExpression increment = Add(syntax, variable, Literal(syntax, 1));
            return new BoundAssignmentStatement(syntax, variable.Variable, increment);
        }

        public static BoundUnaryExpression Not(SyntaxNode syntax, BoundExpression condition)
        {
            Debug.Assert(condition.Type == TypeSymbol.Bool);

            BoundUnaryOperator? op = BoundUnaryOperator.Bind(SyntaxKind.BangToken, TypeSymbol.Bool);
            Debug.Assert(op is not null);
            return new BoundUnaryExpression(syntax, op, condition);
        }

        public static BoundVariableExpression Variable(SyntaxNode syntax, BoundVariableDeclaration variable)
            => Variable(syntax, variable.Variable);

        public static BoundVariableExpression Variable(SyntaxNode syntax, VariableSymbol variable)
            => new BoundVariableExpression(syntax, variable);

        public static BoundLiteralExpression Literal(SyntaxNode syntax, object literal)
        {
            Debug.Assert(literal is string || literal is bool || literal is float || literal is Vector3F || literal is Rotation);

            return new BoundLiteralExpression(syntax, literal);
        }
    }
}
