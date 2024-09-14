using CommandLine;
using FancadeLoaderLib;
using FanScript.Compiler;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockPlacers;
using FanScript.Compiler.Emit.CodeBuilders;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using TextCopy;

namespace FanScript.Cli
{
    internal class Program
    {
#pragma warning disable CS8618
        [Verb("build", HelpText = "Compiles a single fcs file")]
        public class BuildOptions
        {
            [Option("src", Required = true, HelpText = "Path to the .fcs file to compile.")]
            public string Src { get; set; }

            [Option("builder", Default = CodeBuilderEnum.EditorScript, Required = false, HelpText = "Values: EditorScript (Builds code to EditorScript (js) which can be ran in the web wersion of fancade), GameFile (Builds the code into a file, which can be imported with the import button).")]
            public CodeBuilderEnum Builder { get; set; }

            [Option("placer", Default = BlockPlacerEnum.Tower, Required = false, HelpText = "Determines how blocks are placed, values: Tower (Places blocks in towers), Ground (Places blocks on ground, attempts to replicate how normal people place blocks).")]
            public BlockPlacerEnum Placer { get; set; }

            [Option("buildPos", Required = false, HelpText = "(Default: 0,0,0) Position the generated blocks will be placed at.")]
            public Vector3IArg? Pos { get; set; }

            [Option("showTree", Default = false, Required = false, HelpText = "If the syntax tree should be displayed.")]
            public bool ShowSyntaxTree { get; set; }

            [Option("showCompiled", Default = false, Required = false, HelpText = "If the compiled code should be displayed.")]
            public bool ShowCompiledCode { get; set; }
        }
#pragma warning restore CS8618 

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<BuildOptions>(args)
                .MapResult(
                    opts => runVerb(runBuild, opts, "build"),
                errs => 1);
        }

        static int runVerb<T>(Func<T, int> action, T arg, string name)
        {
            try
            {
                return action(arg);
            }
            catch (Exception ex)
            {
                return Log.Error("An Exception occured", ErrorCode.UnknownError, ex);
            }
        }

        static int runBuild(BuildOptions opts)
        {
            if (!File.Exists(opts.Src))
                return Log.Error($"Source file '{opts.Src}' wasn't found.", ErrorCode.FileNotFound);

            Debugger.Launch();

            SyntaxTree tree = SyntaxTree.Load(opts.Src);

            if (opts.ShowSyntaxTree)
            {
                Console.WriteLine(tree.Root);
                Console.WriteLine();
            }

            Compilation compilation = Compilation.CreateScript(null, tree);

            if (opts.ShowCompiledCode)
            {
                compilation.EmitTree(Console.Out);
                foreach (var func in compilation.Functions)
                    compilation.EmitTree(func, Console.Out);

                Console.WriteLine();
            }

            BlockBuilder builder;
            switch (opts.Builder)
            {
                case CodeBuilderEnum.EditorScript:
                    builder = new EditorScriptBlockBuilder();
                    break;
                case CodeBuilderEnum.GameFile:
                    builder = new GameFileBlockBuilder();
                    break;
                default:
                    throw new InvalidDataException($"Unknown CodeBuilder '{opts.Builder}'");
            }

            CodePlacer placer;
            switch (opts.Placer)
            {
                case BlockPlacerEnum.Tower:
                    placer = new TowerCodePlacer(builder)
                    {
                        MaxHeight = 10
                    };
                    break;
                case BlockPlacerEnum.Ground:
                    placer = new GroundCodePlacer(builder);
                    break;
                default:
                    throw new InvalidDataException($"Unknown BlockPlacer '{opts.Placer}'");
            }

            var diagnostics = compilation.Emit(placer, builder);

            if (diagnostics.Any())
            {
                Console.WriteLine("Warning(s)/error(s):");
                Console.Out.WriteDiagnostics(diagnostics);

                if (diagnostics.HasErrors())
                    return (int)ErrorCode.CompilationErrors;
            }

            Vector3I pos = opts.Pos is null ? new Vector3I(0, 0, 0) : new Vector3I(opts.Pos.X, opts.Pos.Y, opts.Pos.Z);

            if (pos.X < 0 || pos.Y < 0 || pos.Z < 0)
                return Log.Error($"Build position ({pos}) cannot be negative.", ErrorCode.InvalidBuildPos);

            switch (opts.Builder)
            {
                case CodeBuilderEnum.EditorScript:
                    {
                        string code = (string)builder.Build(pos);

                        Console.WriteLine("Compiled EditorScrip:");
                        Console.WriteLine(code);

                        ClipboardService.SetText(code);

                        Log.Info("Copied code to clipboard");
                    }
                    break;
                case CodeBuilderEnum.GameFile:
                    {
                        Game game = (Game)builder.Build(pos);

                        game.TrimPrefabs();

                        using (FileStream fs = File.OpenWrite("658B97B57E427478"))
                            game.SaveCompressed(fs);

                        Log.Info($"Built code to file '{Path.GetFullPath("658B97B57E427478")}'");
                    }
                    break;
                default:
                    throw new InvalidDataException($"Unknown CodeBuilder '{opts.Builder}'");
            }

            Log.Info("Done");

            return 0;
        }
    }
}
