using CommandLine;
using FancadeLoaderLib;
using FanScript.Compiler;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockPlacers;
using FanScript.Compiler.Emit.CodeBuilders;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using MathUtils.Vectors;
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

            [Option("builder", Default = CodeBuilderEnum.EditorScript, Required = false, HelpText = "Values: EditorScript (Builds code to EditorScript (js) which can be ran in the web wersion of fancade), GameFile (Builds the code into a file, which can be imported with the import button)")]
            public CodeBuilderEnum Builder { get; set; }

            [Option("placer", Default = BlockPlacerEnum.Tower, Required = false, HelpText = "Determines how blocks are placed, values: Tower (Places blocks in towers), Ground (Places blocks on ground, attempts to replicate how normal people place blocks)")]
            public BlockPlacerEnum Placer { get; set; }
        }
#pragma warning restore CS8618 

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<BuildOptions>(args)
                .MapResult(
                    RunBuild,
                errs => 1);
        }

        static int RunBuild(BuildOptions opts)
        {
            if (!File.Exists(opts.Src))
                return Log.Error($"Source file '{opts.Src}' wasn't found.", ErrorCode.FileNotFound);

            SyntaxTree tree = SyntaxTree.Load(opts.Src);

            Compilation compilation = Compilation.CreateScript(null, tree);

            IBlockPlacer blockPlacer;
            switch (opts.Placer)
            {
                case BlockPlacerEnum.Tower:
                    blockPlacer = new TowerBlockPlacer();
                    break;
                case BlockPlacerEnum.Ground:
                    blockPlacer = new GroundBlockPlacer();
                    break;
                default:
                    throw new InvalidDataException($"Unknown BlockPlacer '{opts.Placer}'");
            }

            CodeBuilder builder;
            switch (opts.Builder)
            {
                case CodeBuilderEnum.EditorScript:
                    builder = new EditorScriptCodeBuilder(blockPlacer);
                    break;
                case CodeBuilderEnum.GameFile:
                    builder = new GameFileCodeBuilder(blockPlacer);
                    break;
                default:
                    throw new InvalidDataException($"Unknown CodeBuilder '{opts.Builder}'");
            }

            var diagnostics = compilation.Emit(builder);

            if (diagnostics.Any())
            {
                Console.WriteLine("Warning(s)/error(s):");
                Console.Out.WriteDiagnostics(diagnostics);
                Console.ReadKey(true);
                if (diagnostics.HasErrors())
                    return (int)ErrorCode.CompilationErrors;
            }

            switch (opts.Builder)
            {
                case CodeBuilderEnum.EditorScript:
                    {
                        string code = (string)builder.Build(new Vector3I(4, 0, 4));
                        Console.WriteLine(code);

                        ClipboardService.SetText(code);

                        Log.Info("Copied code to clipboard");
                    }
                    break;
                case CodeBuilderEnum.GameFile:
                    {
                        Game game = (Game)builder.Build(new Vector3I(0, 0, 0));

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
