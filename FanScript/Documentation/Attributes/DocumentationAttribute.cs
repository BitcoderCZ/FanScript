// <copyright file="DocumentationAttribute.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true, Inherited = true)]
public abstract class DocumentationAttribute : Attribute
{
	protected DocumentationAttribute()
	{
	}

	public string? NameOverwrite { get; set; }

	public string? Info { get; set; }

	public string[]? Remarks { get; set; }

	public string? Examples { get; set; }

	public string[]? Related { get; set; }
}
