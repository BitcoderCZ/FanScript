﻿// <copyright file="BoundProgram.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols.Functions;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding;

internal sealed class BoundProgram
{
	public BoundProgram(BoundProgram? previous, ImmutableArray<Diagnostic> diagnostics, FunctionSymbol? scriptFunction, ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functions, BoundAnalysisResult analysis, ImmutableDictionary<FunctionSymbol, ScopeWSpan> functionScopes)
	{
		Previous = previous;
		Diagnostics = diagnostics;
		ScriptFunction = scriptFunction;
		Functions = functions;
		Analysis = analysis;
		FunctionScopes = functionScopes;
	}

	public BoundProgram? Previous { get; }

	public ImmutableArray<Diagnostic> Diagnostics { get; }

	public FunctionSymbol? ScriptFunction { get; }

	public ImmutableDictionary<FunctionSymbol, BoundBlockStatement> Functions { get; }

	public ImmutableDictionary<FunctionSymbol, ScopeWSpan> FunctionScopes { get; }

	public BoundAnalysisResult Analysis { get; }
}
