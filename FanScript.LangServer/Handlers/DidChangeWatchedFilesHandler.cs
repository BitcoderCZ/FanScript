﻿// <copyright file="DidChangeWatchedFilesHandler.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using System.Threading;
using System.Threading.Tasks;

namespace FanScript.LangServer.Handlers;

internal class DidChangeWatchedFilesHandler : IDidChangeWatchedFilesHandler
{
	public Task<Unit> Handle(DidChangeWatchedFilesParams request, CancellationToken cancellationToken) => Unit.Task;

	public DidChangeWatchedFilesRegistrationOptions GetRegistrationOptions(DidChangeWatchedFilesCapability capability, ClientCapabilities clientCapabilities) => new DidChangeWatchedFilesRegistrationOptions();
}
