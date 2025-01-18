// <copyright file="BoundNode.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

internal abstract class BoundNode
{
	protected BoundNode(SyntaxNode syntax)
	{
		Syntax = syntax;
	}

	public abstract BoundNodeKind Kind { get; }

	public SyntaxNode Syntax { get; }

	public override string ToString()
	{
		using (var writer = new StringWriter())
		{
			this.WriteTo(writer);
			return writer.ToString();
		}
	}
}
