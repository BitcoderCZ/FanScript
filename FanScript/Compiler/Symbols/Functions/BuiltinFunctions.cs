﻿// <copyright file="BuiltinFunctions.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FancadeLoaderLib.Editing.Scripting.Utils;
using FancadeLoaderLib.Partial;
using FancadeLoaderLib.Raw;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Documentation.Attributes;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.DocumentationGenerator")]

namespace FanScript.Compiler.Symbols;

internal static partial class BuiltinFunctions
{
	public static readonly IReadOnlyDictionary<FunctionSymbol, FunctionDocAttribute> FunctionToDoc;

	#region Basic Functions
	[FunctionDoc(
	   Info = """
        Displays <link type="param">value</>.
        """,
	   Examples = """
        <codeblock lang="fcs">
        vec3 pos
        // ...
        inspect(pos)
        </>
        """)]
	public static readonly FunctionSymbol Inspect
	   = new BuiltinFunctionSymbol(
		   BuiltinNamespace,
		   "inspect",
		   [
			   new ParameterSymbol("value", TypeSymbol.Generic)
		   ],
		   TypeSymbol.Void,
		   [TypeSymbol.Bool, TypeSymbol.Float, TypeSymbol.Vector3, TypeSymbol.Rotation, TypeSymbol.Object],
		   (call, context) =>
		   {
			   Block inspect = context.AddBlock(StockBlocks.Values.InspectByType(call.GenericType!.ToWireType()));

			   using (context.ExpressionBlock())
			   {
				   ITerminalStore store = context.EmitExpression(call.Arguments[0]);

				   context.Connect(store, TerminalStore.CIn(inspect, inspect.Type.Terminals[1]));
			   }

			   return new TerminalStore(inspect);
		   });

	[FunctionDoc(
		Info = """
        Returns the value (reference) from <link type="param">array</> at <link type="param">index</>.
        """,
		ReturnValueInfo = """
        The value at <link type="param">index</>.
        """,
		Examples = """
        <codeblock lang="fcs">
        array<float> arr = [3, 2, 1]
        inspect(arr.get(1)) // 2
        </>
        """)]
	public static readonly FunctionSymbol ArrayGet
		= new BuiltinFunctionSymbol(
			BuiltinNamespace,
			"get",
			[
				new ParameterSymbol("array", TypeSymbol.Array),
				new ParameterSymbol("index", TypeSymbol.Float),
			],
			TypeSymbol.Generic,
			TypeSymbol.BuiltInNonGenericTypes,
			(call, context) =>
			{
				var indexArg = call.Arguments[1];

				// optimize for when index is 0
				if (indexArg.ConstantValue is not null && (float)indexArg.ConstantValue.GetValueOrDefault(TypeSymbol.Float) == 0f)
				{
					return context.EmitExpression(call.Arguments[0]);
				}

				Block list = context.AddBlock(StockBlocks.Variables.ListByType(call.GenericType!.ToWireType()));

				using (context.ExpressionBlock())
				{
					ITerminalStore array = context.EmitExpression(call.Arguments[0]);
					ITerminalStore index = context.EmitExpression(call.Arguments[1]);

					context.Connect(array, TerminalStore.CIn(list, list.Type["Variable"]));
					context.Connect(index, TerminalStore.CIn(list, list.Type["Index"]));
				}

				return TerminalStore.COut(list, list.Type["Element"]);
			})
		{
			IsMethod = true,
		};

	[FunctionDoc(
		Info = """
        Sets <link type="param">array</> at <link type="param">index</> to <link type="param">value</>.
        """,
		Examples = """
        <codeblock lang="fcs">
        array<float> arr
        on Loop(0, 5, out inline float i)
        {
            arr.set(i, i + 1)
        }
        // arr - [1, 2, 3, 4, 5]
        </>
        """)]
	public static readonly FunctionSymbol ArraySet
		= new BuiltinFunctionSymbol(
			BuiltinNamespace,
			"set",
			[
				new ParameterSymbol("array", TypeSymbol.Array),
				new ParameterSymbol("index", TypeSymbol.Float),
				new ParameterSymbol("value", TypeSymbol.Generic),
			],
			TypeSymbol.Void,
			TypeSymbol.BuiltInNonGenericTypes,
			(call, context) =>
			{
				var indexArg = call.Arguments[1];

				// optimize for when index is 0
				if (indexArg.ConstantValue is not null && (float)indexArg.ConstantValue.GetValueOrDefault(TypeSymbol.Float) == 0f)
				{
					return context.EmitSetExpression(call.Arguments[0], () =>
					{
						using (context.ExpressionBlock())
						{
							return context.EmitExpression(call.Arguments[2]);
						}
					});
				}

				Block setPtr = context.AddBlock(StockBlocks.Variables.SetPtrByType(call.GenericType!.ToWireType()));

				using (context.ExpressionBlock())
				{
					Block list = context.AddBlock(StockBlocks.Variables.ListByType(call.GenericType!.ToWireType()));

					using (context.ExpressionBlock())
					{
						ITerminalStore array = context.EmitExpression(call.Arguments[0]);
						ITerminalStore index = context.EmitExpression(call.Arguments[1]);

						context.Connect(array, TerminalStore.CIn(list, list.Type["Variable"]));
						context.Connect(index, TerminalStore.CIn(list, list.Type["Index"]));
					}

					ITerminalStore value = context.EmitExpression(call.Arguments[2]);

					context.Connect(TerminalStore.COut(list, list.Type["Element"]), TerminalStore.CIn(setPtr, setPtr.Type["Variable"]));
					context.Connect(value, TerminalStore.CIn(setPtr, setPtr.Type["Value"]));
				}

				return new TerminalStore(setPtr);
			})
		{
			IsMethod = true,
		};

	[FunctionDoc(
		Info = """
        Sets a range of <link type="param">array</>, starting at <link type="param">index</>.
        """,
		Examples = """
        <codeblock lang="fcs">
        array<float> arr
        arr.setRange(2, [1, 2, 3])
        // arr - [0, 0, 1, 2, 3]
        </>
        """)]
	public static readonly FunctionSymbol ArraySetRange
		= new BuiltinFunctionSymbol(
			BuiltinNamespace,
			"setRange",
			[
				new ParameterSymbol("array", TypeSymbol.Array),
				new ParameterSymbol("index", TypeSymbol.Float),
				new ParameterSymbol("value", TypeSymbol.ArraySegment),
			],
			TypeSymbol.Void,
			TypeSymbol.BuiltInNonGenericTypes,
			(call, context) =>
			{
				var array = call.Arguments[0];
				var index = call.Arguments[1];
				var segment = (BoundArraySegmentExpression)call.Arguments[2];

				Debug.Assert(segment.Elements.Length > 0, $"There must be more than 0 elements in an {nameof(BoundArraySegmentExpression)}.");

				return context.EmitSetArraySegment(segment, array, index);
			})
		{
			IsMethod = true,
		};

	[FunctionDoc(
		Info = """
        Sets the value of a variable/array element.
        """,
		ParameterInfos = [
			"""
            The variable/array element to set.
            """,
			"""
            The value to set <link type="param">pointer</> to.
            """
		],
		Examples = """
        <codeblock lang="fcs">
        array<float> arr
        // ...
        inline float first = arr.get(0)
        inspect(first)
        setPtrValue(first, 10)
        // arr - [10, ...]
        </>
        """)]
	public static readonly FunctionSymbol PtrSetValue
		= new BuiltinFunctionSymbol(
			BuiltinNamespace,
			"setPtrValue",
			[
				new ParameterSymbol("pointer", TypeSymbol.Generic),
				new ParameterSymbol("value", TypeSymbol.Generic),
			],
			TypeSymbol.Void,
			TypeSymbol.BuiltInNonGenericTypes,
			(call, context) =>
			{
				Block setPtr = context.AddBlock(StockBlocks.Variables.SetPtrByType(call.GenericType!.ToWireType()));

				using (context.ExpressionBlock())
				{
					ITerminalStore ptr = context.EmitExpression(call.Arguments[0]);
					ITerminalStore value = context.EmitExpression(call.Arguments[1]);

					context.Connect(ptr, TerminalStore.CIn(setPtr, setPtr.Type["Variable"]));
					context.Connect(value, TerminalStore.CIn(setPtr, setPtr.Type["Value"]));
				}

				return new TerminalStore(setPtr);
			});

	[FunctionDoc(
		Info = """
        Creates a comment, that gets emitted into fancade as comment StockBlocks.
        """,
		ParameterInfos = [
			"""
            The text of the comment.
            """
		],
		Examples = """
        <codeblock lang="fcs">
        // comment only visible in code
        fcComment("comment also visible in fancade")
        </>
        """)]
	public static readonly FunctionSymbol FcComment
		= new BuiltinFunctionSymbol(
			BuiltinNamespace,
			"fcComment",
			[
				new ParameterSymbol("TEXT", Modifiers.Constant, TypeSymbol.String)
			],
			TypeSymbol.Void,
			(call, context) =>
			{
				object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

				if (constants is not null && constants[0] is string text && !string.IsNullOrEmpty(text))
				{
					context.WriteComment(text);
				}

				return NopTerminalStore.Instance;
			});

	[FunctionDoc(
		Info = """
        Returns a block with the specified id.
        """,
		ReturnValueInfo = """
        The <link type="type">obj</> specified by <link type="param">BLOCK</>.
        """,
		ParameterInfos = [
			"""
            Id of the block, one of <link type="con">BLOCK</>.
            """
		],
		Remarks = [
			"""
            The id of a block can be get by placing the block at (0, 0, 0) and running log(getBlock(0, 0, 0)) in the EditorScript.
            """,
			"""
            EditorScript cannot connect wires to blocks, so if you are using it as the builder, you will have to connect the object wire to the block manually.
            """
		],
		Examples = """
        <codeblock lang="fcs">
        obj dirt = getBlockById(BLOCK_DIRT)
        </>
        """)]
	public static readonly FunctionSymbol GetBlockById
		= new BuiltinFunctionSymbol(
			BuiltinNamespace,
			"getBlockById",
			[
				new ParameterSymbol("BLOCK", Modifiers.Constant, TypeSymbol.Float)
			],
			TypeSymbol.Object,
			(call, context) =>
			{
				object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

				if (constants is null)
				{
					return NopTerminalStore.Instance;
				}

				ushort id = (ushort)System.Math.Clamp((int)((constants[0] as float?) ?? 0f), ushort.MinValue, ushort.MaxValue);

				BlockDef def;
				if (id < RawGame.CurrentNumbStockPrefabs)
				{
					var groupEnumerable = StockBlocks.PrefabList.GetGroupEnumerable(id);

					if (groupEnumerable.Any())
					{
						PartialPrefabGroup group = new PartialPrefabGroup(groupEnumerable);
						PartialPrefab mainPrefab = group[byte3.Zero];

						def = new BlockDef(group, BlockType.NonScript, TerminalBuilder.Empty);
					}
					else
					{
						def = new BlockDef(string.Empty, id, BlockType.NonScript, PrefabType.Normal, new int3(1, 1, 1), TerminalBuilder.Empty);
					}
				}
				else
				{
					def = new BlockDef(string.Empty, id, BlockType.NonScript, PrefabType.Normal, new int3(1, 1, 1), TerminalBuilder.Empty);
				}

				Block block = context.AddBlock(def);

				// TODO
				/*if (context.Builder is not IConnectToBlocksBuilder)
				{
					context.Diagnostics.ReportOpeationNotSupportedOnBuilder(call.Syntax.Location, BuilderUnsupportedOperation.ConnectToBlock);
					return NopTerminalStore.Instance;
				}*/

				return new TerminalStore(NopTerminal.Instance, [new BlockVoxelTerminal(block)]);
			});

	[FunctionDoc(
		Info = """
        *Dirrectly* convers <link type="param">vec</> to <link type="type">rot</>.
        """)]
	public static readonly FunctionSymbol ToRot
	   = new BuiltinFunctionSymbol(
		   BuiltinNamespace,
		   "toRot",
		   [
			   new ParameterSymbol("vec", TypeSymbol.Vector3)
		   ],
		   TypeSymbol.Rotation,
		   (call, context) =>
		   {
			   object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), false);

			   if (constants is not null)
			   {
				   float3 vec = (constants[0] as float3?) ?? float3.Zero;

				   Block block = context.AddBlock(StockBlocks.Values.Rotation);

				   context.SetSetting(block, 0, new Rotation(vec));

				   return TerminalStore.COut(block, block.Type["Rotation"]);
			   }
			   else
			   {
				   Block make = context.AddBlock(StockBlocks.Math.Make_Rotation);

				   using (context.ExpressionBlock())
				   {
					   var (x, y, z) = context.BreakVector(call.Arguments[0]);

					   context.Connect(x, TerminalStore.CIn(make, make.Type["X angle"]));
					   context.Connect(y, TerminalStore.CIn(make, make.Type["Y angle"]));
					   context.Connect(z, TerminalStore.CIn(make, make.Type["Z angle"]));
				   }

				   return TerminalStore.COut(make, make.Type["Rotation"]);
			   }
		   })
	   {
		   IsMethod = true,
	   };

	[FunctionDoc(
		Info = """
        *Dirrectly* convers <link type="param">rot</> to <link type="type">vec3</>.
        """)]
	public static readonly FunctionSymbol ToVec
	   = new BuiltinFunctionSymbol(
		   BuiltinNamespace,
		   "toVec",
		   [
			   new ParameterSymbol("rot", TypeSymbol.Rotation)
		   ],
		   TypeSymbol.Vector3,
		   (call, context) =>
		   {
			   object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), false);

			   if (constants is not null)
			   {
				   Rotation rot = constants[0] is Rotation r ? r : new Rotation(float3.Zero);

				   Block block = context.AddBlock(StockBlocks.Values.Vector);

				   context.SetSetting(block, 0, rot);

				   return TerminalStore.COut(block, block.Type["Vector"]);
			   }
			   else
			   {
				   Block make = context.AddBlock(StockBlocks.Math.Make_Vector);

				   using (context.ExpressionBlock())
				   {
					   var (x, y, z) = context.BreakVector(call.Arguments[0]);

					   context.Connect(x, TerminalStore.CIn(make, make.Type["X"]));
					   context.Connect(y, TerminalStore.CIn(make, make.Type["Y"]));
					   context.Connect(z, TerminalStore.CIn(make, make.Type["Z"]));
				   }

				   return TerminalStore.COut(make, make.Type["Vector"]);
			   }
		   })
	   {
		   IsMethod = true,
	   };

	//public static readonly FunctionSymbol PlayMidi
	//    = new BuiltinFunctionSymbol("playMidi",
	//        [
	//            new ParameterSymbol("fileName", TypeSymbol.String),
	//        ], TypeSymbol.Void, (call, context) =>
	//        {
	//            object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(), true);

	//            if (constants is null)
	//                return NopTerminalStore.Instance;

	//            string? path = constants[0] as string;

	//            if (string.IsNullOrWhiteSpace(path))
	//                throw new Exception();

	//            MidiFile file;
	//            using (FileStream stream = File.OpenRead(path))
	//                file = MidiFile.Read(stream, new ReadingSettings());

	//            MidiConvertSettings convertSettings = MidiConvertSettings.Default;
	//            convertSettings.MaxFrames = 60 * 45;

	//            MidiConverter converter = new MidiConverter(file, convertSettings);

	//            FcSong song = converter.Convert();
	//            var (blocksSize, blocks) = song.ToBlocks();

	//            TerminalConnector connector = new TerminalConnector(context.Connect);

	//            // channel event structure:
	//            // t - type
	//            // d - delta time since last event (in frames)
	//            // a - data0 (optional - depends on type)
	//            // b - data1 (optional - depends on type)
	//            // tttd_dddd - z_pos: 0
	//            // aaaa_aaaa - z_pos: 1
	//            // bbbb_bbbb - z_pos: 2

	//            // TODO: emit set variable originObj to originBlock variable

	//            SyntaxTree tree = SyntaxTree.Parse(SourceText.From($$"""
	//            global array<vec3> midi_c_pos // channel pos
	//            global array<float> midi_c_si // channel sound index
	//            global array<float> midi_c_wt // channel wait time

	//            on Play
	//            {
	//                obj originObj
	//                originObj.getPos(out global vec3 origin, out _)
	//                origin.y -= .5
	//                on Loop(0, {{song.Channels.Count}}, out inline float channelIndex)
	//                {
	//                    midi_c_pos.set(channelIndex, origin + vec3(0, 0, channelIndex * {{FcSong.ChannelSize.X}}))
	//                    midi_c_wt.set(channelIndex, -1)
	//                }
	//            }

	//            on Loop(0, {{song.Channels.Count}}, out inline float channelIndex)
	//            {
	//                //inline float channelIndex = _channelIndex * {{FcSong.ChannelSize.X}}
	//                inline vec3 midi_pos_ref = midi_c_pos.get(channelIndex)
	//                inline float midi_t_ref = midi_c_wt.get(channelIndex)

	//                while (true)
	//                {
	//                    vec3 midi_pos = midi_c_pos.get(channelIndex)
	//                    float midi_t = midi_c_wt.get(channelIndex)

	//                    inspect(midi_pos)
	//                    inspect(midi_t)

	//                    if (midi_t > 0)
	//                    {
	//                        midi_t_ref--
	//                        break
	//                    }
	//                    else
	//                    {
	//                        // read event type
	//                        readBinary(midi_pos, 3, out float midi_et)
	//                        // read event delta (time since last event)
	//                        readBinary(midi_pos + vec3(0, 3, 0), 5, out float midi_ed)

	//                        inspect(midi_ed)

	//                        if (midi_t == -1 && midi_ed > 0)
	//                        {
	//                            setPtrValue(midi_t_ref, midi_ed)
	//                            break
	//                        }
	//                        else
	//                        {
	//                            inspect(midi_et)

	//                            if (midi_et < 0.5)
	//                            {
	//                                // wait
	//                                readBinary(midi_pos + vec3(1, 0, 0), 8, out float midi_w_d)

	//                                setPtrValue(midi_pos_ref, midi_pos + vec3(2, 0, 0)) 
	//                                setPtrValue(midi_t_ref, midi_w_d)
	//                                break
	//                            }
	//                            else if (midi_et < 1.5)
	//                            {
	//                                // play note
	//                                readBinary(midi_pos + vec3(1, 0, 0), 7, out float midi_n_n)
	//                                readBinary(midi_pos + vec3(1, 7, 0), 1, out float midi_n_hv) // has non default velocity (not 255)
	//                                float midi_n_v
	//                                if (midi_n_hv > 0.5)
	//                                {
	//                                    readBinary(midi_pos + vec3(2, 0, 0), 8, out midi_n_v)
	//                                    setPtrValue(midi_pos_ref, midi_pos + vec3(3, 0, 0)) 
	//                                }
	//                                else
	//                                {
	//                                    midi_n_v = 255
	//                                    setPtrValue(midi_pos_ref, midi_pos + vec3(2, 0, 0)) 
	//                                }

	//                                // stop current note if playing - shouldn't be needed, but...
	//                                stopSound(midi_c_si.get(channelIndex))

	//                                // https://discord.com/channels/409219533618806786/464440459410800644/1224463893058031778
	//                                playSound(midi_n_v / 255, pow(2, /*(*/midi_n_n /*- 60)*/ / 12), out float channel, false, SOUND_PIANO)
	//                                midi_c_si.set(channelIndex, channel)

	//                                setPtrValue(midi_t_ref, -1)
	//                            }
	//                            else if (midi_et < 2.5)
	//                            {
	//                                // stop current note
	//                                stopSound(midi_c_si.get(channelIndex))

	//                                setPtrValue(midi_pos_ref, midi_pos + vec3(1, 0, 0))
	//                                setPtrValue(midi_t_ref, -1)
	//                            }
	//                            else if (midi_et < 3.5)
	//                            {
	//                                // set instrument, nop for now
	//                                setPtrValue(midi_pos_ref, midi_pos + vec3(2, 0, 0))
	//                                setPtrValue(midi_t_ref, -1)
	//                            }
	//                            else
	//                            {
	//                                // prevent infinite loop
	//                                setPtrValue(midi_pos_ref, midi_pos + vec3(1, 0, 0))
	//                                setPtrValue(midi_t_ref, -1)
	//                            }
	//                        }
	//                    }
	//                }
	//            }

	//            func readBinary(vec3 pos, float len, out float value)
	//            {
	//                float y = pos.y + 0.5//0.9375

	//                value = 0
	//                float bitVal = 1

	//                on Loop(0, len, out inline float i)
	//                {
	//                    raycast(vec3(pos.x, y + 0.625/*0.125*/, pos.z), vec3(pos.x, y, pos.z), out bool didHit, out _, out _)

	//                    if (didHit)
	//                        value += bitVal

	//                    y++
	//                    bitVal *= 2
	//                }

	//                value = round(value)
	//            }
	//            """));

	//            Compilation compilation = Compilation.CreateScript(null, tree);

	//            // TODO: Replace with Debug.Assert lenght == 0
	//            var diagnostics = compilation.Emit(new TowerCodePlacer(context.Builder), context.Builder);
	//            if (diagnostics.Length != 0)
	//                throw new Exception(diagnostics[0].ToString());

	//            Block originBlock = new Block(int3.Zero, new BlockDef(string.Empty, 512, BlockType.NonScript, new int2(1, 1)));

	//            context.Builder.AddBlockSegments(
	//                new Block[] { originBlock }
	//                    .Concat(StockBlocks.Select(pos => new Block(pos, StockBlocks.Shrub)))
	//            );

	//            return connector.Store;
	//        });
	#endregion

	private static readonly Namespace BuiltinNamespace = new Namespace("builtin");

	private static IEnumerable<FunctionSymbol>? functionsCache;

	static BuiltinFunctions()
	{
		FunctionToDoc = typeof(BuiltinFunctions)
			.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Static)
			.SelectMany(([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type) => type
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType == typeof(FunctionSymbol))
				.Select(f =>
				{
					FunctionSymbol func = (FunctionSymbol)f.GetValue(null)!;
					FunctionDocAttribute? attrib = f.GetCustomAttribute<FunctionDocAttribute>();

					return attrib is null ? throw new Exception($"Field \"{f.Name}\" is missing {nameof(FunctionDocAttribute)}") : (func, attrib);
				}))
			.Concat(typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType == typeof(FunctionSymbol))
				.Select(f =>
				{
					FunctionSymbol func = (FunctionSymbol)f.GetValue(null)!;
					FunctionDocAttribute? attrib = f.GetCustomAttribute<FunctionDocAttribute>();

					return attrib is null ? throw new Exception($"Field \"{f.Name}\" is missing {nameof(FunctionDocAttribute)}") : (func, attrib);
				}))
			.ToDictionary()
			.AsReadOnly();
	}

	public static void Init()
	{
		RuntimeHelpers.RunClassConstructor(typeof(BuiltinFunctions).TypeHandle);
		RuntimeHelpers.RunClassConstructor(typeof(Control).TypeHandle);
		RuntimeHelpers.RunClassConstructor(typeof(Math).TypeHandle);
	}

	internal static IEnumerable<FunctionSymbol> GetAll()
		=> functionsCache ??= typeof(BuiltinFunctions)
		.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Static)
		.SelectMany(([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type) => type
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f.FieldType == typeof(FunctionSymbol))
			.Select(f => (FunctionSymbol)f.GetValue(null)!))
		.Concat(typeof(BuiltinFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f.FieldType == typeof(FunctionSymbol))
			.Select(f => (FunctionSymbol)f.GetValue(null)!));

	#region Helper functions

	// (A) - active block (has before and after), num - numb inputs, num - number outputs
	private static ITerminalStore EmitAX0(BoundCallExpression call, IEmitContext context, BlockDef blockDef, int argumentOffset = 0, Type[]? constantTypes = null)
	{
		constantTypes ??= [];

		Block block = context.AddBlock(blockDef);

		int argLength = call.Arguments.Length - constantTypes.Length;
		Debug.Assert(argLength >= 0, "Number of arguments constant types must be less than or equal to the total number of arguments.");

		if (argLength != 0)
		{
			using (context.ExpressionBlock())
			{
				for (int i = 0; i < argLength; i++)
				{
					context.Connect(context.EmitExpression(call.Arguments[i]), TerminalStore.CIn(block, block.Type.Terminals[argLength - i + argumentOffset]));
				}
			}
		}

		if (constantTypes.Length != 0)
		{
			object?[]? constants = context.ValidateConstants(call.Arguments.AsMemory(^constantTypes.Length..), true);

			if (constants is null)
			{
				return NopTerminalStore.Instance;
			}

			for (int i = 0; i < constantTypes.Length; i++)
			{
				object? val = constants[i];
				Type type = constantTypes[i];

				if (val is null)
				{
					if (type == typeof(string))
					{
						val = string.Empty;
					}
					else if (type == typeof(Rotation))
					{
						val = new Rotation(default);
					}
					else
					{
						Debug.Assert(type.IsValueType, "Types that are not string or Rotation must be a value type.");
						val = RuntimeHelpers.GetUninitializedObject(type);
					}
				}
				else
				{
					val = Convert.ChangeType(val, type);
				}

				context.SetSetting(block, i, val);
			}
		}

		return new TerminalStore(block);
	}

	private static ITerminalStore EmitAXX(BoundCallExpression call, IEmitContext context, int numbReturnArgs, BlockDef blockDef)
	{
		if (numbReturnArgs <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
		}
		else if (numbReturnArgs > call.Arguments.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
		}

		int retArgStart = call.Arguments.Length - numbReturnArgs;

		Block? block = context.AddBlock(blockDef);

		TerminalConnector connector = new TerminalConnector(context.Connect);
		connector.Add(new TerminalStore(block));

		if (retArgStart != 0)
		{
			using (context.ExpressionBlock())
			{
				for (int i = 0; i < retArgStart; i++)
				{
					context.Connect(context.EmitExpression(call.Arguments[i]), TerminalStore.CIn(block, block.Type.Terminals[retArgStart - i + numbReturnArgs]));
				}
			}
		}

		using (context.StatementBlock())
		{
			for (int i = retArgStart; i < call.Arguments.Length; i++)
			{
				VariableSymbol variable = ((BoundVariableExpression)call.Arguments[i]).Variable;

				ITerminalStore varStore = context.EmitSetVariable(variable, () => TerminalStore.COut(block, block.Type.Terminals[call.Arguments.Length - i]));

				connector.Add(varStore);
			}
		}

		return connector.Store;
	}

	private static ITerminalStore EmitXX(BoundCallExpression call, IEmitContext context, int numbReturnArgs, BlockDef blockDef)
	{
		if (numbReturnArgs <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
		}
		else if (numbReturnArgs > call.Arguments.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(numbReturnArgs));
		}

		int retArgStart = call.Arguments.Length - numbReturnArgs;

		TerminalConnector connector = new TerminalConnector(context.Connect);

		Block? block = null;

		for (int i = retArgStart; i < call.Arguments.Length; i++)
		{
			VariableSymbol variable = ((BoundVariableExpression)call.Arguments[i]).Variable;

			ITerminalStore varStore;

#pragma warning disable IDE0045 // Convert to conditional expression - why here ???
			if (block is null)
			{
				varStore = context.EmitSetVariable(variable, () =>
				{
					using (context.ExpressionBlock())
					{
						block = context.AddBlock(blockDef);

						if (retArgStart != 0)
						{
							using (context.ExpressionBlock())
							{
								for (int i = 0; i < retArgStart; i++)
								{
									context.Connect(context.EmitExpression(call.Arguments[i]), TerminalStore.CIn(block, block.Type.Terminals[(retArgStart - 1) - i + numbReturnArgs]));
								}
							}
						}
					}

					return TerminalStore.COut(block, block.Type.Terminals[(call.Arguments.Length - 1) - i]);
				});
			}
			else
			{
				varStore = context.EmitSetVariable(variable, () => TerminalStore.COut(block, block.Type.Terminals[(call.Arguments.Length - 1) - i]));
			}
#pragma warning restore IDE0045 // Convert to conditional expression

			connector.Add(varStore);
		}

		return connector.Store;
	}

	private static TerminalStore EmitX1(BoundCallExpression call, IEmitContext context, BlockDef blockDef)
	{
		Block block = context.AddBlock(blockDef);

		if (call.Arguments.Length != 0)
		{
			using (context.ExpressionBlock())
			{
				for (int i = 0; i < call.Arguments.Length; i++)
				{
					context.Connect(context.EmitExpression(call.Arguments[i]), TerminalStore.CIn(block, block.Type.Terminals[call.Arguments.Length - i]));
				}
			}
		}

		return TerminalStore.COut(block, block.Type.Terminals[0]);
	}
	#endregion
}
