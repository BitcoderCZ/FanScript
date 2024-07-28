using FanScript.Compiler.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Tests.Syntax
{
    public class SyntaxFactTests
    {
        [Theory]
        [MemberData(nameof(GetSyntaxKindData))]
        public void SyntaxFact_GetText_RoundTrips(SyntaxKind kind)
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
}
