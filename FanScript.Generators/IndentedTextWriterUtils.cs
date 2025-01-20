using System;
using System.CodeDom.Compiler;

namespace FanScript.Generators;
internal static class IndentedTextWriterUtils
{
	public static IDisposable CurlyIndent(this IndentedTextWriter writer, string openingLine = "")
	{
		if (!string.IsNullOrWhiteSpace(openingLine))
		{
			writer.WriteLine(openingLine);
		}

		writer.WriteLine("{");
		writer.Indent++;

		return new Disposable(() =>
		{
			writer.Indent--;
			writer.WriteLine("}");
		});
	}
}
