using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockPlacers;
using FanScript.Compiler.Emit.CodeBuilders;
using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Collections.Frozen;
using System.Reflection;

namespace FanScript.Tests
{
    public class FCBlocksTests
    {
        [Fact]
        public void FCBlocks_IDsDoNotRepeated()
        {
            HashSet<ushort> ids = new();

            foreach (var def in getBlockDefs())
                if (!ids.Add(def.Id))
                    Assert.Fail($"Id {def.Id} has been encountered multiple times");
        }
        [Fact]
        public void FCBlocks_Manual_TerminalsAreCorrect()
        {
            var defBlocks = getBlockDefs().ToArray();
            var active = defBlocks.Where(def => def.Type == BlockType.Active);
            var pasive = defBlocks.Where(def => def.Type != BlockType.Active);

            FrozenDictionary<(TerminalType, WireType), (BlockDef, Terminal)> terminalDict = new Dictionary<(TerminalType, WireType), (BlockDef, Terminal)>(generateTerminalDict()).ToFrozenDictionary();

            CodeBuilder builder = new GameFileCodeBuilder(new GroundBlockPlacer());
            builder.BlockPlacer.EnterStatementBlock();

            Block? lastBlock = null;

            foreach (var def in active)
            {
                Block block = placeAndConnectAllTerminals(def);

                if (lastBlock is not null)
                    builder.Connect(new BlockConnectTarget(lastBlock, lastBlock.Type.After), new BlockConnectTarget(block, block.Type.Before));

                lastBlock = block;
            }

            foreach (var def in pasive)
                placeAndConnectAllTerminals(def);

            builder.BlockPlacer.ExitStatementBlock();

            FancadeLoaderLib.Game game = (FancadeLoaderLib.Game)builder.Build(Vector3I.Zero);

            using (var stream = new FileStream("correct_terminals_output_check_manually", FileMode.Create, FileAccess.Write))
                game.SaveCompressed(stream);

            Block placeAndConnectAllTerminals(BlockDef def)
            {
                int off = def.Type == BlockType.Active ? 1 : 0;
                if (def.Terminals.Length - off * 2 <= 0)
                    return builder.AddBlock(def);

                ReadOnlySpan<Terminal> terminals = def.Terminals.AsSpan(off..^off);

                (Block, Terminal)[] connectToTerminals = new (Block, Terminal)[terminals.Length];

                builder.BlockPlacer.EnterStatementBlock();
                for (int i = 0; i < terminals.Length; i++)
                {
                    Terminal terminal = terminals[i];

                    WireType type = terminal.WireType;
                    if (type != WireType.Void)
                        type = (WireType)((int)type & (int.MaxValue ^ 1)); // xPtr => x

                    var (_def, _terminal) = terminalDict[(terminal.Type, type)];
                    connectToTerminals[i] = (builder.AddBlock(_def), _terminal);
                }
                builder.BlockPlacer.ExitStatementBlock();

                Block block = builder.AddBlock(def);

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
                foreach (var _type in Enum.GetValues<WireType>())
                {
                    if (_type == WireType.Error ||
                        (_type != WireType.Void && (int)_type % 2 == 1))
                        continue;

                    BlockDef def = _type == WireType.Void ? Blocks.Variables.Set_Variable_Num : Blocks.Variables.VariableByType(_type);

                    var type = (WireType)((int)_type | 1);

                    var terminal = def.Terminals.First(term => term.Type == TerminalType.Out && term.WireType == type);

                    yield return new KeyValuePair<(TerminalType, WireType), (BlockDef, Terminal)>((TerminalType.In, _type), (def, terminal));
                }

                // this *could* be names "type", but for some fuckinf reason if it is, it's always WireType.Error
                foreach (var _type in Enum.GetValues<WireType>())
                {
                    if (_type == WireType.Error ||
                        (_type != WireType.Void && (int)_type % 2 == 1))
                        continue;

                    BlockDef def = _type == WireType.Void ? Blocks.Variables.Set_Variable_Num : Blocks.Variables.Set_VariableByType(_type);

                    var terminal = def.Terminals.First(term => term.Type == TerminalType.In && term.WireType == _type);

                    yield return new KeyValuePair<(TerminalType, WireType), (BlockDef, Terminal)>((TerminalType.Out, _type), (def, terminal));
                }
            }
        }

        #region Utils
        private static IEnumerable<BlockDef> getBlockDefs()
        {
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
        #endregion
    }
}
