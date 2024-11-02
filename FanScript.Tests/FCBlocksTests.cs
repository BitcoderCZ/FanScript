using FanScript.Compiler;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockPlacers;
using FanScript.Compiler.Emit.CodeBuilders;
using FanScript.Compiler.Emit.Utils;
using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FanScript.Tests
{
    public class FCBlocksTests
    {
        [Fact]
        public void IDsDoNotRepeated()
        {
            HashSet<ushort> ids = [];

            foreach (var def in GetBlockDefs())
                if (!ids.Add(def.Id))
                    Assert.Fail($"Id {def.Id} has been encountered multiple times");
        }
        [Fact]
        public void Manual_TerminalsAreCorrect()
        {
            var defBlocks = GetBlockDefs().ToArray();
            var active = defBlocks.Where(def => def.Type == BlockType.Active);
            var pasive = defBlocks.Where(def => def.Type != BlockType.Active);

            FrozenDictionary<(TerminalType, WireType), (BlockDef, Terminal)> terminalDict = new Dictionary<(TerminalType, WireType), (BlockDef, Terminal)>(generateTerminalDict()).ToFrozenDictionary();

            BlockBuilder builder = new GameFileBlockBuilder();
            CodePlacer placer = new GroundCodePlacer(builder);

            using (placer.StatementBlock())
            {
                EmitConnector connector = new EmitConnector(builder.Connect);

                foreach (var def in active)
                {
                    Block block = placeAndConnectAllTerminals(def);

                    connector.Add(new BasicEmitStore(block));
                }

                foreach (var def in pasive)
                    placeAndConnectAllTerminals(def);
            }

            FancadeLoaderLib.Game game = (FancadeLoaderLib.Game)builder.Build(Vector3I.Zero);

            using (var stream = new FileStream("correct_terminals_output_check_manually", FileMode.Create, FileAccess.Write))
                game.SaveCompressed(stream);

            Block placeAndConnectAllTerminals(BlockDef def)
            {
                int off = def.Type == BlockType.Active ? 1 : 0;
                if (def.TerminalArray.Length - off * 2 <= 0)
                    return placer.PlaceBlock(def);

                ReadOnlySpan<Terminal> terminals = def.TerminalArray.AsSpan(off..^off);

                (Block, Terminal)[] connectToTerminals = new (Block, Terminal)[terminals.Length];

                Block block = placer.PlaceBlock(def);

                using (placer.ExpressionBlock())
                {
                    for (int i = terminals.Length - 1; i >= 0; i--)
                    {
                        Terminal terminal = terminals[i];

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
                            new BlockConnectTarget(_block, _terminal),
                            new BlockConnectTarget(block, terminals[i])
                        );
                    }
                    else
                    {
                        builder.Connect(
                            new BlockConnectTarget(block, terminals[i]),
                            new BlockConnectTarget(_block, _terminal)
                        );
                    }
                }

                return block;
            }

            IEnumerable<KeyValuePair<(TerminalType, WireType), (BlockDef, Terminal)>> generateTerminalDict()
            {
                foreach (var type in Enum.GetValues<WireType>())
                {
                    if (type == WireType.Error)
                        continue;

                    BlockDef def = type == WireType.Void ? Blocks.Variables.Set_Variable_Num : Blocks.Variables.VariableByType(type);

                    WireType ptrType = type.ToPointer();

                    var terminal = def.TerminalArray.First(term => term.Type == TerminalType.Out && term.WireType == ptrType);

                    yield return new KeyValuePair<(TerminalType, WireType), (BlockDef, Terminal)>((TerminalType.In, type), (def, terminal));
                }

                // this *could* be names "type", but for some fuckinf reason if it is, it's always WireType.Error
                foreach (var type in Enum.GetValues<WireType>())
                {
                    if (type == WireType.Error)
                        continue;

                    BlockDef def = type == WireType.Void ? Blocks.Variables.Set_Variable_Num : Blocks.Variables.Set_VariableByType(type);

                    WireType nonPtrType = type.ToNormal();

                    var terminal = def.TerminalArray.First(term => term.Type == TerminalType.In && term.WireType == nonPtrType);

                    yield return new KeyValuePair<(TerminalType, WireType), (BlockDef, Terminal)>((TerminalType.Out, type), (def, terminal));
                }
            }
        }

        #region Utils
        private static IEnumerable<BlockDef> GetBlockDefs()
        {
            BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public;

            foreach (FieldInfo? field in
                Enumerable.Concat(
                    typeof(Blocks).GetFields(bindingFlags),
                    typeof(Blocks).GetNestedTypes(bindingFlags)
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
}
