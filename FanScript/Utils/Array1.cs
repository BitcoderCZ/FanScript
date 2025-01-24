// <copyright file="Array1.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;

namespace FanScript.Utils;

[InlineArray(1)]
internal struct Array1<T>
{
#pragma warning disable IDE0044 // Add readonly modifier
	private T _element0;
#pragma warning restore IDE0044
}
