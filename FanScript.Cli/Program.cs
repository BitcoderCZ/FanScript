using CommandLine;
using FancadeLoaderLib;
using FanScript.Compiler;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockPlacers;
using FanScript.Compiler.Emit.CodeBuilders;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using MathUtils.Vectors;
using TextCopy;

#if DEBUG
using System.Diagnostics;
#endif

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

            // builder options
            [Option("inGameFile", Default = null, Required = false, HelpText = "(Only used with GameFileBuilder) Path to the game file that will be used by GameFile builder, this file will not be overwriten. If not supplied, a new game is created.")]
            public string? InGameFile { get; set; }

            [Option("useExistingPrefab", Default = false, Required = false, HelpText = "(Only used with GameFileBuilder) If true an existing prefab from InGameFile will be used (prefabIndex) otherwise a new prefab wiil be created (prefabName, prefabType).")]
            public bool UseExistingPrefab { get; set; }

            [Option("prefabName", SetName = "gfb_new", Default = "New Block", Required = false, HelpText = "(Only used with GameFileBuilder) Name of the new prefab, in which code will get placed.")]
            public string PrefabName { get; set; }
            [Option("prefabType", SetName = "gfb_new", Default = PrefabType.Script, Required = false, HelpText = $"(Only used with GameFileBuilder) Type of the new prefab, in which code will get placed. Values: {nameof(PrefabType.Normal)}, {nameof(PrefabType.Physics)}, {nameof(PrefabType.Script)}, {nameof(PrefabType.Level)}.")]
            public PrefabType PrefabType { get; set; }

            [Option("prefabIndex", SetName = "gfb_existing", Default = (ushort)0, Required = false, HelpText = "(Only used with GameFileBuilder)Index of the custom prefab that code will get placed in. NOT the prefab's id, but it's index as a custom prefab - the first custom prefab would be id 597 and INDEX 0.")]
            public ushort PrefabIndex { get; set; }
        }
#pragma warning restore CS8618 

        static int Main(string[] args)
        {
#if DEBUG
            Debugger.Launch();
#endif

            return Parser.Default.ParseArguments<BuildOptions>(args)
                .MapResult(
                    opts => runVerb(runBuild, opts, "build"),
                errs => 1);
        }

        static int runVerb<T>(Func<T, int> action, T arg, string name)
        {
#if !DEBUG
            try
            {
#endif
            return action(arg);
#if !DEBUG
            }
            catch (Exception ex)
            {
                return Log.Error("An Exception occured", ErrorCode.UnknownError, ex);
            }
#endif
        }

        static int runBuild(BuildOptions opts)
        {
            if (!File.Exists(opts.Src))
                return Log.Error($"Source file '{opts.Src}' wasn't found.", ErrorCode.FileNotFound);

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
                Console.WriteLine();

                foreach (var func in compilation.Functions)
                {
                    compilation.EmitTree(func, Console.Out);
                    Console.WriteLine();
                }

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
                    placer = new TowerCodePlacer(builder);
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

                        Console.WriteLine("Compiled EditorScript:");
                        Console.WriteLine(code);

                        ClipboardService.SetText(code);

                        Log.Info("Copied code to clipboard");
                    }
                    break;
                case CodeBuilderEnum.GameFile:
                    {
                        GameFileBlockBuilder.Args args;
                        if (opts.UseExistingPrefab)
                            args = new(opts.InGameFile, opts.PrefabIndex);
                        else
                            args = new(opts.InGameFile, opts.PrefabName, opts.PrefabType);

                        Game game = (Game)builder.Build(pos, args);

                        game.TrimPrefabs();

                        using (FileStream fs = File.OpenWrite("GAME.fcg"))
                            game.SaveCompressed(fs);

                        Log.Info($"Built code to file '{Path.GetFullPath("GAME.fcg")}'");
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
