using FancadeLoaderLib;
using FancadeLoaderLib.Editing.Utils;
using MathUtils.Vectors;

/*
log("Block id: " + getBlock(0, 0, 0));
for (var i = 0; i < 10; i++) {
    if (getTerminalName(0, 0, 0, i) == "")
        continue;
    log("Name: " + getTerminalName(0, 0, 0, i) + ", Index: " + i + ", Type: " + getTerminalType(0, 0, 0, i));
}
*/

namespace FanScript.Compiler.Emit.CodeBuilders
{
    public class GameFileCodeBuilder : CodeBuilder
    {
        public override BuildPlatformInfo PlatformInfo =>
            BuildPlatformInfo.CanGetBlocks |
            BuildPlatformInfo.CanCreateCustomBlocks;

        public GameFileCodeBuilder(IBlockPlacer blockPlacer) : base(blockPlacer)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="args">In any order - [optional] <see cref="Game"/> (Game to write to), [optional] <see langword="int"/> (Index of level to write to)</param>
        /// <returns>The <see cref="Game"/> object that was written to</returns>
        /// <exception cref="InvalidDataException"></exception>
        public override object Build(Vector3I startPos, params object[] args)
        {
            Game game = (args?.FirstOrDefault(arg => arg is Game) as Game) ?? new Game("My game");
            int prefabIndex = (args?.FirstOrDefault(arg => arg is int) as int?) ?? 0;

            if (!game.Prefabs.Any())
                game.Prefabs.Add(Prefab.CreateLevel("Level 1"));

            prefabIndex = Math.Clamp(prefabIndex, 0, game.Prefabs.Count - 1);

            PreBuild(startPos);

            PrefabList builtInBlocks;
            using (FcBinaryReader reader = new FcBinaryReader("baseBlocks.fcbl")) // fcbl - Fancade block list
                builtInBlocks = PrefabList.Load(reader);

            Prefab prefab = game.Prefabs[prefabIndex];

            Dictionary<ushort, PrefabGroup> groupCache = new Dictionary<ushort, PrefabGroup>();
            
            for (int i = 0; i < blocks.Count; i++)
            {
                FCInfo.Block block = blocks[i];
                if (block.Type.IsGroup)
                {
                    if (!groupCache.TryGetValue(block.Type.Id, out var group))
                    {
                        group = builtInBlocks.GetGroupAsGroup(block.Type.Id);
                        groupCache.Add(block.Type.Id, group);
                    }

                    prefab.Blocks.SetGroup(block.Pos, group);
                }
                else
                    prefab.Blocks.SetBlock(block.Pos, block.Type.Id);
            }

            for (int i = 0; i < values.Count; i++)
            {
                ValueRecord set = values[i];
                prefab.Settings.Add(new PrefabSetting()
                {
                    Index = (byte)set.ValueIndex,
                    Type = (set.Value switch
                    {
                        byte => SettingType.Byte,
                        ushort => SettingType.Ushort,
                        float => SettingType.Float,
                        Vector3F => SettingType.Vec3,
                        Rotation => SettingType.Vec3,
                        string => SettingType.String,
                        _ => throw new InvalidDataException($"Unsupported type of value: '{set.Value.GetType()}'."),
                    }),
                    Position = (Vector3US)set.Block.Pos,
                    Value = set.Value is Rotation rot ? rot.Value : set.Value,
                });
            }

            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionRecord con = connections[i];
                prefab.Connections.Add(new Connection()
                {
                    From = (Vector3US)con.From.Pos,
                    FromVoxel = (Vector3US)(con.From.VoxelPos ?? ChooseSubPos(con.From.Pos)),
                    To = (Vector3US)con.To.Pos,
                    ToVoxel = (Vector3US)(con.To.VoxelPos ?? ChooseSubPos(con.To.Pos)),
                });
            }

            return game;
        }
    }
}
