// <copyright file="GameFileBlockBuilder.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FancadeLoaderLib;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Partial;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Diagnostics.CodeAnalysis;

/*
log("Block id: " + getBlock(0, 0, 0));
for (var i = 0; i < 10; i++) {
    if (getTerminalName(0, 0, 0, i) == "")
        continue;
    log("Name: " + getTerminalName(0, 0, 0, i) + ", Index: " + i + ", Type: " + getTerminalType(0, 0, 0, i));
}
*/

namespace FanScript.Compiler.Emit.BlockBuilders;

public class GameFileBlockBuilder : BlockBuilder, IConnectToBlocksBuilder
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="startPos"></param>
	/// <param name="iArgs">Must be null or <see cref="Args"/></param>
	/// <returns>The <see cref="Game"/> object that was written to.</returns>
	/// <exception cref="InvalidDataException"></exception>
	public override Game Build(int3 startPos, IArgs? iArgs)
	{
		Args args = (iArgs as Args) ?? Args.Default;

		Game game;
		if (string.IsNullOrEmpty(args.InGameFile))
		{
			game = new Game("FanScript");
		}
		else
		{
			using FileStream fs = File.OpenRead(args.InGameFile);
			game = Game.LoadCompressed(fs);
		}

		Prefab prefab;
		if (args.CreateNewPrefab)
		{
			if (args.PrefabType == PrefabType.Level)
			{
				prefab = Prefab.CreateLevel(args.PrefabName);

				int index = 0;

				while (index < game.Prefabs.Count && game.Prefabs[index].Type == PrefabType.Level)
				{
					index++;
				}

				game.Prefabs.Insert(index, prefab);
			}
			else
			{
				prefab = Prefab.CreateBlock(args.PrefabName);
				prefab.Type = args.PrefabType.Value;
				prefab.Voxels = BlockVoxelsGenerator.CreateScript(int2.One).First().Value;

				game.Prefabs.Add(prefab);
			}
		}
		else
		{
			if (args.PrefabIndex < 0 || args.PrefabIndex >= game.Prefabs.Count)
			{
				throw new IndexOutOfRangeException($"PrefabIndex must be greater or equal to 0 and smaller than the number of prefabs ({game.Prefabs.Count}).");
			}

			prefab = game.Prefabs[args.PrefabIndex.Value];
		}

		Block[] blocks = PreBuild(startPos, false);

		PartialPrefabList stockPrefabs = StockPrefabs.Instance.List;

		Dictionary<ushort, PartialPrefabGroup> groupCache = [];

		for (int i = 0; i < blocks.Length; i++)
		{
			Block block = blocks[i];
			if (block.Type.IsGroup)
			{
				if (!groupCache.TryGetValue(block.Type.Id, out var group))
				{
					group = stockPrefabs.GetGroup(block.Type.Id);
					groupCache.Add(block.Type.Id, group);
				}

				prefab.Blocks.SetGroup(block.Pos, group);
			}
			else
			{
				prefab.Blocks.SetBlock(block.Pos, block.Type.Id);
			}
		}

		for (int i = 0; i < values.Count; i++)
		{
			ValueRecord set = values[i];
			prefab.Settings.Add(new PrefabSetting()
			{
				Index = (byte)set.ValueIndex,
				Type = set.Value switch
				{
					byte => SettingType.Byte,
					ushort => SettingType.Ushort,
					float => SettingType.Float,
					float3 => SettingType.Vec3,
					Rotation => SettingType.Vec3,
					string => SettingType.String,
					_ => throw new InvalidDataException($"Unsupported type of value: '{set.Value.GetType()}'."),
				},
				Position = (ushort3)set.Block.Pos,
				Value = set.Value is Rotation rot ? rot.Value : set.Value,
			});
		}

		for (int i = 0; i < connections.Count; i++)
		{
			ConnectionRecord con = connections[i];
			prefab.Connections.Add(new Connection()
			{
				From = (ushort3)con.From.Pos,
				FromVoxel = (ushort3)(con.From.VoxelPos ?? ChooseSubPos(con.From.Pos)),
				To = (ushort3)con.To.Pos,
				ToVoxel = (ushort3)(con.To.VoxelPos ?? ChooseSubPos(con.To.Pos)),
			});
		}

		return game;
	}

	public sealed class Args : IArgs
	{
		public static readonly Args Default = new Args(null, "New Block", FancadeLoaderLib.PrefabType.Script);

		public readonly string? InGameFile;

		public readonly string? PrefabName;
		public readonly PrefabType? PrefabType;

		public readonly ushort? PrefabIndex;

		public Args(string? inGameFile, string prefabName, PrefabType prefabType)
		{
			InGameFile = inGameFile;

			CreateNewPrefab = true;

			PrefabName = prefabName;
			PrefabType = prefabType;
		}

		public Args(string? inGameFile, ushort prefabIndex)
		{
			InGameFile = inGameFile;

			CreateNewPrefab = false;

			PrefabIndex = prefabIndex;
		}

		[MemberNotNullWhen(true, nameof(PrefabName), nameof(PrefabType))]
		[MemberNotNullWhen(false, nameof(PrefabIndex))]
		public bool CreateNewPrefab { get; private set; }
	}
}
