// <copyright file="FunctionSymbol.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Symbols.Functions;

public class FunctionSymbol : Symbol, IComparable<FunctionSymbol>
{
	private int? _hashCode;

	internal FunctionSymbol(Namespace @namespace, Modifiers modifiers, TypeSymbol type, string name, ImmutableArray<ParameterSymbol> parameters, FunctionDeclarationSyntax? declaration = null)
		: base(name)
	{
		Namespace = @namespace;
		Modifiers = modifiers;
		Type = type;
		Parameters = parameters;
		Declaration = declaration;
	}

	internal FunctionSymbol(Namespace @namespace, Modifiers modifiers, TypeSymbol type, string name, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<TypeSymbol>? allowedGenericTypes, FunctionDeclarationSyntax? declaration = null)
		: base(name)
	{
		Namespace = @namespace;
		Modifiers = modifiers;
		Type = type;
		Parameters = parameters;
		Declaration = declaration;

		IsGeneric = true;
		AllowedGenericTypes = allowedGenericTypes;
	}

	public override SymbolKind Kind => SymbolKind.Function;

	public Namespace Namespace { get; }

	public Modifiers Modifiers { get; }

	public TypeSymbol Type { get; }

	public ImmutableArray<ParameterSymbol> Parameters { get; }

	public short Id { get; init; } = -1;

	public FunctionDeclarationSyntax? Declaration { get; }

	public bool IsMethod { get; init; }

	[MemberNotNullWhen(true, nameof(AllowedGenericTypes))]
	public bool IsGeneric { get; }

	public ImmutableArray<TypeSymbol>? AllowedGenericTypes { get; }

	public string ToString(bool onlyParams)
	{
		if (!onlyParams)
		{
			return ToString();
		}

		using (var writer = new StringWriter())
		{
			WriteTo(writer, onlyParams, false);
			return writer.ToString();
		}
	}

	public int CompareTo(FunctionSymbol? other)
	{
		if (other is null)
		{
			return 1;
		}

		int nameComp = string.Compare(Name, other.Name, StringComparison.Ordinal);

		return nameComp != 0
			? nameComp
			: Parameters.Length != other.Parameters.Length ? Parameters.Length.CompareTo(other.Parameters.Length) : 0;
	}

	public override void WriteTo(TextWriter writer)
		=> WriteTo(writer, false, false);

	public void WriteTo(TextWriter writer, bool onlyParams, bool writeAsMethod)
	{
		if (!onlyParams)
		{
			if (Modifiers != 0)
			{
				writer.WriteModifiers(Modifiers);
				writer.WriteSpace();
			}

			writer.WriteWritable(Type);
			writer.WriteSpace();
			writer.WriteIdentifier(Name);
		}

		if (IsGeneric)
		{
			writer.WritePunctuation(SyntaxKind.LessToken);
			writer.WritePunctuation(SyntaxKind.GreaterToken);
		}

		writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);

		int startParam = (writeAsMethod && IsMethod) ? 1 : 0;
		for (int i = startParam; i < Parameters.Length; i++)
		{
			if (i > startParam)
			{
				writer.WritePunctuation(SyntaxKind.CommaToken);
				writer.WriteSpace();
			}

			writer.WriteWritable(Parameters[i]);
		}

		writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
	}

	public override int GetHashCode()
		=> _hashCode ??= HashCode.Combine(
			Name,
			Type,
			Parameters
				.Aggregate(default(HashCode), (hash, param) =>
				{
					hash.Add(param);
					return hash;
				})
				.ToHashCode());

	public override bool Equals(object? obj)
		=> obj is FunctionSymbol other && Name == other.Name && Type == other.Type && Parameters.SequenceEqual(other.Parameters);
}

internal sealed class FunctionFactory
{
	private short _lastId;

	public FunctionSymbol Create(Namespace @namespace, Modifiers modifiers, TypeSymbol type, string name, ImmutableArray<ParameterSymbol> parameters, FunctionDeclarationSyntax? declaration = null)
		=> new FunctionSymbol(@namespace, modifiers, type, name, parameters, declaration)
		{
			Id = _lastId++,
		};

	public FunctionSymbol CreateGeneric(Namespace @namespace, Modifiers modifiers, TypeSymbol type, string name, ImmutableArray<ParameterSymbol> parameters, ImmutableArray<TypeSymbol>? allowedGenericTypes, FunctionDeclarationSyntax? declaration = null)
		=> new FunctionSymbol(@namespace, modifiers, type, name, parameters, allowedGenericTypes, declaration)
		{
			Id = _lastId++,
		};
}
