// <copyright file="EventDocAttribute.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Documentation.Attributes;

public sealed class EventDocAttribute : DocumentationAttribute
{
	public EventDocAttribute()
		: base()
	{
	}

	public string?[]? ParamInfos { get; set; }
}
