using FancadeLoaderLib;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting;
using FancadeLoaderLib.Editing.Scripting.Builders;
using FancadeLoaderLib.Editing.Scripting.Placers;
using FancadeLoaderLib.Editing.Scripting.Terminals;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FancadeLoaderLib.Editing.Scripting.Utils;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FanScript.Tests;

public class FCBlocksTests
{
	[Fact]
	public void IDsDoNotRepeated()
	{
		HashSet<ushort> ids = [];

		foreach (var def in GetBlockDefs())
		{
			if (!ids.Add(def.Prefab.Id))
			{
				Assert.Fail($"Id {def.Prefab.Id} has been encountered multiple times");
			}
		}
	}

	[Fact]
	public void Manual_TerminalsAreCorrect()
	{
		var defBlocks = GetBlockDefs().ToArray();
		var active = defBlocks.Where(def => def.BlockType == BlockType.Active);
		var pasive = defBlocks.Where(def => def.BlockType != BlockType.Active);

		FrozenDictionary<(TerminalType, WireType), (BlockDef, TerminalDef)> terminalDict = new Dictionary<(TerminalType, WireType), (BlockDef, TerminalDef)>(generateTerminalDict()).ToFrozenDictionary();

		BlockBuilder builder = new GameFileBlockBuilder(null, "Test level", PrefabType.Level);
		GroundCodePlacer placer = new GroundCodePlacer(builder);

		using (placer.StatementBlock())
		{
			TerminalConnector connector = new TerminalConnector(builder.Connect);

			foreach (var def in active)
			{
				Block block = placeAndConnectAllTerminals(def);

				connector.Add(new TerminalStore(block));
			}

			foreach (var def in pasive)
				placeAndConnectAllTerminals(def);
		}

		Game game = (Game)builder.Build(int3.Zero);

		using (var stream = new FileStream("correct_terminals_output_check_manually", FileMode.Create, FileAccess.Write))
		{
			game.SaveCompressed(stream);
		}

		Block placeAndConnectAllTerminals(BlockDef def)
		{
			int off = def.BlockType == BlockType.Active ? 1 : 0;
			if (def.Terminals.Length - off * 2 <= 0)
				return placer.PlaceBlock(def);

			ReadOnlySpan<TerminalDef> terminals = def.Terminals.AsSpan(off..^off);

			(Block, TerminalDef)[] connectToTerminals = new (Block, TerminalDef)[terminals.Length];

			Block block = placer.PlaceBlock(def);

			using (placer.ExpressionBlock())
			{
				for (int i = terminals.Length - 1; i >= 0; i--)
				{
					TerminalDef terminal = terminals[i];

					WireType type = terminal.WireType;

					var (_def, _terminal) = terminalDict[(terminal.Type, type)];
					connectToTerminals[i] = (placer.PlaceBlock(_def), _terminal);
				}
			}

			for (int i = 0; i < connectToTerminals.Length; i++)
			{
				var (_block, _terminal) = connectToTerminals[i];
				if (terminals[i].Type == TerminalType.In)
				{
					builder.Connect(
						new BlockTerminal(_block, _terminal),
						new BlockTerminal(block, terminals[i])
					);
				}
				else
				{
					builder.Connect(
						new BlockTerminal(block, terminals[i]),
						new BlockTerminal(_block, _terminal)
					);
				}
			}

			return block;
		}

		IEnumerable<KeyValuePair<(TerminalType, WireType), (BlockDef, TerminalDef)>> generateTerminalDict()
		{
			foreach (var type in Enum.GetValues<WireType>())
			{
				if (type == WireType.Error)
					continue;

				BlockDef def = type == WireType.Void ? StockBlocks.Variables.Set_Variable_Num : StockBlocks.Variables.GetVariableByType(type);

				WireType ptrType = type.ToPointer();

				var terminal = def.Terminals.First(term => term.Type == TerminalType.Out && term.WireType == ptrType);

				yield return new KeyValuePair<(TerminalType, WireType), (BlockDef, TerminalDef)>((TerminalType.In, type), (def, terminal));
			}

			// this *could* be names "type", but for some fuckinf reason if it is, it's always WireType.Error
			foreach (var type in Enum.GetValues<WireType>())
			{
				if (type == WireType.Error)
					continue;

				BlockDef def = type == WireType.Void ? StockBlocks.Variables.Set_Variable_Num : StockBlocks.Variables.SetVariableByType(type);

				WireType nonPtrType = type.ToNotPointer();

				var terminal = def.Terminals.First(term => term.Type == TerminalType.In && term.WireType == nonPtrType);

				yield return new KeyValuePair<(TerminalType, WireType), (BlockDef, TerminalDef)>((TerminalType.Out, type), (def, terminal));
			}
		}
	}

	#region Utils
	private static IEnumerable<BlockDef> GetBlockDefs()
	{
		BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public;

		foreach (FieldInfo? field in
			Enumerable.Concat(
				typeof(StockBlocks).GetFields(bindingFlags),
				typeof(StockBlocks).GetNestedTypes(bindingFlags)
					.Aggregate(new List<FieldInfo>(), (list, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] type) =>
					{
						list.AddRange(type.GetFields(bindingFlags));
						return list;
					})
			)
			.Where(field => field.FieldType == typeof(BlockDef)))
		{
			yield return (BlockDef)field.GetValue(null)!;
		}
	}
	#endregion
}
