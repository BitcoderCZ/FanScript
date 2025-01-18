using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System.Collections.Immutable;

namespace FanScript.Tests.Syntax;

public class LexerTests
{
	[Fact]
	public void Lexes_UnterminatedString()
	{
		string text = "\"text";
		ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text, out var diagnostics);

		SyntaxToken token = Assert.Single(tokens);

		Assert.Equal(SyntaxKind.StringToken, token.Kind);
		Assert.Equal(text, token.Text);

		Compiler.Diagnostics.Diagnostic diagnostic = Assert.Single(diagnostics);

		Assert.Equal(new TextSpan(0, 1), diagnostic.Location.Span);
		Assert.Equal("Unterminated string literal.", diagnostic.Message);
	}

	[Fact]
	public void Covers_AllTokens()
	{
		IEnumerable<SyntaxKind> tokenKinds = Enum.GetValues<SyntaxKind>()
							 .Where(k => k.IsToken());

		IEnumerable<SyntaxKind> testedTokenKinds = GetTokens()
			.Concat(GetSeparators())
			.Select(t => t.kind);

		var untestedTokenKinds = new SortedSet<SyntaxKind>(tokenKinds);

		untestedTokenKinds.Remove(SyntaxKind.BadToken);
		untestedTokenKinds.Remove(SyntaxKind.EndOfFileToken);
		untestedTokenKinds.ExceptWith(testedTokenKinds);

		Assert.Empty(untestedTokenKinds);
	}

	[Theory]
	[MemberData(nameof(GetTokensData))]
	public void Lexes_Token(SyntaxKind kind, string text)
	{
		ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text);

		SyntaxToken token = Assert.Single(tokens);

		Assert.Equal(kind, token.Kind);
		Assert.Equal(text, token.Text);
	}

	[Theory]
	[MemberData(nameof(GetSeparatorsData))]
	public void Lexes_Separator(SyntaxKind kind, string text)
	{
		ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text, includeEndOfFile: true);

		SyntaxToken token = Assert.Single(tokens);
		SyntaxTrivia trivia = Assert.Single(token.LeadingTrivia);

		Assert.Equal(kind, trivia.Kind);
		Assert.Equal(text, trivia.Text);
	}

	[Theory]
	[MemberData(nameof(GetTokenPairsData))]
	public void Lexes_TokenPairs(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)
	{
		string text = t1Text + t2Text;
		ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text);

		Assert.Equal(2, tokens.Length);
		Assert.Equal(t1Kind, tokens[0].Kind);
		Assert.Equal(t1Text, tokens[0].Text);
		Assert.Equal(t2Kind, tokens[1].Kind);
		Assert.Equal(t2Text, tokens[1].Text);
	}

	[Theory]
	[MemberData(nameof(GetTokenPairsWithSeparatorData))]
	public void TokenPairs_WithSeparators(SyntaxKind t1Kind, string t1Text, SyntaxKind separatorKind, string separatorText, SyntaxKind t2Kind, string t2Text)
	{
		string text = t1Text + separatorText + t2Text;
		ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text);

		Assert.Equal(2, tokens.Length);
		Assert.Equal(t1Kind, tokens[0].Kind);
		Assert.Equal(t1Text, tokens[0].Text);

		SyntaxTrivia separator = Assert.Single(tokens[0].TrailingTrivia);

		Assert.Equal(separatorKind, separator.Kind);
		Assert.Equal(separatorText, separator.Text);

		Assert.Equal(t2Kind, tokens[1].Kind);
		Assert.Equal(t2Text, tokens[1].Text);
	}

	[Theory]
	[InlineData("foo")]
	[InlineData("foo42")]
	[InlineData("foo_42")]
	[InlineData("_foo")]
	public void Lexes_Identifiers(string name)
	{
		ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(name);

		Assert.Single(tokens);

		SyntaxToken token = tokens[0];

		Assert.Equal(SyntaxKind.IdentifierToken, token.Kind);
		Assert.Equal(name, token.Text);
	}

	public static IEnumerable<object[]> GetTokensData()
	{
		foreach (var t in GetTokens())
			yield return [t.kind, t.text];
	}

	public static IEnumerable<object[]> GetSeparatorsData()
	{
		foreach (var t in GetSeparators())
			yield return [t.kind, t.text];
	}

	public static IEnumerable<object[]> GetTokenPairsData()
	{
		foreach (var t in GetTokenPairs())
			yield return [t.t1Kind, t.t1Text, t.t2Kind, t.t2Text];
	}

	public static IEnumerable<object[]> GetTokenPairsWithSeparatorData()
	{
		foreach (var t in GetTokenPairsWithSeparator())
			yield return [t.t1Kind, t.t1Text, t.separatorKind, t.separatorText, t.t2Kind, t.t2Text];
	}

	private static IEnumerable<(SyntaxKind kind, string text)> GetTokens()
	{
		var fixedTokens = Enum.GetValues<SyntaxKind>()
			.Select(k => (k, text: SyntaxFacts.GetText(k)))
			.Where(t => t.text is not null)
			.Cast<(SyntaxKind, string)>();

		var dynamicTokens = new[]
		{
			(SyntaxKind.FloatToken, "1"),
			(SyntaxKind.FloatToken, "123"),
			(SyntaxKind.IdentifierToken, "a"),
			(SyntaxKind.IdentifierToken, "abc"),
			(SyntaxKind.StringToken, "\"Test\""),
			(SyntaxKind.StringToken, "\"Te\\\"st\""),
		};

		return fixedTokens.Concat(dynamicTokens);
	}

	private static IEnumerable<(SyntaxKind kind, string text)> GetSeparators()
		=> [
			(SyntaxKind.WhitespaceTrivia, " "),
			(SyntaxKind.WhitespaceTrivia, "  "),
			(SyntaxKind.LineBreakTrivia, "\r"),
			(SyntaxKind.LineBreakTrivia, "\n"),
			(SyntaxKind.LineBreakTrivia, "\r\n"),
			(SyntaxKind.MultiLineCommentTrivia, "/**/"),
		];

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Seperate if statements are more readable in this case")]
	private static bool RequiresSeparator(SyntaxKind t1Kind, SyntaxKind t2Kind)
	{
		bool t1IsKeyword = t1Kind.IsKeyword() || t1Kind.IsModifier();
		bool t2IsKeyword = t2Kind.IsKeyword() || t2Kind.IsModifier();

		// aa
		if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.IdentifierToken)
			return true;

		// floatfloat
		if (t1IsKeyword && t2IsKeyword)
			return true;

		// floata
		if (t1IsKeyword && t2Kind == SyntaxKind.IdentifierToken)
			return true;

		// afloat
		if (t1Kind == SyntaxKind.IdentifierToken && t2IsKeyword)
			return true;

		// a1
		if (t1Kind == SyntaxKind.IdentifierToken && t2Kind == SyntaxKind.FloatToken)
			return true;

		if (t1Kind == SyntaxKind.FloatToken)
		{
			char? _fc = SyntaxFacts.GetText(t2Kind)?[0];
			if (_fc is char fc)
			{
				fc = char.ToLowerInvariant(fc);

				if ((fc >= 'a' && fc <= 'f') || fc == 'x')
					return true;
			}

			switch (t2Kind)
			{
				case SyntaxKind.IdentifierToken:
					return true;
			}
		}

		// float1
		if (t1IsKeyword && t2Kind == SyntaxKind.FloatToken)
			return true;

		// 11
		if (t1Kind == SyntaxKind.FloatToken && t2Kind == SyntaxKind.FloatToken)
			return true;

		// .1
		if (t1Kind == SyntaxKind.DotToken && t2Kind == SyntaxKind.FloatToken)
			return true;

		// 1.
		if (t1Kind == SyntaxKind.FloatToken && t2Kind == SyntaxKind.DotToken)
			return true;

		// ==
		if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.EqualsToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		// !=
		if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.BangToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		// +=
		if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		// ++
		if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.PlusToken)
			return true;
		if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.PlusPlusToken)
			return true;
		if (t1Kind == SyntaxKind.PlusToken && t2Kind == SyntaxKind.PlusEqualsToken)
			return true;

		// -=
		if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		// --
		if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.MinusToken)
			return true;
		if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.MinusMinusToken)
			return true;
		if (t1Kind == SyntaxKind.MinusToken && t2Kind == SyntaxKind.MinusEqualsToken)
			return true;

		// *=
		if (t1Kind == SyntaxKind.StarToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.StarToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		// /=
		if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		// %=
		if (t1Kind == SyntaxKind.PercentToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.PercentToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		// <=
		if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.LessToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		// >=
		if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsToken)
			return true;
		if (t1Kind == SyntaxKind.GreaterToken && t2Kind == SyntaxKind.EqualsEqualsToken)
			return true;

		//if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandToken)
		//    return true;

		//if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandAmpersandToken)
		//    return true;

		//if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.EqualsToken)
		//    return true;

		//if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.EqualsEqualsToken)
		//    return true;

		//if (t1Kind == SyntaxKind.AmpersandToken && t2Kind == SyntaxKind.AmpersandEqualsToken)
		//    return true;

		//if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeToken)
		//    return true;

		//if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipePipeToken)
		//    return true;

		//if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.EqualsToken)
		//    return true;

		//if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.EqualsEqualsToken)
		//    return true;

		//if (t1Kind == SyntaxKind.PipeToken && t2Kind == SyntaxKind.PipeEqualsToken)
		//    return true;

		//if (t1Kind == SyntaxKind.HatToken && t2Kind == SyntaxKind.EqualsToken)
		//    return true;

		//if (t1Kind == SyntaxKind.HatToken && t2Kind == SyntaxKind.EqualsEqualsToken)
		//    return true;

		// //
		if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SlashToken)
			return true;
		if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SlashEqualsToken)
			return true;
		if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.SingleLineCommentTrivia)
			return true;
		if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.MultiLineCommentTrivia)
			return true;

		// /*
		if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.StarToken)
			return true;
		if (t1Kind == SyntaxKind.SlashToken && t2Kind == SyntaxKind.StarEqualsToken)
			return true;

		return false;
	}

	private static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)> GetTokenPairs()
	{
		foreach (var t1 in GetTokens())
		{
			foreach (var t2 in GetTokens())
			{
				if (!RequiresSeparator(t1.kind, t2.kind))
					yield return (t1.kind, t1.text, t2.kind, t2.text);
			}
		}
	}

	private static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind separatorKind, string separatorText, SyntaxKind t2Kind, string t2Text)> GetTokenPairsWithSeparator()
	{
		foreach (var t1 in GetTokens())
		{
			foreach (var t2 in GetTokens())
			{
				if (RequiresSeparator(t1.kind, t2.kind))
				{
					foreach (var s in GetSeparators())
					{
						if (!RequiresSeparator(t1.kind, s.kind) && !RequiresSeparator(s.kind, t2.kind))
							yield return (t1.kind, t1.text, s.kind, s.text, t2.kind, t2.text);
					}
				}
			}
		}
	}
}
