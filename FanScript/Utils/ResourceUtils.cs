// <copyright file="ResourceUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.Reflection;

namespace FanScript.Utils;

internal static class ResourceUtils
{
	public static Stream OpenResource(string name)
	{
		Assembly assembly = Assembly.GetAssembly(typeof(ResourceUtils))!;

		Stream? stream = assembly.GetManifestResourceStream("FanScript." + name);

		return stream is null ? throw new FileNotFoundException($"Resource \"FanScript.{name}\" wasn't found.") : stream;
	}
}
