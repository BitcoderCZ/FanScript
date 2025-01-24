// <copyright file="ErrorCode.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace FanScript.Cli;

internal enum ErrorCode
{
	None,
	UnknownError,
	CompilationErrors = 10,
	FileNotFound = 20,
	InvalidBuildPos,
}
