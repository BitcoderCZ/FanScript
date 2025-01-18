// <copyright file="BoundConstant.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib;
using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using MathUtils.Vectors;

namespace FanScript.Compiler.Binding;

internal sealed class BoundConstant
{
	public BoundConstant(object? value)
	{
		Value = value;
	}

	public object? Value { get; }

	public object GetValueOrDefault(TypeSymbol type)
	{
		if (Value is not null)
		{
			return Value;
		}

		if (type.IsGenericInstance)
		{
			if (type.NonGenericEquals(TypeSymbol.Array) ||
				type.NonGenericEquals(TypeSymbol.ArraySegment))
			{
				return GetDefault(type.InnerType);
			}
		}

		return GetDefault(type);

		static object GetDefault(TypeSymbol type)
		{
			return type == TypeSymbol.Bool
				? false
				: type == TypeSymbol.Float
				? 0f
				: type == TypeSymbol.String
				? string.Empty
				: type == TypeSymbol.Vector3
				? float3.Zero
				: type == TypeSymbol.Rotation ? (object)new Rotation(float3.Zero) : throw new UnexpectedSymbolException(type);
		}
	}
}
