// <copyright file="AssertingEnumerator.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Syntax;

namespace FanScript.Tests.Syntax;

internal sealed class AssertingEnumerator : IDisposable
{
	private readonly IEnumerator<SyntaxNode> _enumerator;
	private bool _hasErrors;

	public AssertingEnumerator(SyntaxNode node)
	{
		_enumerator = Flatten(node).GetEnumerator();
	}

	public void Dispose()
	{
		if (!_hasErrors)
		{
			Assert.False(_enumerator.MoveNext());
		}

		_enumerator.Dispose();
	}

	public void AssertNode(SyntaxKind kind)
	{
		try
		{
			Assert.True(_enumerator.MoveNext());
			Assert.Equal(kind, _enumerator.Current.Kind);
			Assert.IsNotType<SyntaxToken>(_enumerator.Current);
		}
		catch when (MarkFailed())
		{
			throw;
		}
	}

	public void AssertToken(SyntaxKind kind, string text)
	{
		try
		{
			Assert.True(_enumerator.MoveNext());
			Assert.Equal(kind, _enumerator.Current.Kind);
			SyntaxToken token = Assert.IsType<SyntaxToken>(_enumerator.Current);
			Assert.Equal(text, token.Text);
		}
		catch when (MarkFailed())
		{
			throw;
		}
	}

	private static IEnumerable<SyntaxNode> Flatten(SyntaxNode node)
	{
		var stack = new Stack<SyntaxNode>();
		stack.Push(node);

		while (stack.Count > 0)
		{
			SyntaxNode n = stack.Pop();
			yield return n;

			foreach (var child in n.GetChildren().Reverse())
			{
				stack.Push(child);
			}
		}
	}

	private bool MarkFailed()
	{
		_hasErrors = true;
		return false;
	}
}
