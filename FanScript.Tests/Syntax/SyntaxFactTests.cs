using FanScript.Compiler.Syntax;
using System.Collections.Immutable;

namespace FanScript.Tests.Syntax;

public class SyntaxFactTests
{
	[Theory]
	[MemberData(nameof(GetSyntaxKindData))]
	public void GetText_RoundTrips(SyntaxKind kind)
	{
		string? text = SyntaxFacts.GetText(kind);
		if (text is null)
			return;

		ImmutableArray<SyntaxToken> tokens = SyntaxTree.ParseTokens(text);
		SyntaxToken token = Assert.Single(tokens);

		Assert.Equal(kind, token.Kind);
		Assert.Equal(text, token.Text);
	}

	public static IEnumerable<object[]> GetSyntaxKindData()
	{
		SyntaxKind[] kinds = Enum.GetValues<SyntaxKind>();

		foreach (var kind in kinds)
			yield return [kind];
	}
}
