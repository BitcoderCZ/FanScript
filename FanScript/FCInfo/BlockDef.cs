// <copyright file="BlockDef.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace FanScript.FCInfo;

/// <summary>
/// Definition of a Fancade block.
/// </summary>
public class BlockDef
{
	public readonly string Name;
	public readonly ushort Id;
	public readonly BlockType Type;
	public readonly int3 Size;

	public readonly ImmutableArray<Terminal> TerminalArray;

	public readonly Indexable<string, Terminal> Terminals;

	public BlockDef(string name, ushort id, BlockType type, int3 size, params Terminal[] terminals)
	{
		Name = name;
		Id = id;
		Type = type;
		Size = size;

		TerminalArray = terminals is null ? [] : [.. terminals];

		InitTerminals();

		Terminals = new Indexable<string, Terminal>(termName =>
		{
			for (int i = 0; i < TerminalArray.Length; i++)
			{
				if (TerminalArray[i].Name == termName)
				{
					return TerminalArray[i];
				}
			}

			throw new KeyNotFoundException($"Terminal '{termName}' wasn't found.");
		});
	}

	public BlockDef(string name, ushort id, BlockType type, int2 size, params Terminal[] terminals)
	{
		Name = name;
		Id = id;
		Type = type;
		Size = new int3(size.X, 1, size.Y);

		TerminalArray = terminals is null ? [] : [.. terminals];

		InitTerminals();

		Terminals = new Indexable<string, Terminal>(termName =>
		{
			for (int i = 0; i < TerminalArray.Length; i++)
			{
				if (TerminalArray[i].Name == termName)
				{
					return TerminalArray[i];
				}
			}

			throw new KeyNotFoundException($"Terminal '{termName}' wasn't found.");
		});
	}

	public bool IsGroup => Size != int3.One;

	public Terminal Before => Type == BlockType.Active ? TerminalArray.Get(^1) : throw new InvalidOperationException("Only active blocks have Before and After");

	public Terminal After => Type == BlockType.Active ? TerminalArray[0] : throw new InvalidOperationException("Only active blocks have Before and After");

	public static bool operator ==(BlockDef a, BlockDef b)
		=> a?.Equals(b) ?? b is null;

	public static bool operator !=(BlockDef a, BlockDef b)
		=> !a?.Equals(b) ?? b is not null;

	public Terminal GetTerminal(string name)
	{
		foreach (Terminal terminal in TerminalArray)
		{
			if (terminal.Name == name)
			{
				return terminal;
			}
		}

		throw new KeyNotFoundException($"Terminal with name '{name}' isn't on block '{Name}'");
	}

	public override string ToString()
		=> $"{{Name: {Name}, Id: {Id}, Type: {Type}, Size: {Size}}}";

	public override int GetHashCode()
		=> Id;

	public override bool Equals(object? obj)
		=> obj is BlockDef other && other.Id == Id;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(BlockDef other)
		=> other is not null && other.Id == Id;

	private void InitTerminals()
	{
		int off = Type == BlockType.Active ? 1 : 0;

		int countIn = 0;
		int countOut = 0;

		// count in and out terminals
		for (int i = off; i < TerminalArray.Length - off; i++)
		{
			if (TerminalArray[i].Type == TerminalType.In)
			{
				countIn++;
			}
			else
			{
				countOut++;
			}
		}

		// if a block has less/more in/out terminals, one of the sides will start higher
		countIn = Size.Z - countIn;
		countOut = Size.Z - countOut;

		int outXPos = (Size.X * 8) - 2;

		for (int i = off; i < TerminalArray.Length - off; i++)
		{
			Terminal terminal = TerminalArray[i];

			if (terminal.Type == TerminalType.In)
			{
				terminal.Init(i, new int3(0, 1, (countIn++ * 8) + 3));
			}
			else
			{
				terminal.Init(i, new int3(outXPos, 1, (countOut++ * 8) + 3));
			}
		}

		if (Type == BlockType.Active)
		{
			After.Init(0, new int3(3, 1, 0));
			Before.Init(TerminalArray.Length - 1, new int3(3, 1, (Size.Z * 8) - 2));
		}
	}
}
