// <copyright file="Program.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using CommandLineParser;
using CommandLineParser.Attributes;
using CommandLineParser.Commands;
using FancadeLoaderLib;
using FancadeLoaderLib.Editing.Scripting;
using FancadeLoaderLib.Editing.Scripting.Builders;
using FancadeLoaderLib.Editing.Scripting.Placers;
using FanScript.Compiler;
using FanScript.Compiler.Syntax;
using FanScript.Utils;
using MathUtils.Vectors;
using TextCopy;

#if DEBUG
using System.Diagnostics;
#endif

namespace FanScript.Cli;

internal class Program
{
	private static int Main(string[] args)
	{
#if DEBUG
		Debugger.Launch();
#endif
		return CommandParser.ParseAndRun(args, new ParseOptions(), null, [typeof(BuildCommand)]);
	}

	[HelpText("Compiles a single fcs file.")]
	[CommandName("build")]
	public class BuildCommand : ConsoleCommand
	{
		[Required]
		[HelpText("Path to the .fcs file to compile.")]
		[Argument("src")]
		public string Src { get; set; } = null!;

		[HelpText("Which builder to use. EditorScript - Builds code to EditorScript (js) which can be ran in the web wersion of fancade, GameFile - Builds the code into a file, which can be imported with the import button.")]
		[Option("builder")]
		public CodeBuilderEnum Builder { get; set; } = CodeBuilderEnum.EditorScript;

		[HelpText("Determines how blocks are placed, values: Tower (Places blocks in towers), Ground (Places blocks on ground, attempts to replicate how normal people place blocks).")]
		[Option("placer")]
		public BlockPlacerEnum Placer { get; set; } = BlockPlacerEnum.Tower;

		[HelpText("Position the generated blocks will be placed at.")]
		[Option("buildPos")]
		public Int3Arg Pos { get; set; } = new Int3Arg("0,0,0");

		[HelpText("If the syntax tree should be displayed.")]
		[Option("showTree")]
		public bool ShowSyntaxTree { get; set; } = false;

		[HelpText("If the compiled code should be displayed.")]
		[Option("showCompiled")]
		public bool ShowCompiledCode { get; set; } = false;

		// builder options
		[DependsOn(nameof(Builder), CodeBuilderEnum.GameFile)]
		[HelpText("Path to the game file that will be used by GameFile builder, this file will not be overwriten. If not supplied, a new game is created.")]
		[Option("inGameFile")]
		public string? InGameFile { get; set; }

		[DependsOn(nameof(Builder), CodeBuilderEnum.GameFile)]
		[HelpText("If an existing prefab should be used or a new one created.")]
		[Option("useExistingPrefab")]
		public bool UseExistingPrefab { get; set; } = false;

		[DependsOn(nameof(Builder), CodeBuilderEnum.GameFile)]
		[DependsOn(nameof(UseExistingPrefab), false)]
		[HelpText("Name of the new prefab, in which code will get placed.")]
		[Option("prefabName")]
		public string PrefabName { get; set; } = "New Block";

		[DependsOn(nameof(Builder), CodeBuilderEnum.GameFile)]
		[DependsOn(nameof(UseExistingPrefab), false)]
		[HelpText("Type of the new prefab, in which code will get placed.")]
		[Option("prefabType")]
		public PrefabType PrefabType { get; set; } = PrefabType.Script;

		[DependsOn(nameof(Builder), CodeBuilderEnum.GameFile)]
		[DependsOn(nameof(UseExistingPrefab), true)]
		[HelpText("Index of the custom prefab that code will get placed in. NOT the prefab's id, but it's index as a custom prefab - the first custom prefab would be id 597 and INDEX 0.")]
		[Option("prefabIndex")]
		public ushort PrefabIndex { get; set; }

		public override int Run()
		{
			if (!File.Exists(Src))
			{
				return Log.Error($"Source file '{Src}' wasn't found.", ErrorCode.FileNotFound);
			}

			SyntaxTree tree = SyntaxTree.Load(Src);

			if (ShowSyntaxTree)
			{
				Console.WriteLine(tree.Root);
				Console.WriteLine();
			}

			Compilation compilation = Compilation.Create(null, tree);

			if (ShowCompiledCode)
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
			switch (Builder)
			{
				case CodeBuilderEnum.EditorScript:
					builder = new EditorScriptBlockBuilder();
					break;
				case CodeBuilderEnum.GameFile:
					Game? inGame = null;

					if (!string.IsNullOrEmpty(InGameFile))
					{
						if (!File.Exists(InGameFile))
						{
							return Log.Error($"In game file '{Src}' wasn't found.", ErrorCode.FileNotFound);
						}

						try
						{
							using (FileStream fs = File.OpenRead(InGameFile))
							{
								inGame = Game.LoadCompressed(fs);
							}
						}
						catch (Exception ex)
						{
							return Log.Error($"Failed to load in game.", ErrorCode.UnknownError, ex);
						}
					}

					builder = UseExistingPrefab && inGame is not null
						? new GameFileBlockBuilder(inGame, PrefabIndex)
						: (BlockBuilder)new GameFileBlockBuilder(inGame, PrefabName, PrefabType);
					break;
				default:
					throw new InvalidDataException($"Unknown CodeBuilder '{Builder}'");
			}

			ICodePlacer placer = Placer switch
			{
				BlockPlacerEnum.Tower => new TowerCodePlacer(builder),
				BlockPlacerEnum.Ground => new GroundCodePlacer(builder),
				_ => throw new InvalidDataException($"Unknown BlockPlacer '{Placer}'"),
			};
			var diagnostics = compilation.Emit(placer, builder);

			if (diagnostics.Any())
			{
				Console.WriteLine("Warning(s)/error(s):");
				Console.Out.WriteDiagnostics(diagnostics);

				if (diagnostics.HasErrors())
				{
					return (int)ErrorCode.CompilationErrors;
				}
			}

			int3 pos = Pos is null ? int3.Zero : new int3(Pos.X, Pos.Y, Pos.Z);

			if (pos.X < 0 || pos.Y < 0 || pos.Z < 0)
			{
				return Log.Error($"Build position ({pos}) cannot be negative.", ErrorCode.InvalidBuildPos);
			}

			switch (Builder)
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
						Game game = (Game)builder.Build(pos);

						game.TrimPrefabs();

						using (FileStream fs = File.OpenWrite("GAME.fcg"))
						{
							game.SaveCompressed(fs);
						}

						Log.Info($"Built code to file '{Path.GetFullPath("GAME.fcg")}'");
					}

					break;
				default:
					throw new InvalidDataException($"Unknown CodeBuilder '{Builder}'");
			}

			Log.Info("Done");

			return 0;
		}
	}
}
