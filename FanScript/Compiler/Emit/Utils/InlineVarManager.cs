// <copyright file="InlineVarManager.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting;
using FancadeLoaderLib.Editing.Scripting.TerminalStores;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static FancadeLoaderLib.Editing.StockBlocks;

namespace FanScript.Compiler.Emit.Utils;

internal sealed class InlineVarManager
{
	private readonly Dictionary<VariableSymbol, Entry> _dict = [];

	public void Set(VariableSymbol variable, ITerminalStore store)
	{
		Debug.Assert(variable.Modifiers.HasFlag(Modifiers.Inline), "Only inline variables can be set.");

		_dict[variable] = new Entry(store);
	}

	public bool TryGet(VariableSymbol variable, IEmitContext context, [NotNullWhen(true)] out ITerminalStore? store)
	{
		if (!_dict.TryGetValue(variable, out var entry))
		{
			store = NopTerminalStore.Instance;
			return true;
		}

		if (entry.UseCount + 1 < FancadeConstants.MaxWireSplits)
		{
			store = entry.GetStore();
			return true;
		}

		WireType type = entry.GetStoreType();
		if (type == WireType.Error)
		{
			type = variable.Type.ToWireType();
		}

		var passthrough = GetPassthrough(type);

		if (passthrough is null)
		{
			if (entry.UseCount + 1 <= FancadeConstants.MaxWireSplits)
			{
				store = entry.GetStore();
				return true;
			}

			store = null;
			return false;
		}

		Block passthroughBlock = context.AddBlock(passthrough.Value.Def);

		context.Connect(entry.GetStore(), TerminalStore.CIn(passthroughBlock, passthrough.Value.In));

		entry = new Entry(TerminalStore.COut(passthroughBlock, passthrough.Value.Out));
		_dict[variable] = entry;
		store = entry.GetStore();
		return true;
	}

	private static (BlockDef Def, TerminalDef In, TerminalDef Out)? GetPassthrough(WireType type)
	{
		switch (type)
		{
			case WireType.Bool:
				return (StockBlocks.Math.LogicalOr, StockBlocks.Math.LogicalOr["Tru1"], StockBlocks.Math.LogicalOr["Tru1 | Tru2"]);
			case WireType.Float:
				return (StockBlocks.Math.Add_Number, StockBlocks.Math.Add_Number["Num1"], StockBlocks.Math.Add_Number["Num1 + Num2"]);
			case WireType.Vec3:
				return (StockBlocks.Math.Add_Vector, StockBlocks.Math.Add_Vector["Vec1"], StockBlocks.Math.Add_Vector["Vec1 + Vec2"]);
			case WireType.Rot:
				return (StockBlocks.Math.Multiply_Rotation, StockBlocks.Math.Multiply_Rotation["Rot1"], StockBlocks.Math.Multiply_Rotation["Rot1 * Rot2"]);
			default:
				{
					if (type.IsPointer())
					{
						BlockDef def = Variables.ListByType(type);
						return (def, def["Variable"], def["Element"]);
					}

					return null;
				}
		}
	}

	private class Entry
	{
		public int UseCount;
		private readonly ITerminalStore _store;

		public Entry(ITerminalStore store)
		{
			_store = store;
		}

		public ITerminalStore GetStore()
		{
			UseCount++;
			Debug.Assert(UseCount <= FancadeConstants.MaxWireSplits, $"A terminals shouldn't be used more times than wire split limit ({FancadeConstants.MaxWireSplits}).");
			return _store;
		}

		public WireType GetStoreType()
			=> _store.Out.IsEmpty ? WireType.Error : _store.Out[0].WireType;
	}
}
