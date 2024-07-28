using FanScript.Compiler.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Tests.Text
{
    public class SourceTextTests
    {
        [Theory]
        [InlineData(".", 1)]
        [InlineData(".\r\n", 2)]
        [InlineData(".\r\n\r\n", 3)]
        public void SourceText_IncludesLastLine(string text, int expectedLineCount)
        {
            SourceText sourceText = SourceText.From(text);

            Assert.Equal(expectedLineCount, sourceText.Lines.Length);
        }
    }
}
