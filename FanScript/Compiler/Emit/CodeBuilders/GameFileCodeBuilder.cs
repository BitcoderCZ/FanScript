using FancadeLoaderLib;
using FanScript.FCInfo;
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
            int levelIndex = (args?.FirstOrDefault(arg => arg is int) as int?) ?? 0;

            if (game.Levels.Count == 0)
                game.Levels.Add(new Level("Level 1"));

            levelIndex = Math.Clamp(levelIndex, 0, game.Levels.Count - 1);

            if (!Blocks.PositionsLoaded)
                Blocks.LoadPositions();

            PreBuild(startPos);

            BlockList builtInBlocks;
            using (SaveReader reader = new SaveReader("baseBlocks.fcbl")) // fcbl - Fancade block list
                builtInBlocks = BlockList.Load(reader);

            Level level = game.Levels[levelIndex];

            for (int i = 0; i < blocks.Count; i++)
            {
                FCInfo.Block block = blocks[i];
                level.BlockIds.SetBlock(block.Pos, builtInBlocks.GetBlock(block.Type.Id));
            }

            for (int i = 0; i < values.Count; i++)
            {
                ValueRecord set = values[i];
                level.BlockValues.Add(new BlockValue()
                {
                    ValueIndex = (byte)set.ValueIndex,
                    Type = (byte)(set.Value switch
                    {
                        byte => 1,
                        ushort => 2,
                        float => 4,
                        Vector3F => 5,
                        Rotation => 5,
                        string => 6,
                        _ => throw new InvalidDataException($"Unsupported type of value: '{set.Value.GetType()}'."),
                    }),
                    Position = (Vector3US)set.Block.Pos,
                    Value = set.Value is Rotation rot ? rot.Value : set.Value,
                });
            }

            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionRecord con = connections[i];
                level.Connections.Add(new Connection()
                {
                    From = (Vector3US)con.From.Pos,
                    FromConnector = (Vector3US)(con.From.SubPos ?? ChooseSubPos(con.From.Pos)),
                    To = (Vector3US)con.To.Pos,
                    ToConnector = (Vector3US)(con.To.SubPos ?? ChooseSubPos(con.To.Pos)),
                });
            }

            return game;
        }
    }
}
