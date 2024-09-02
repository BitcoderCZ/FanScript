#define EDITOR_SCRIPT
//#define GROUND_PLACER

#if !EDITOR_SCRIPT
using FancadeLoaderLib;
#endif
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
            SyntaxTree source = SyntaxTree.Load(@"E:\dev\VSProjects\FanScript.Core\FanScript\source.fcs");

            if (source.Diagnostics.Any())
            {
                Console.WriteLine("Lexer/Parser warning(s)/error(s)");
                Console.Out.WriteDiagnostics(source.Diagnostics);
                Console.ReadKey(true);
                if (source.Diagnostics.HasErrors())
                    return;
            }

            Console.WriteLine(source.Root);
            Console.WriteLine();

            Compilation compilation = Compilation.CreateScript(null, source);
            BoundGlobalScope scope = compilation.GlobalScope;

            if (scope.Diagnostics.Any())
            {
                Console.WriteLine("Binder warning(s)/error(s)");
                Console.Out.WriteDiagnostics(scope.Diagnostics);
                Console.ReadKey(true);
                if (scope.Diagnostics.HasErrors())
                    return;
            }
            for (int i = 0; i < scope.Statements.Length; i++)
                scope.Statements[i].WriteTo(Console.Out);

            Console.WriteLine();

            compilation.EmitTree(Console.Out);
            foreach (var func in compilation.Functions)
                compilation.EmitTree(func, Console.Out);

#if GROUND_PLACER
            IBlockPlacer placer = new GroundBlockPlacer()
            {
                BlockXOffset = 3,
            };
#else
            IBlockPlacer placer = new TowerBlockPlacer()
            {
                MaxHeight = 25,
            };
#endif

#if EDITOR_SCRIPT
            CodeBuilder builder = new EditorScriptCodeBuilder(placer);
#else
            CodeBuilder builder = new GameFileCodeBuilder(placer);
#endif
            ImmutableArray<Diagnostic> diagnostics = compilation.Emit(builder);
            if (diagnostics.Any())
            {
                Console.WriteLine("Emitter warning(s)/error(s)");
                Console.Out.WriteDiagnostics(diagnostics);
                Console.ReadKey(true);
                if (scope.Diagnostics.HasErrors())
                    return;
            }

#if EDITOR_SCRIPT
            string code = (string)builder.Build(new Vector3I(4, 0, 4));
            Console.WriteLine(code);

            ClipboardService.SetText(code);

            Console.WriteLine("Copied to console");
#else
            Game game = (Game)builder.Build(new Vector3I(0, 0, 0));

            using (FileStream fs = File.OpenWrite("658B97B57E427478"))
                game.SaveCompressed(fs);
#endif

            Console.WriteLine("Done");
            Console.ReadKey(true);
        }
    }
}
