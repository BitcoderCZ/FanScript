// <copyright file="TypeDocAttribute.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Attributes;

public sealed class TypeDocAttribute : DocumentationAttribute
{
	public TypeDocAttribute()
		: base()
	{
	}

	public string? HowToCreate { get; set; }
}
