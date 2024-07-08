using FanScript.Compiler;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockPlacers;
using FanScript.Compiler.Emit.CodeBuilders;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;
using TextCopy;

namespace FanScript
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            SyntaxTree source = SyntaxTree.Load(@"C:\dev\VSProjects\FanScript.Core\FanScript\source.txt");
            Console.WriteLine(source.Root);

            Console.WriteLine();

            Compilation compilation = Compilation.CreateScript(null, source);
            BoundGlobalScope scope = compilation.GlobalScope;

            if (scope.Diagnostics.Any())
            {
                Console.Out.WriteDiagnostics(scope.Diagnostics);
                Console.ReadKey(true);
            }
            for (int i = 0; i < scope.Statements.Length; i++)
                Console.WriteLine(scope.Statements[i]);

            compilation.EmitTree(Console.Out);

            CodeBuilder builder = new EditorScriptCodeBuilder(new GroundBlockPlacer());
            ImmutableArray<Diagnostic> diagnostics = compilation.Emit(builder);
            if (diagnostics.Any())
            {
                Console.Out.WriteDiagnostics(diagnostics);
                Console.ReadKey(true);
            }
            else
            {
                string code = (string)builder.Build(new Vector3I(4, 0, 4));
                Console.WriteLine(code);

                ClipboardService.SetText(code);

                Console.WriteLine("Copied to console");
            }

            Console.WriteLine("Done");
            Console.ReadKey(true);
        }
    }
}
