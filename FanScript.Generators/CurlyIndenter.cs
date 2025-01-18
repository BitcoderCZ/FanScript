using System;
using System.CodeDom.Compiler;

namespace FanScript.Generators
{
	/// <summary>
	/// Takes care of opening and closing curly braces for code generation
	/// </summary>
	internal class CurlyIndenter : IDisposable
	{
		private IndentedTextWriter indentedTextWriter;

		/// <summary>
		/// Default constructor that maked a tidies creation of the line before the opening curly
		/// </summary>
		/// <param name="indentedTextWriter">The writer to use</param>
		/// <param name="openingLine">any line to write before the curly</param>
		public CurlyIndenter(IndentedTextWriter indentedTextWriter, string openingLine = "")
		{
			this.indentedTextWriter = indentedTextWriter;
			if (!string.IsNullOrWhiteSpace(openingLine))
				indentedTextWriter.WriteLine(openingLine);
			indentedTextWriter.WriteLine("{");
			indentedTextWriter.Indent++;
		}

		/// <summary>
		/// When the variable goes out of scope the closing brace is injected and indentation reduced.
		/// </summary>
		public void Dispose()
		{
			indentedTextWriter.Indent--;
			indentedTextWriter.WriteLine("}");
		}
	}
}
