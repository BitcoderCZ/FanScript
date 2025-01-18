// <copyright file="BoundNodePrinter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib;
using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using MathUtils.Vectors;
using System.CodeDom.Compiler;

namespace FanScript.Compiler.Binding;

internal static class BoundNodePrinter
{
	public static void WriteTo(this BoundNode node, TextWriter writer)
	{
		if (writer is IndentedTextWriter iw)
		{
			WriteTo(node, iw);
		}
		else
		{
			WriteTo(node, new IndentedTextWriter(writer));
		}
	}

	public static void WriteTo(this BoundNode node, IndentedTextWriter writer)
	{
		switch (node)
		{
			case BoundBlockStatement blockStatement:
				WriteBlockStatement(blockStatement, writer);
				break;
			case BoundEventStatement eventStatement:
				WriteEventStatement(eventStatement, writer);
				break;
			case BoundNopStatement nopStatement:
				WriteNopStatement(nopStatement, writer);
				break;
			case BoundPostfixStatement postfixStatement:
				WritePostfixStatement(postfixStatement, writer);
				break;
			case BoundPrefixStatement prefixStatement:
				WritePrefixStatement(prefixStatement, writer);
				break;
			case BoundVariableDeclarationStatement variableDeclarationStatement:
				WriteVariableDeclaration(variableDeclarationStatement, writer);
				break;
			case BoundAssignmentStatement assignmentStatement:
				WriteAssignmentStatement(assignmentStatement, writer);
				break;
			case BoundCompoundAssignmentStatement compoundAssignmentStatement:
				WriteCompoundAssignmentStatement(compoundAssignmentStatement, writer);
				break;
			case BoundIfStatement ifStatement:
				WriteIfStatement(ifStatement, writer);
				break;
			case BoundWhileStatement whileStatement:
				WriteWhileStatement(whileStatement, writer);
				break;
			case BoundDoWhileStatement doWhileStatement:
				WriteDoWhileStatement(doWhileStatement, writer);
				break;

			//case BoundNodeKind.ForStatement:
			//    WriteForStatement((BoundForStatement)node, writer);
			//    break;
			case BoundLabelStatement labelStatement:
				WriteLabelStatement(labelStatement, writer);
				break;
			case BoundGotoStatement gotoStatement:
				WriteGotoStatement(gotoStatement, writer);
				break;
			case BoundEventGotoStatement eventGotoStatement:
				WriteEventGotoStatement(eventGotoStatement, writer);
				break;
			case BoundConditionalGotoStatement conditionalGotoStatement:
				WriteConditionalGotoStatement(conditionalGotoStatement, writer);
				break;
			case BoundReturnStatement returnStatement:
				WriteReturnStatement(returnStatement, writer);
				break;
			case BoundEmitterHintStatement emitterHintStatement:
				WriteEmitterHint(emitterHintStatement, writer);
				break;
			case BoundCallStatement callStatement:
				WriteCallStatement(callStatement, writer);
				break;
			case BoundExpressionStatement expressionStatement:
				WriteExpressionStatement(expressionStatement, writer);
				break;
			case BoundErrorExpression errorExpression:
				WriteErrorExpression(errorExpression, writer);
				break;
			case BoundLiteralExpression literalExpression:
				WriteLiteralExpression(literalExpression, writer);
				break;
			case BoundVariableExpression variableExpression:
				WriteVariableExpression(variableExpression, writer);
				break;
			case BoundUnaryExpression unaryExpression:
				WriteUnaryExpression(unaryExpression, writer);
				break;
			case BoundBinaryExpression binaryExpression:
				WriteBinaryExpression(binaryExpression, writer);
				break;
			case BoundCallExpression callExpression:
				WriteCallExpression(callExpression, writer);
				break;
			case BoundConversionExpression conversionExpression:
				WriteConversionExpression(conversionExpression, writer);
				break;
			case BoundConstructorExpression constructorExpression:
				WriteConstructorExpression(constructorExpression, writer);
				break;
			case BoundPostfixExpression postfixExpression:
				WritePostfixExpression(postfixExpression, writer);
				break;
			case BoundPrefixExpression prefixExpression:
				WritePrefixExpression(prefixExpression, writer);
				break;
			case BoundArraySegmentExpression arraySegmentExpression:
				WriteArraySegmentExpression(arraySegmentExpression, writer);
				break;
			case BoundAssignmentExpression assignmentExpression:
				WriteAssignmentExpression(assignmentExpression, writer);
				break;
			case BoundCompoundAssignmentExpression compoundAssignmentExpression:
				WriteCompoundAssignmentExpression(compoundAssignmentExpression, writer);
				break;
			default:
				throw new UnexpectedBoundNodeException(node);
		}
	}

	private static void WriteNestedStatement(this IndentedTextWriter writer, BoundStatement node)
	{
		bool needsIndentation = node is not BoundBlockStatement;

		if (needsIndentation)
		{
			writer.Indent++;
		}

		node.WriteTo(writer);

		if (needsIndentation)
		{
			writer.Indent--;
		}
	}

	private static void WriteNestedExpression(this IndentedTextWriter writer, int parentPrecedence, BoundExpression expression)
	{
		if (expression is BoundUnaryExpression unary)
		{
			writer.WriteNestedExpression(parentPrecedence, SyntaxFacts.GetUnaryOperatorPrecedence(unary.Op.SyntaxKind), unary);
		}
		else if (expression is BoundBinaryExpression binary)
		{
			writer.WriteNestedExpression(parentPrecedence, SyntaxFacts.GetBinaryOperatorPrecedence(binary.Op.SyntaxKind), binary);
		}
		else
		{
			expression.WriteTo(writer);
		}
	}

	private static void WriteNestedExpression(this IndentedTextWriter writer, int parentPrecedence, int currentPrecedence, BoundExpression expression)
	{
		bool needsParenthesis = parentPrecedence >= currentPrecedence;

		if (needsParenthesis)
		{
			writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);
		}

		expression.WriteTo(writer);

		if (needsParenthesis)
		{
			writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
		}
	}

	private static void WriteBlockStatement(BoundBlockStatement node, IndentedTextWriter writer)
	{
		writer.WritePunctuation(SyntaxKind.OpenBraceToken);
		writer.WriteLine();
		writer.Indent++;

		foreach (var s in node.Statements)
		{
			s.WriteTo(writer);
		}

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
		{
			WriteArgumentClause(node.ArgumentClause, writer);
		}

		writer.WriteLine();
		WriteBlockStatement(node.Block, writer);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Write... method parameters should be consistent")]
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

		node.OptionalAssignment?.WriteTo(writer);
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

			// "else if"
			if (node.ElseStatement is BoundIfStatement)
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

	/*private static void WriteForStatement(BoundForStatement node, IndentedTextWriter writer)
    {
        writer.WriteKeyword(SyntaxKind.ForKeyword);
        writer.WriteSpace();
        writer.WriteIdentifier(node.Variable.Name);
        writer.WriteSpace();
        writer.WritePunctuation(SyntaxKind.EqualsToken);
        writer.WriteSpace();
        node.LowerBound.WriteTo(writer);
        writer.WriteSpace();
        writer.WriteKeyword(SyntaxKind.ToKeyword);
        writer.WriteSpace();
        node.UpperBound.WriteTo(writer);
        writer.WriteLine();
        writer.WriteNestedStatement(node.Body);
    }*/

	private static void WriteLabelStatement(BoundLabelStatement node, IndentedTextWriter writer)
	{
		bool unindent = writer.Indent > 0;
		if (unindent)
		{
			writer.Indent--;
		}

		writer.WritePunctuation(node.Label.Name);
		writer.WritePunctuation(SyntaxKind.ColonToken);
		writer.WriteLine();

		if (unindent)
		{
			writer.Indent++;
		}
	}

	private static void WriteGotoStatement(BoundGotoStatement node, IndentedTextWriter writer)
	{
		writer.WriteKeyword("goto"); // There is no SyntaxKind for goto
		writer.WriteSpace();
		writer.WriteIdentifier(node.Label.Name);
		if (node.IsRollback)
		{
			writer.WriteSpace();
			writer.WriteKeyword("[rollback]");
		}

		writer.WriteLine();
	}

	private static void WriteEventGotoStatement(BoundEventGotoStatement node, IndentedTextWriter writer)
	{
		writer.WriteKeyword("goto"); // There is no SyntaxKind for goto
		writer.WriteSpace();
		writer.WriteIdentifier(node.Label.Name);
		writer.WriteSpace();
		writer.WriteKeyword("if");
		writer.WriteSpace();
		writer.WriteIdentifier(node.EventType.ToString());
		if (node.ArgumentClause is not null)
		{
			WriteArgumentClause(node.ArgumentClause, writer);
		}

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

	private static void WriteEmitterHint(BoundEmitterHintStatement node, IndentedTextWriter writer)
	{
		switch (node.Hint)
		{
			case BoundEmitterHintStatement.HintKind.StatementBlockEnd:
				//writer.Indent--;
				break;
		}

		writer.WritePunctuation("<" + node.Hint + ">");
		writer.WriteLine();

		switch (node.Hint)
		{
			case BoundEmitterHintStatement.HintKind.StatementBlockStart:
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

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Write... method parameters should be consistent")]
	private static void WriteErrorExpression(BoundErrorExpression node, IndentedTextWriter writer)
		=> writer.WriteKeyword("?");

	private static void WriteLiteralExpression(BoundLiteralExpression node, IndentedTextWriter writer)
	{
		if (node.Type == TypeSymbol.Null)
		{
			writer.WriteKeyword(SyntaxKind.KeywordNull);
		}
		else if (node.Type == TypeSymbol.Bool)
		{
			writer.WriteKeyword((bool)node.Value! ? SyntaxKind.KeywordTrue : SyntaxKind.KeywordFalse);
		}
		else if (node.Type == TypeSymbol.Float)
		{
			writer.WriteNumber((float)node.Value!);
		}
		else if (node.Type == TypeSymbol.Vector3 || node.Type == TypeSymbol.Rotation)
		{
			float3 val = node.Type == TypeSymbol.Rotation ? ((Rotation)node.Value!).Value : (float3)node.Value!;

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
		{
			writer.WriteString("\"" +
				((string)node.Value!)
					.Replace("\\", "\\\\")
					.Replace("\"", "\\\"")
				+ "\"");
		}
		else
		{
			throw new UnexpectedSymbolException(node.Type);
		}
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
		{
			node.Expression.WriteTo(writer); // arraySegment to array
		}
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
			{
				isFirst = false;
			}
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
				{
					isFirst = false;
				}
				else
				{
					writer.WritePunctuation(SyntaxKind.CommaToken);
					writer.WriteSpace();
				}

				writer.WriteModifiers(modifiers);
				if (modifiers != 0)
				{
					writer.WriteSpace();
				}

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
