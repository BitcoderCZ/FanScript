// <copyright file="OperatorDocAttribute.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Attributes;

public abstract class OperatorDocAttribute : DocumentationAttribute
{
	public OperatorDocAttribute()
		: base()
	{
	}

	public string?[]? CombinationInfos { get; set; }
}

public sealed class BinaryOperatorDocAttribute : OperatorDocAttribute
{
	public BinaryOperatorDocAttribute()
		: base()
	{
	}
}

public sealed class UnaryOperatorDocAttribute : OperatorDocAttribute
{
	public UnaryOperatorDocAttribute()
		: base()
	{
	}
}
