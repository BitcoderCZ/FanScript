using FanScript.Compiler.Symbols;
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
                case BoundNodeKind.SpecialBlockStatement:
                    WriteSpecialBlockStatement((BoundSpecialBlockStatement)node, writer);
                    break;
                case BoundNodeKind.NopStatement:
                    WriteNopStatement((BoundNopStatement)node, writer);
                    break;
                case BoundNodeKind.VariableDeclaration:
                    WriteVariableDeclaration((BoundVariableDeclaration)node, writer);
                    break;
                case BoundNodeKind.AssignmentStatement:
                    WriteAssignmentStatement((BoundAssignmentStatement)node, writer);
                    break;
                case BoundNodeKind.CompoundAssignmentStatement:
                    WriteCompoundAssignmentStatement((BoundCompoundAssignmentStatement)node, writer);
                    break;
                case BoundNodeKind.ArrayInitializerStatement:
                    WriteArrayInitializerStatement((BoundArrayInitializerStatement)node, writer);
                    break;
                case BoundNodeKind.IfStatement:
                    WriteIfStatement((BoundIfStatement)node, writer);
                    break;
                case BoundNodeKind.WhileStatement:
                    WriteWhileStatement((BoundWhileStatement)node, writer);
                    break;
                //case BoundNodeKind.DoWhileStatement:
                //    WriteDoWhileStatement((BoundDoWhileStatement)node, writer);
                //    break;
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
                //case BoundNodeKind.ReturnStatement:
                //    WriteReturnStatement((BoundReturnStatement)node, writer);
                //    break;
                case BoundNodeKind.EmitterHint:
                    WriteEmitterHint((BoundEmitterHint)node, writer);
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
                case BoundNodeKind.SpecialBlockCondition:
                    WriteSpecialBlockCondition((BoundSpecialBlockCondition)node, writer);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private static void WriteNestedStatement(this IndentedTextWriter writer, BoundStatement node)
        {
            var needsIndentation = node is not BoundBlockStatement;

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

        private static void WriteSpecialBlockStatement(BoundSpecialBlockStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.KeywordOn);
            writer.WriteSpace();
            writer.WriteIdentifier(node.Type.ToString());
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);
            bool isFirst = true;
            foreach (BoundExpression argument in node.Arguments)
            {
                if (isFirst)
                    isFirst = false;
                else
                {
                    writer.WritePunctuation(SyntaxKind.CommaToken);
                    writer.WriteSpace();
                }

                argument.WriteTo(writer);
            }
            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
            writer.WriteLine();
            WriteBlockStatement(node.Block, writer);
        }

        private static void WriteNopStatement(BoundNopStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("nop");
            writer.WriteLine();
        }

        private static void WriteVariableDeclaration(BoundVariableDeclaration node, IndentedTextWriter writer)
        {
            if (node.Variable.Modifiers != 0)
            {
                writer.WriteModifiers(node.Variable.Modifiers);
                writer.WriteSpace();
            }
            writer.WriteType(node.Variable.Type);
            writer.WriteSpace();

            if (node.OptionalAssignment is not null)
                node.OptionalAssignment.WriteTo(writer);
            else
            {
                writer.WriteIdentifier(node.Variable.Name);
                writer.WriteLine();
            }
        }

        private static void WriteAssignmentStatement(BoundAssignmentStatement node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.Expression.WriteTo(writer);

            writer.WriteLine();
        }

        private static void WriteCompoundAssignmentStatement(BoundCompoundAssignmentStatement node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.Expression.WriteTo(writer);

            writer.WriteLine();
        }

        private static void WriteArrayInitializerStatement(BoundArrayInitializerStatement node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();

            writer.WritePunctuation(SyntaxKind.OpenSquareToken);
            for (int i = 0; i < node.Elements.Length - 1; i++)
            {
                node.Elements[i].WriteTo(writer);
                writer.WritePunctuation(SyntaxKind.CommaToken);
                writer.WriteSpace();
            }

            if (node.Elements.Length != 0)
                node.Elements[node.Elements.Length - 1].WriteTo(writer);

            writer.WritePunctuation(SyntaxKind.CloseSquareToken);

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

        //private static void WriteDoWhileStatement(BoundDoWhileStatement node, IndentedTextWriter writer)
        //{
        //    writer.WriteKeyword(SyntaxKind.DoKeyword);
        //    writer.WriteLine();
        //    writer.WriteNestedStatement(node.Body);
        //    writer.WriteKeyword(SyntaxKind.WhileKeyword);
        //    writer.WriteSpace();
        //    node.Condition.WriteTo(writer);
        //    writer.WriteLine();
        //}

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

        //private static void WriteReturnStatement(BoundReturnStatement node, IndentedTextWriter writer)
        //{
        //    writer.WriteKeyword(SyntaxKind.ReturnKeyword);
        //    if (node.Expression is not null)
        //    {
        //        writer.WriteSpace();
        //        node.Expression.WriteTo(writer);
        //    }
        //    writer.WriteLine();
        //}

        private static void WriteEmitterHint(BoundEmitterHint node, IndentedTextWriter writer)
        {
            switch (node.Hint)
            {
                case BoundEmitterHint.HintKind.StatementBlockEnd:
                    writer.Indent--;
                    break;
            }

            writer.WritePunctuation("<" + node.Hint + ">");
            writer.WriteLine();

            switch (node.Hint)
            {
                case BoundEmitterHint.HintKind.StatementBlockStart:
                    writer.Indent++;
                    break;
            }
        }

        private static void WriteExpressionStatement(BoundExpressionStatement node, IndentedTextWriter writer)
        {
            node.Expression.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteErrorExpression(BoundErrorExpression node, IndentedTextWriter writer)
        {
            writer.WriteKeyword("?");
        }

        private static void WriteLiteralExpression(BoundLiteralExpression node, IndentedTextWriter writer)
        {
            string value = node.Value.ToString()!;

            if (node.Type == TypeSymbol.Bool)
                writer.WriteKeyword((bool)node.Value ? SyntaxKind.KeywordTrue : SyntaxKind.KeywordFalse);
            else if (node.Type == TypeSymbol.Float)
                writer.WriteNumber(value);
            else if (node.Type == TypeSymbol.Vector3 || node.Type == TypeSymbol.Rotation)
            {
                Vector3F val;
                SyntaxKind keyword;
                if (node.Type == TypeSymbol.Rotation)
                {
                    val = ((Rotation)node.Value).Value;
                    keyword = SyntaxKind.KeywordRotation;
                }
                else
                {
                    val = (Vector3F)node.Value;
                    keyword = SyntaxKind.KeywordVector3;
                }

                writer.WriteKeyword(keyword);
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
            //else if (node.Type == TypeSymbol.String)
            //{
            //    value = "\"" + value.Replace("\"", "\"\"") + "\"";
            //    writer.WriteString(value);
            //}
            else
                throw new Exception($"Unexpected type {node.Type}");
        }

        private static void WriteVariableExpression(BoundVariableExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
        }

        private static void WriteUnaryExpression(BoundUnaryExpression node, IndentedTextWriter writer)
        {
            var precedence = SyntaxFacts.GetUnaryOperatorPrecedence(node.Op.SyntaxKind);

            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WriteNestedExpression(precedence, node.Operand);
        }

        private static void WriteBinaryExpression(BoundBinaryExpression node, IndentedTextWriter writer)
        {
            var precedence = SyntaxFacts.GetBinaryOperatorPrecedence(node.Op.SyntaxKind);

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
                writer.WriteType(node.GenericType);
                writer.WritePunctuation(SyntaxKind.GreaterToken);
            }

            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);

            bool isFirst = true;
            foreach (BoundExpression argument in node.Arguments)
            {
                if (isFirst)
                    isFirst = false;
                else
                {
                    writer.WritePunctuation(SyntaxKind.CommaToken);
                    writer.WriteSpace();
                }

                argument.WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
        }

        private static void WriteConversionExpression(BoundConversionExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Type?.Name);
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);
            node.Expression.WriteTo(writer);
            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
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

        private static void WriteSpecialBlockCondition(BoundSpecialBlockCondition node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.SBType.ToString());
        }
    }
}
