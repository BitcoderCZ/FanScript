using FanScript.FCInfo;
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

        public static IEnumerable<object[]> GetBlockDefs()
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
                yield return [field.GetValue(null)!];
        }

        private static void loadBlocks()
        {
            if (!Blocks.PositionsLoaded)
                Blocks.LoadPositions();
        }
    }
}
