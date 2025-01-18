// <copyright file="IConnectTarget.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.Tests")]

namespace FanScript.Compiler.Emit;

public interface IConnectTarget
{
	int3 Pos { get; }

	int TerminalIndex { get; }

	int3? VoxelPos { get; }
}

internal sealed class NopConnectTarget : IConnectTarget
{
	public static readonly NopConnectTarget Instance = new NopConnectTarget();

	private NopConnectTarget()
	{
	}

	public int3 Pos => new int3(-1, -1, -1);

	public int TerminalIndex => -1;

	public int3? VoxelPos => new int3(-1, -1, -1);
}

internal sealed class BlockConnectTarget : IConnectTarget
{
	public readonly Block Block;
	public readonly Terminal Terminal;

	public BlockConnectTarget(Block block, Terminal terminal)
	{
		Block = block;
		Terminal = terminal;
	}

	public int3 Pos => Block.Pos;

	public int TerminalIndex => Terminal.Index;

	public int3? VoxelPos => Terminal.Pos;
}

internal sealed class BlockVoxelConnectTarget : IConnectTarget
{
	public readonly Block Block;

	public BlockVoxelConnectTarget(Block block, int3? voxelPos = null)
	{
		Block = block;
		VoxelPos = voxelPos ?? new int3(7, 3, 3);
	}

	public int3 Pos => Block.Pos;

	public int TerminalIndex { get; init; }

	public int3? VoxelPos { get; init; }
}

internal sealed class AbsoluteConnectTarget : IConnectTarget
{
	public AbsoluteConnectTarget(int3 pos, int3? voxelPos = null)
	{
		Pos = pos;
		VoxelPos = voxelPos;
	}

	public int3 Pos { get; init; }

	public int TerminalIndex { get; init; }

	public int3? VoxelPos { get; init; }
}

#pragma warning disable SA1204 // Static elements should appear before instance elements
internal static class ConnectTargetE
#pragma warning restore SA1204
{
	public static WireType GetWireType(this IConnectTarget connectTarget)
		=> connectTarget switch
		{
			BlockConnectTarget blockTarget => blockTarget.Terminal.WireType,
			_ => WireType.Error,
		};
}
