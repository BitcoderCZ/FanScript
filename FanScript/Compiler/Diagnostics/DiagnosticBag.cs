﻿// <copyright file="DiagnosticBag.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing.Scripting;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System.Collections;

namespace FanScript.Compiler.Diagnostics;

public sealed class DiagnosticBag : IEnumerable<Diagnostic>
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void AddRange(IEnumerable<Diagnostic> diagnostics)
		=> _diagnostics.AddRange(diagnostics);

	public void ReportInvalidNumber(TextLocation location, string text)
		=> ReportError(location, $"The number {text} isn't a valid number.");

	public void ReportBadCharacter(TextLocation location, char character)
		=> ReportError(location, $"Bad character input: '{character}'.");

	public void ReportUnterminatedString(TextLocation location)
		=> ReportError(location, "Unterminated string literal.");

	public void ReportInvalidEscapeSequance(TextLocation location, string escapeSequance)
		=> ReportError(location, $"Invalid escape sequance: '{escapeSequance}'.");

	public void ReportUnterminatedMultiLineComment(TextLocation location)
		=> ReportError(location, "Unterminated multi-line comment.");

	public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind expectedKind)
		=> ReportError(location, $"Unexpected token <{actualKind}>, expected <{expectedKind}>.");

	public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind[] expectedKinds)
		=> ReportError(location, $"Unexpected token <{actualKind}>, expected one of <{string.Join(", ", expectedKinds)}>.");

	public void ReportUnexpectedIdentifier(TextLocation location, string actualText, string expectedText)
		=> ReportError(location, $"Unexpected token '{actualText}', expected '{expectedText}'.");

	public void ReportUnexpectedIdentifier(TextLocation location, string actualText, IEnumerable<string> expectedTexts)
		=> ReportError(location, $"Unexpected token '{actualText}', expected one of {string.Join(", ", expectedTexts.Select(text => "'" + text + "'").ToArray())}.");

	public void ReportUndefinedUnaryOperator(TextLocation location, string operatorText, TypeSymbol? operandType)
		=> ReportError(location, $"Unary operator '{operatorText}' is not defined for type '{operandType}'.");

	public void ReportUndefinedBinaryOperator(TextLocation location, string operatorText, TypeSymbol? leftType, TypeSymbol? rightType)
		=> ReportError(location, $"Binary operator '{operatorText}' is not defined for types '{leftType}' and '{rightType}'.");

	public void ReportUndefinedPostfixOperator(TextLocation location, string operatorText, TypeSymbol? operandType)
		=> ReportError(location, $"Postfix operator '{operatorText}' is not defined for type '{operandType}'.");

	public void ReportUndefinedPrefixOperator(TextLocation location, string operatorText, TypeSymbol? operandType)
		=> ReportError(location, $"Prefix operator '{operatorText}' is not defined for type '{operandType}'.");

	public void ReportParameterAlreadyDeclared(TextLocation location, string parameterName)
		=> ReportError(location, $"A parameter with the name '{parameterName}' already exists.");

	public void ReportUndefinedVariable(TextLocation location, string name)
		=> ReportError(location, $"Variable '{name}' doesn't exist.");

	public void ReportUndefinedType(TextLocation location, string name)
		=> ReportError(location, $"Type '{name}' doesn't exist.");

	public void ReportCannotConvert(TextLocation location, TypeSymbol? fromType, TypeSymbol? toType)
		=> ReportError(location, $"Cannot convert type '{fromType}' to '{toType}'.");

	public void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol? fromType, TypeSymbol? toType)
		=> ReportError(location, $"Cannot convert type '{fromType}' to '{toType}'. An explicit conversion exists (are you missing a cast?)");

	public void ReportSymbolAlreadyDeclared(TextLocation location, string name)
		=> ReportError(location, $"'{name}' is already declared.");

	public void ReportCannotAssignReadOnlyVariable(TextLocation location, string name)
		=> ReportError(location, $"Variable '{name}' is read-only and cannot be assigned to.");

	public void ReportUndefinedFunction(TextLocation location, string name)
		=> ReportError(location, $"Function '{name}' doesn't exist.");

	public void ReportNotAFunction(TextLocation location, string name)
		=> ReportError(location, $"'{name}' is not a function.");

	public void ReportWrongArgumentCount(TextLocation location, string type, string name, int expectedCount, int actualCount)
		=> ReportError(location, $"{type} '{name}' requires {expectedCount} arguments but was given {actualCount}.");

	public void ReportExpressionMustHaveValue(TextLocation location)
		=> ReportError(location, "Expression must have a value.");

	public void ReportAllPathsMustReturn(TextLocation location)
		=> ReportError(location, "Not all code paths return a value.");

	public void ReportInvalidExpressionStatement(TextLocation location)
		=> ReportError(location, $"Only call expressions can be used as a statement.");

	public void ReportOnlyOneFileCanHaveGlobalStatements(TextLocation location)
		=> ReportError(location, $"At most one file can have global statements.");

	public void ReportInvalidBreakOrContinue(TextLocation location, string text)
	  => ReportError(location, $"The keyword '{text}' can only be used inside of loops.");

	public void ReportInvalidKeywordInEvent(TextLocation location, string text)
	  => ReportError(location, $"The keyword '{text}' cannot be used inside of an event block.");

	public void ReportValueMustBeConstant(TextLocation location)
		=> ReportError(location, "Value must be constant.");

	public void ReportGenericTypeNotAllowed(TextLocation location)
		=> ReportError(location, "Generic type isn't allowed here.");

	public void ReportGenericTypeRecursion(TextLocation location)
		=> ReportError(location, "Type argument cannot be generic.");

	public void ReportNotAGenericType(TextLocation location)
		=> ReportError(location, "Non generic type cannot have a type argument.");

	public void ReportCannotInferGenericType(TextLocation location)
		=> ReportError(location, "Cannot infer type argument from usage.");

	public void ReportSpecificGenericTypeNotAllowed(TextLocation location, TypeSymbol genericType, IEnumerable<TypeSymbol> allowedGenericTypes)
		=> ReportError(location, $"Type argument type '{genericType}' isn't allowed, allowed types: <{string.Join(", ", allowedGenericTypes)}>.");

	public void ReportTypeMustHaveGenericParameter(TextLocation location)
		=> ReportError(location, "Type must have a type argument.");

	public void ReportNonGenericMethodTypeArguments(TextLocation location)
		=> ReportError(location, "Non-generic methods cannot be used with a type argument.");

	public void ReportVariableNameTooLong(TextLocation location, string name)
		=> ReportError(location, $"Variable name '{name}' is too long, maximum allowed length is {FancadeConstants.MaxVariableNameLength}");

	public void ReportEmptyArrayInitializer(TextLocation location)
		=> ReportError(location, $"Array initializer cannot be empty.");

	public void ReportNotAModifier(TextLocation location, string text)
		=> ReportError(location, $"'{text}' isn't a valid modifier.");

	public void ReportInvalidModifier(TextLocation location, Modifiers modifier, ModifierTarget usedTarget, IEnumerable<ModifierTarget> validTargets)
		=> ReportError(location, $"Modifier '{modifier.ToKind().GetText()}' was used on <{usedTarget}>, but it can only be used on <{string.Join(", ", validTargets)}>.");

	public void ReportDuplicateModifier(TextLocation location, Modifiers modifier)
		=> ReportError(location, $"Duplicate '{modifier.ToKind().GetText()}' modifier.");

	public void ReportInvalidModifierOnType(TextLocation location, Modifiers modifier, TypeSymbol type)
		=> ReportError(location, $"Modifier '{modifier.ToKind().GetText()}' isn't valid on a variable of type '{type}'.");

	public void ReportVariableNotInitialized(TextLocation location)
		=> ReportError(location, "A readonly/constant variable needs to be initialized.");

	public void ReportConflictingModifiers(TextLocation location, Modifiers modifier, Modifiers conflictingModifier)
		=> ReportError(location, $"Modifier '{conflictingModifier.ToKind().GetText()}' conflicts with modifier '{modifier.ToKind().GetText()}'.");

	public void ReportUnknownEvent(TextLocation location, string text)
		=> ReportError(location, $"Unknown event '{text}'.");

	public void ReportArgumentMustHaveModifier(TextLocation location, string name, Modifiers modifier)
		=> ReportError(location, $"Argument for paramater '{name}' must be passed with the '{modifier.ToKind().GetText()}' modifier.");

	public void ReportArgumentCannotHaveModifier(TextLocation location, string name, Modifiers modifier)
		=> ReportError(location, $"Argument for paramater '{name}' cannot be passed with the '{modifier.ToKind().GetText()}' modifier.");

	public void ReportByRefArgMustBeVariable(TextLocation location, Modifiers makesRefMod)
		=> ReportError(location, $"A {makesRefMod.ToKind().GetText()} argument must be an assignable variable.");

	public void ReportMustBeName(TextLocation location)
		=> ReportError(location, "Expression must be a name");

	public void ReportUndefinedProperty(TextLocation location, TypeSymbol type, string name)
		=> ReportError(location, $"Type '{type}' doesn't have a property '{name}'.");

	public void ReportCannotAssignReadOnlyProperty(TextLocation location, string name)
		=> ReportError(location, $"Property '{name}' is read-only and cannot be assigned to.");

	public void ReportSBMustHaveArguments(TextLocation location, string sbType)
		=> ReportError(location, $"Event '{sbType}' must have arguments.");

	public void ReportMissignRequiredModifiers(TextLocation location, Modifiers mod, IEnumerable<Modifiers> required)
		=> ReportError(location, $"Modifier '{mod.ToKind().GetText()}' requires that one of <{string.Join(", ", required.Select(reqMod => reqMod.ToKind().GetText()))}> is present.");

	public void ReportInvalidName(TextLocation location, string name)
		=> ReportError(location, $"Name '{name}' cannot be used.");

	public void ReportDiscardCannotBeUsed(TextLocation location)
		=> ReportError(location, "_ cannot be used as a variable.");

	public void ReportInvalidReturnWithValueInGlobalStatements(TextLocation location)
		=> ReportError(location, "The 'return' keyword cannot be followed by an expression in global statements.");

	public void ReportInvalidReturnExpression(TextLocation location, string functionName)
		=> ReportError(location, $"Since the function '{functionName}' does not return a value the 'return' keyword cannot be followed by an expression.");

	public void ReportMissingReturnExpression(TextLocation location, TypeSymbol returnType)
		=> ReportError(location, $"An expression of type '{returnType}' is expected.");

	public void ReportFeatureNotImplemented(TextLocation location, string feature)
		=> ReportError(location, $"Feature '{feature}' has not been implemented yet.");

	public void ReportUnknownBuildCommand(TextLocation location, string text)
		=> ReportError(location, $"Unknown build command '{text}'.");

	public void ReportCircularCall(TextLocation location, IEnumerable<FunctionSymbol> cycle)
		=> ReportError(location, $"Circular calls (recursion) aren't allowed ({string.Join(" -> ", cycle.Select(func => func.ToString()))}).");

	public void ReportTooManyInlineVariableUses(TextLocation location, string variableName)
		=> ReportError(location, $"Inline variable '{variableName}' has been used to many times (max is {FancadeConstants.MaxWireSplits}), either use it less times or remove the inline modifier.");

	public void ReportUnreachableCode(TextLocation location)
	  => ReportWarning(location, $"Unreachable code detected.");

	// TODO: this needs all the added statements/expressions (right?)
	public void ReportUnreachableCode(SyntaxNode node)
	{
		switch (node)
		{
			case BlockStatementSyntax blockStatement:
				StatementSyntax? firstStatement = blockStatement.Statements.FirstOrDefault();

				// Report just for non empty StockBlocks.
				if (firstStatement is not null)
				{
					ReportUnreachableCode(firstStatement);
				}

				return;
			case VariableDeclarationStatementSyntax variableDeclarationStatement:
				ReportUnreachableCode(variableDeclarationStatement.TypeClause.Location);
				return;
			case IfStatementSyntax ifStatement:
				ReportUnreachableCode(ifStatement.IfKeyword.Location);
				return;
			case WhileStatementSyntax whileStatement:
				ReportUnreachableCode(whileStatement.WhileKeyword.Location);
				return;
			case DoWhileStatementSyntax doWhileStatement:
				ReportUnreachableCode(doWhileStatement.DoKeyword.Location);
				return;

			//case SyntaxKind.ForStatement:
			//    ReportUnreachableCode(((ForStatementSyntax)node).Keyword.Location);
			//    return;
			case BreakStatementSyntax breakStatement:
				ReportUnreachableCode(breakStatement.Keyword.Location);
				return;
			case ContinueStatementSyntax continueStatement:
				ReportUnreachableCode(continueStatement.Keyword.Location);
				return;
			case ReturnStatementSyntax returnStatement:
				ReportUnreachableCode(returnStatement.ReturnKeyword.Location);
				return;
			case AssignmentStatementSyntax:
				ReportUnreachableCode(node.Location);
				return;
			case CallStatementSyntax callStatement:
				ReportUnreachableCode(callStatement.Identifier.Location);
				return;
			case ExpressionStatementSyntax expressionStatement:
				ExpressionSyntax expression = expressionStatement.Expression;
				ReportUnreachableCode(expression);
				return;
			case NameExpressionSyntax nameExpression:
				ReportUnreachableCode(nameExpression.IdentifierToken.Location);
				break;
			case CallExpressionSyntax callExpression:
				ReportUnreachableCode(callExpression.Identifier.Location);
				return;
			default:
				throw new UnexpectedSyntaxException(node);
		}
	}

	public void ReportOpeationNotSupportedOnBuilder(TextLocation location, BuilderUnsupportedOperation unsupportedOperation)
	{
		string msg = unsupportedOperation switch
		{
			BuilderUnsupportedOperation.ConnectToBlock => $"Current {nameof(BlockBuilder)} cannot connect object wires to blocks, you will have to connect it manually.",
			BuilderUnsupportedOperation.CreateCustomBlocks => $"Current {nameof(BlockBuilder)} cannot create custom StockBlocks.",
			_ => throw new UnknownEnumValueException<BuilderUnsupportedOperation>(unsupportedOperation),
		};

		ReportWarning(location, msg);
	}

	public void ReportFailedToDeclare(TextLocation location, string type, string name)
		=> ReportWarning(location, $"Failed to declare {type} '{name}'.");

	private void ReportError(TextLocation location, string message)
		=> _diagnostics.Add(Diagnostic.Error(location, message));

	private void ReportWarning(TextLocation location, string message)
		=> _diagnostics.Add(Diagnostic.Warning(location, message));
}