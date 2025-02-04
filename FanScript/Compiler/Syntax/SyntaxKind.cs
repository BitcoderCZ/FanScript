﻿// <copyright file="SyntaxKind.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Compiler.Syntax;

public enum SyntaxKind : byte
{
	BadToken,
	EndOfFileToken,
	PlusToken,
	PlusPlusToken,
	PlusEqualsToken,
	MinusToken,
	MinusMinusToken,
	MinusEqualsToken,
	StarToken,
	StarEqualsToken,
	SlashToken,
	SlashEqualsToken,
	PercentToken,
	PercentEqualsToken,
	OpenParenthesisToken,
	CloseParenthesisToken,
	OpenSquareToken,
	CloseSquareToken,
	OpenBraceToken,
	CloseBraceToken,
	ColonToken,
	SemicolonToken,
	CommaToken,
	HashtagToken,
	DotToken,
	AmpersandAmpersandToken,
	PipePipeToken,
	EqualsToken,
	EqualsEqualsToken,
	BangToken,
	BangEqualsToken,
	LessToken,
	LessOrEqualsToken,
	GreaterToken,
	GreaterOrEqualsToken,
	IdentifierToken,

	CompilationUnit,
	GlobalStatement,
	BlockStatement,
	EventStatement,
	PostfixStatement,
	PrefixStatement,
	ExpressionStatement,
	IfStatement,
	WhileStatement,
	DoWhileStatement,
	BreakStatement,
	ContinueStatement,
	VariableDeclarationStatement,
	AssignmentStatement,
	ReturnStatement,
	BuildCommandStatement,
	CallStatement,

	UnaryExpression,
	BinaryExpression,
	ParenthesizedExpression,
	LiteralExpression,
	CallExpression,
	NameExpression,
	ConstructorExpression,
	PostfixExpression,
	PrefixExpression,
	PropertyExpression,
	VariableDeclarationExpression,
	ArraySegmentExpression,
	AssignmentExpression,
	ModifiersWExpressionSyntax,

	Parameter,
	TypeClause,
	ArgumentClause,
	ElseClause,
	ModifierClause,

	FloatToken,
	StringToken,

	KeywordTrue,
	KeywordFalse,
	KeywordFor,
	KeywordIf,
	KeywordElse,
	KeywordWhile,
	KeywordDo,
	KeywordBreak,
	KeywordContinue,
	KeywordOn,
	KeywordNull,

	SkippedTextTrivia,
	LineBreakTrivia,
	WhitespaceTrivia,
	SingleLineCommentTrivia,
	MultiLineCommentTrivia,

	FunctionDeclaration,

	KeywordFunction,
	KeywordReturn,

	ReadOnlyModifier,
	ConstantModifier,
	RefModifier,
	OutModifier,
	GlobalModifier,
	SavedModifier,
	InlineModifier,
}
