﻿using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Reflection;

namespace FanScript.Tests
{
    public class FCBlocksTests
    {
        [Theory]
        [MemberData(nameof(GetBlockDefs))]
        public void FCBlocks_InitializedConnectorPositions(BlockDef blockDef)
        {
            foreach (var terminal in blockDef.Terminals)
                Assert.NotEqual(terminal.Pos, Vector3I.Zero);
        }

        [Fact]
        public void FCBlocks_IDsDoNotRepeated()
        {
            HashSet<ushort> ids = new();

            foreach (var def in getBlockDefs())
                if (!ids.Add(def.Id))
                    Assert.Fail($"Id {def.Id} has been encountered multiple times");
        }

        public static IEnumerable<object[]> GetBlockDefs()
            => getBlockDefs()
            .Select(def => new object[] { def });

        #region Utils
        private static IEnumerable<BlockDef> getBlockDefs()
        {
            loadBlocks();

            BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public;

            foreach (FieldInfo? field in
                Enumerable.Concat(
                    typeof(Blocks).GetFields(bindingFlags),
                    typeof(Blocks).GetNestedTypes(bindingFlags)
                        .Aggregate(new List<FieldInfo>(), (list, type) =>
                        {
                            list.AddRange(type.GetFields(bindingFlags));
                            return list;
                        })
                )
                .Where(field => field.FieldType == typeof(BlockDef))
            )
                yield return (BlockDef)field.GetValue(null)!;
        }
        private static void loadBlocks()
        {
            if (!Blocks.PositionsLoaded)
                Blocks.LoadPositions();
        }
        #endregion
    }
}