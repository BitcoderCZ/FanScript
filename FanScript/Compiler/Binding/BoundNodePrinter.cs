using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using MathUtils.Vectors;
using System.CodeDom.Compiler;

namespace FanScript.Compiler.Binding
{
    internal static class BoundNodePrinter
    {
        public static void WriteTo(this BoundNode node, TextWriter writer)
        {
            if (writer is IndentedTextWriter iw)
                WriteTo(node, iw);
            else
                WriteTo(node, new IndentedTextWriter(writer));
        }

        public static void WriteTo(this BoundNode node, IndentedTextWriter writer)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    WriteBlockStatement((BoundBlockStatement)node, writer);
                    break;
                case BoundNodeKind.EventStatement:
                    WriteEventStatement((BoundEventStatement)node, writer);
                    break;
                case BoundNodeKind.NopStatement:
                    WriteNopStatement((BoundNopStatement)node, writer);
                    break;
                case BoundNodeKind.PostfixStatement:
                    WritePostfixStatement((BoundPostfixStatement)node, writer);
                    break;
                case BoundNodeKind.PrefixStatement:
                    WritePrefixStatement((BoundPrefixStatement)node, writer);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    WriteVariableDeclaration((BoundVariableDeclarationStatement)node, writer);
                    break;
                case BoundNodeKind.AssignmentStatement:
                    WriteAssignmentStatement((BoundAssignmentStatement)node, writer);
                    break;
                case BoundNodeKind.CompoundAssignmentStatement:
                    WriteCompoundAssignmentStatement((BoundCompoundAssignmentStatement)node, writer);
                    break;
                case BoundNodeKind.IfStatement:
                    WriteIfStatement((BoundIfStatement)node, writer);
                    break;
                case BoundNodeKind.WhileStatement:
                    WriteWhileStatement((BoundWhileStatement)node, writer);
                    break;
                case BoundNodeKind.DoWhileStatement:
                    WriteDoWhileStatement((BoundDoWhileStatement)node, writer);
                    break;
                //case BoundNodeKind.ForStatement:
                //    WriteForStatement((BoundForStatement)node, writer);
                //    break;
                case BoundNodeKind.LabelStatement:
                    WriteLabelStatement((BoundLabelStatement)node, writer);
                    break;
                case BoundNodeKind.GotoStatement:
                    WriteGotoStatement((BoundGotoStatement)node, writer);
                    break;
                case BoundNodeKind.RollbackGotoStatement:
                    WriteRollbackGotoStatement((BoundRollbackGotoStatement)node, writer);
                    break;
                case BoundNodeKind.ConditionalGotoStatement:
                    WriteConditionalGotoStatement((BoundConditionalGotoStatement)node, writer);
                    break;
                case BoundNodeKind.ReturnStatement:
                    WriteReturnStatement((BoundReturnStatement)node, writer);
                    break;
                case BoundNodeKind.EmitterHint:
                    WriteEmitterHint((BoundEmitterHint)node, writer);
                    break;
                case BoundNodeKind.CallStatement:
                    WriteCallStatement((BoundCallStatement)node, writer);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    WriteExpressionStatement((BoundExpressionStatement)node, writer);
                    break;
                case BoundNodeKind.ErrorExpression:
                    WriteErrorExpression((BoundErrorExpression)node, writer);
                    break;
                case BoundNodeKind.LiteralExpression:
                    WriteLiteralExpression((BoundLiteralExpression)node, writer);
                    break;
                case BoundNodeKind.VariableExpression:
                    WriteVariableExpression((BoundVariableExpression)node, writer);
                    break;
                case BoundNodeKind.UnaryExpression:
                    WriteUnaryExpression((BoundUnaryExpression)node, writer);
                    break;
                case BoundNodeKind.BinaryExpression:
                    WriteBinaryExpression((BoundBinaryExpression)node, writer);
                    break;
                case BoundNodeKind.CallExpression:
                    WriteCallExpression((BoundCallExpression)node, writer);
                    break;
                case BoundNodeKind.ConversionExpression:
                    WriteConversionExpression((BoundConversionExpression)node, writer);
                    break;
                case BoundNodeKind.ConstructorExpression:
                    WriteConstructorExpression((BoundConstructorExpression)node, writer);
                    break;
                case BoundNodeKind.PostfixExpression:
                    WritePostfixExpression((BoundPostfixExpression)node, writer);
                    break;
                case BoundNodeKind.PrefixExpression:
                    WritePrefixExpression((BoundPrefixExpression)node, writer);
                    break;
                case BoundNodeKind.ArraySegmentExpression:
                    WriteArraySegmentExpression((BoundArraySegmentExpression)node, writer);
                    break;
                case BoundNodeKind.AssignmentExpression:
                    WriteAssignmentExpression((BoundAssignmentExpression)node, writer);
                    break;
                case BoundNodeKind.CompoundAssignmentExpression:
                    WriteCompoundAssignmentExpression((BoundCompoundAssignmentExpression)node, writer);
                    break;
                case BoundNodeKind.EventCondition:
                    WriteEventCondition((BoundEventCondition)node, writer);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private static void WriteNestedStatement(this IndentedTextWriter writer, BoundStatement node)
        {
            bool needsIndentation = node is not BoundBlockStatement;

            if (needsIndentation)
                writer.Indent++;

            node.WriteTo(writer);

            if (needsIndentation)
                writer.Indent--;
        }

        private static void WriteNestedExpression(this IndentedTextWriter writer, int parentPrecedence, BoundExpression expression)
        {
            if (expression is BoundUnaryExpression unary)
                writer.WriteNestedExpression(parentPrecedence, SyntaxFacts.GetUnaryOperatorPrecedence(unary.Op.SyntaxKind), unary);
            else if (expression is BoundBinaryExpression binary)
                writer.WriteNestedExpression(parentPrecedence, SyntaxFacts.GetBinaryOperatorPrecedence(binary.Op.SyntaxKind), binary);
            else
                expression.WriteTo(writer);
        }

        private static void WriteNestedExpression(this IndentedTextWriter writer, int parentPrecedence, int currentPrecedence, BoundExpression expression)
        {
            var needsParenthesis = parentPrecedence >= currentPrecedence;

            if (needsParenthesis)
                writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);

            expression.WriteTo(writer);

            if (needsParenthesis)
                writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
        }

        private static void WriteBlockStatement(BoundBlockStatement node, IndentedTextWriter writer)
        {
            writer.WritePunctuation(SyntaxKind.OpenBraceToken);
            writer.WriteLine();
            writer.Indent++;

            foreach (var s in node.Statements)
                s.WriteTo(writer);

            writer.Indent--;
            writer.WritePunctuation(SyntaxKind.CloseBraceToken);
            writer.WriteLine();
        }

        private static void WriteEventStatement(BoundEventStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.KeywordOn);
            writer.WriteSpace();
            writer.WriteIdentifier(node.Type.ToString());
            if (node.ArgumentClause is not null)
                WriteArgumentClause(node.ArgumentClause, writer);
            writer.WriteLine();
            WriteBlockStatement(node.Block, writer);
        }

        private static void WriteNopStatement(BoundNopStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("nop");
            writer.WriteLine();
        }

        private static void WritePostfixStatement(BoundPostfixStatement node, IndentedTextWriter writer)
        {
            WriteVariable(node.Variable, writer);
            writer.WritePunctuation(node.PostfixKind.ToSyntaxString());
            writer.WriteLine();
        }

        private static void WritePrefixStatement(BoundPrefixStatement node, IndentedTextWriter writer)
        {
            writer.WritePunctuation(node.PrefixKind.ToSyntaxString());
            WriteVariable(node.Variable, writer);
            writer.WriteLine();
        }

        private static void WriteVariableDeclaration(BoundVariableDeclarationStatement node, IndentedTextWriter writer)
        {
            if (node.Variable.Modifiers != 0)
            {
                writer.WriteModifiers(node.Variable.Modifiers);
                writer.WriteSpace();
            }
            node.Variable.Type.WriteTo(writer);
            writer.WriteSpace();

            if (node.OptionalAssignment is null || node.OptionalAssignment.Kind == BoundNodeKind.BlockStatement)
            {
                WriteVariable(node.Variable, writer);
                writer.WriteLine();
            }

            if (node.OptionalAssignment is not null)
                node.OptionalAssignment.WriteTo(writer);
        }

        private static void WriteAssignmentStatement(BoundAssignmentStatement node, IndentedTextWriter writer)
        {
            WriteVariable(node.Variable, writer);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.Expression.WriteTo(writer);

            writer.WriteLine();
        }

        private static void WriteCompoundAssignmentStatement(BoundCompoundAssignmentStatement node, IndentedTextWriter writer)
        {
            WriteVariable(node.Variable, writer);
            writer.WriteSpace();
            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.Expression.WriteTo(writer);

            writer.WriteLine();
        }

        private static void WriteIfStatement(BoundIfStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.KeywordIf);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStatement(node.ThenStatement);

            if (node.ElseStatement is not null)
            {
                writer.WriteKeyword(SyntaxKind.KeywordElse);
                if (node.ElseStatement is BoundIfStatement) // "else if"
                {
                    writer.WriteSpace();
                    node.ElseStatement.WriteTo(writer);
                }
                else
                {
                    writer.WriteLine();
                    writer.WriteNestedStatement(node.ElseStatement);
                }
            }
        }

        private static void WriteWhileStatement(BoundWhileStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.KeywordWhile);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStatement(node.Body);
        }

        private static void WriteDoWhileStatement(BoundDoWhileStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.KeywordDo);
            writer.WriteLine();
            writer.WriteNestedStatement(node.Body);
            writer.WriteKeyword(SyntaxKind.KeywordWhile);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
        }

        //private static void WriteForStatement(BoundForStatement node, IndentedTextWriter writer)
        //{
        //    writer.WriteKeyword(SyntaxKind.ForKeyword);
        //    writer.WriteSpace();
        //    writer.WriteIdentifier(node.Variable.Name);
        //    writer.WriteSpace();
        //    writer.WritePunctuation(SyntaxKind.EqualsToken);
        //    writer.WriteSpace();
        //    node.LowerBound.WriteTo(writer);
        //    writer.WriteSpace();
        //    writer.WriteKeyword(SyntaxKind.ToKeyword);
        //    writer.WriteSpace();
        //    node.UpperBound.WriteTo(writer);
        //    writer.WriteLine();
        //    writer.WriteNestedStatement(node.Body);
        //}

        private static void WriteLabelStatement(BoundLabelStatement node, IndentedTextWriter writer)
        {
            bool unindent = writer.Indent > 0;
            if (unindent)
                writer.Indent--;

            writer.WritePunctuation(node.Label.Name);
            writer.WritePunctuation(SyntaxKind.ColonToken);
            writer.WriteLine();

            if (unindent)
                writer.Indent++;
        }

        private static void WriteGotoStatement(BoundGotoStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("goto"); // There is no SyntaxKind for goto
            writer.WriteSpace();
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteLine();
        }

        private static void WriteRollbackGotoStatement(BoundRollbackGotoStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("goto"); // There is no SyntaxKind for goto
            writer.WriteSpace();
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteSpace();
            writer.WriteKeyword("[rollback]");
            writer.WriteLine();
        }

        private static void WriteConditionalGotoStatement(BoundConditionalGotoStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("goto"); // There is no SyntaxKind for goto
            writer.WriteSpace();
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteSpace();
            writer.WriteKeyword(node.JumpIfTrue ? "if" : "unless");
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteReturnStatement(BoundReturnStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.KeywordReturn);
            if (node.Expression is not null)
            {
                writer.WriteSpace();
                node.Expression.WriteTo(writer);
            }
            writer.WriteLine();
        }

        private static void WriteEmitterHint(BoundEmitterHint node, IndentedTextWriter writer)
        {
            switch (node.Hint)
            {
                case BoundEmitterHint.HintKind.StatementBlockEnd:
                    //writer.Indent--;
                    break;
            }

            writer.WritePunctuation("<" + node.Hint + ">");
            writer.WriteLine();

            switch (node.Hint)
            {
                case BoundEmitterHint.HintKind.StatementBlockStart:
                    //writer.Indent++;
                    break;
            }
        }

        private static void WriteCallStatement(BoundCallStatement node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Function.Name);

            if (node.Function.IsGeneric)
            {
                writer.WritePunctuation(SyntaxKind.LessToken);
                node.GenericType?.WriteTo(writer);
                writer.WritePunctuation(SyntaxKind.GreaterToken);
            }

            WriteArgumentClause(node.ArgumentClause, writer);

            writer.WriteLine();
        }

        private static void WriteExpressionStatement(BoundExpressionStatement node, IndentedTextWriter writer)
        {
            if (node.Expression.Kind == BoundNodeKind.NopExpression)
            {
                writer.WriteKeyword("nop");
                writer.WriteLine();
                return;
            }

            node.Expression.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteErrorExpression(BoundErrorExpression node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("?");
        }

        private static void WriteLiteralExpression(BoundLiteralExpression node, IndentedTextWriter writer)
        {
            if (node.Type == TypeSymbol.Null)
                writer.WriteKeyword(SyntaxKind.KeywordNull);
            else if (node.Type == TypeSymbol.Bool)
                writer.WriteKeyword((bool)node.Value! ? SyntaxKind.KeywordTrue : SyntaxKind.KeywordFalse);
            else if (node.Type == TypeSymbol.Float)
                writer.WriteNumber((float)node.Value!);
            else if (node.Type == TypeSymbol.Vector3 || node.Type == TypeSymbol.Rotation)
            {
                Vector3F val;
                if (node.Type == TypeSymbol.Rotation)
                    val = ((Rotation)node.Value!).Value;
                else
                    val = (Vector3F)node.Value!;

                node.Type.WriteTo(writer);
                writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);
                writer.WriteNumber(val.X);
                writer.WritePunctuation(SyntaxKind.CommaToken);
                writer.WriteSpace();
                writer.WriteNumber(val.Y);
                writer.WritePunctuation(SyntaxKind.CommaToken);
                writer.WriteSpace();
                writer.WriteNumber(val.Z);
                writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
            }
            else if (node.Type == TypeSymbol.String)
                writer.WriteString("\"" +
                    ((string)node.Value!)
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                    + "\"");
            else
                throw new Exception($"Unexpected type {node.Type}");
        }

        private static void WriteVariableExpression(BoundVariableExpression node, IndentedTextWriter writer)
            => WriteVariable(node.Variable, writer);

        private static void WriteUnaryExpression(BoundUnaryExpression node, IndentedTextWriter writer)
        {
            int precedence = SyntaxFacts.GetUnaryOperatorPrecedence(node.Op.SyntaxKind);

            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WriteNestedExpression(precedence, node.Operand);
        }

        private static void WriteBinaryExpression(BoundBinaryExpression node, IndentedTextWriter writer)
        {
            int precedence = SyntaxFacts.GetBinaryOperatorPrecedence(node.Op.SyntaxKind);

            writer.WriteNestedExpression(precedence, node.Left);
            writer.WriteSpace();
            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WriteSpace();
            writer.WriteNestedExpression(precedence, node.Right);
        }

        private static void WriteCallExpression(BoundCallExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Function.Name);

            if (node.Function.IsGeneric)
            {
                writer.WritePunctuation(SyntaxKind.LessToken);
                node.GenericType?.WriteTo(writer);
                writer.WritePunctuation(SyntaxKind.GreaterToken);
            }

            WriteArgumentClause(node.ArgumentClause, writer);
        }

        private static void WriteConversionExpression(BoundConversionExpression node, IndentedTextWriter writer)
        {
            if (node.Type.NonGenericEquals(TypeSymbol.Array))
                node.Expression.WriteTo(writer); // arraySegment to array
            else
            {
                writer.WriteIdentifier(node.Type.Name);
                writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);
                node.Expression.WriteTo(writer);
                writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
            }
        }

        private static void WriteConstructorExpression(BoundConstructorExpression node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Type.Name);
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);
            node.ExpressionX.WriteTo(writer);
            writer.WritePunctuation(SyntaxKind.CommaToken);
            writer.WriteSpace();
            node.ExpressionY.WriteTo(writer);
            writer.WritePunctuation(SyntaxKind.CommaToken);
            writer.WriteSpace();
            node.ExpressionZ.WriteTo(writer);
            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
        }

        private static void WritePostfixExpression(BoundPostfixExpression node, IndentedTextWriter writer)
        {
            WriteVariable(node.Variable, writer);
            writer.WritePunctuation(node.PostfixKind.ToSyntaxString());
        }

        private static void WritePrefixExpression(BoundPrefixExpression node, IndentedTextWriter writer)
        {
            writer.WritePunctuation(node.PrefixKind.ToSyntaxString());
            WriteVariable(node.Variable, writer);
        }

        private static void WriteArraySegmentExpression(BoundArraySegmentExpression node, IndentedTextWriter writer)
        {
            writer.WritePunctuation(SyntaxKind.OpenSquareToken);

            bool isFirst = true;
            foreach (var element in node.Elements)
            {
                if (isFirst)
                    isFirst = false;
                else
                {
                    writer.WritePunctuation(SyntaxKind.CommaToken);
                    writer.WriteSpace();
                }

                element.WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.CloseSquareToken);
        }

        private static void WriteAssignmentExpression(BoundAssignmentExpression node, IndentedTextWriter writer)
        {
            WriteVariable(node.Variable, writer);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.Expression.WriteTo(writer);
        }

        private static void WriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node, IndentedTextWriter writer)
        {
            WriteVariable(node.Variable, writer);
            writer.WriteSpace();
            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.Expression.WriteTo(writer);
        }

        private static void WriteEventCondition(BoundEventCondition node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.EventType.ToString());
            if (node.ArgumentClause is not null)
                WriteArgumentClause(node.ArgumentClause, writer);
        }

        #region Helper functions
        private static void WriteArgumentClause(BoundArgumentClause node, IndentedTextWriter writer)
        {
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);

            if (node.Arguments.Length != 0)
            {
                bool isFirst = true;
                foreach (var (argument, modifiers) in node.Arguments.Zip(node.ArgModifiers))
                {
                    if (isFirst)
                        isFirst = false;
                    else
                    {
                        writer.WritePunctuation(SyntaxKind.CommaToken);
                        writer.WriteSpace();
                    }

                    writer.WriteModifiers(modifiers);
                    if (modifiers != 0)
                        writer.WriteSpace();
                    argument.WriteTo(writer);
                }
            }

            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
        }

        private static void WriteVariable(VariableSymbol variable, IndentedTextWriter writer)
        {
            switch (variable)
            {
                case PropertySymbol propertySymbol:
                    {
                        propertySymbol.Expression.WriteTo(writer);
                        writer.WritePunctuation(SyntaxKind.DotToken);
                        goto default;
                    }
                default:
                    writer.WriteIdentifier(variable.ResultName);
                    break;
            }
        }
        #endregion
    }
}
