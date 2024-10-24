using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using System.CodeDom.Compiler;

namespace FanScript.Compiler.Binding
{
    internal sealed class ControlFlowGraph
    {
        private ControlFlowGraph(BasicBlock start, BasicBlock end, List<BasicBlock> blocks, List<BasicBlockBranch> branches)
        {
            Start = start;
            End = end;
            Blocks = blocks;
            Branches = branches;
        }

        public BasicBlock Start { get; }
        public BasicBlock End { get; }
        public List<BasicBlock> Blocks { get; }
        public List<BasicBlockBranch> Branches { get; }

        public sealed class BasicBlock
        {
            public BasicBlock()
            {
            }

            public BasicBlock(bool isStart)
            {
                IsStart = isStart;
                IsEnd = !isStart;
            }

            public bool IsStart { get; }
            public bool IsEnd { get; }
            public List<BoundStatement> Statements { get; } = new List<BoundStatement>();
            public List<BasicBlockBranch> Incoming { get; } = new List<BasicBlockBranch>();
            public List<BasicBlockBranch> Outgoing { get; } = new List<BasicBlockBranch>();

            public override string ToString()
            {
                if (IsStart)
                    return "<Start>";

                if (IsEnd)
                    return "<End>";

                using (StringWriter writer = new StringWriter())
                using (IndentedTextWriter indentedWriter = new IndentedTextWriter(writer))
                {
                    foreach (BoundStatement statement in Statements)
                        statement.WriteTo(indentedWriter);

                    return writer.ToString();
                }
            }
        }

        public sealed class BasicBlockBranch
        {
            public BasicBlockBranch(BasicBlock from, BasicBlock to, BoundExpression? condition)
            {
                From = from;
                To = to;
                Condition = condition;
            }

            public BasicBlock From { get; }
            public BasicBlock To { get; }
            public BoundExpression? Condition { get; }

            public override string ToString()
            {
                if (Condition is null)
                    return string.Empty;

                return Condition.ToString();
            }
        }

        public sealed class BasicBlockBuilder
        {
            private List<BoundStatement> statements = new List<BoundStatement>();
            private List<BasicBlock> blocks = new List<BasicBlock>();

            public List<BasicBlock> Build(BoundBlockStatement block)
            {
                foreach (var statement in block.Statements)
                {
                    switch (statement)
                    {
                        case BoundLabelStatement:
                            startBlock();
                            statements.Add(statement);
                            break;
                        case BoundGotoStatement:
                        case BoundEventGotoStatement:
                        case BoundConditionalGotoStatement:
                        case BoundReturnStatement:
                            statements.Add(statement);
                            startBlock();
                            break;
                        case BoundNopStatement:
                        case BoundEmitterHintStatement:
                        case BoundVariableDeclarationStatement:
                        case BoundAssignmentStatement:
                        case BoundPostfixStatement:
                        case BoundPrefixStatement:
                        case BoundCallStatement:
                        case BoundExpressionStatement:
                            statements.Add(statement);
                            break;
                        default:
                            throw new UnexpectedBoundNodeException(statement);
                    }
                }

                endBlock();

                return blocks.ToList();
            }

            private void startBlock()
            {
                endBlock();
            }

            private void endBlock()
            {
                if (statements.Count > 0)
                {
                    BasicBlock block = new BasicBlock();
                    block.Statements.AddRange(statements);
                    blocks.Add(block);
                    statements.Clear();
                }
            }
        }

        public sealed class GraphBuilder
        {
            private Dictionary<BoundStatement, BasicBlock> blockFromStatement = new Dictionary<BoundStatement, BasicBlock>();
            private Dictionary<BoundLabel, BasicBlock> blockFromLabel = new Dictionary<BoundLabel, BasicBlock>();
            private List<BasicBlockBranch> branches = new List<BasicBlockBranch>();
            private BasicBlock start = new BasicBlock(isStart: true);
            private BasicBlock end = new BasicBlock(isStart: false);

            public ControlFlowGraph Build(List<BasicBlock> blocks)
            {
                if (!blocks.Any())
                    connect(start, end);
                else
                    connect(start, blocks.First());

                foreach (BasicBlock block in blocks)
                {
                    foreach (BoundStatement statement in block.Statements)
                    {
                        blockFromStatement.Add(statement, block);
                        if (statement is BoundLabelStatement labelStatement)
                            blockFromLabel.Add(labelStatement.Label, block);
                    }
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    BasicBlock current = blocks[i];
                    BasicBlock next = i == blocks.Count - 1 ? end : blocks[i + 1];

                    foreach (BoundStatement statement in current.Statements)
                    {
                        bool isLastStatementInBlock = statement == current.Statements.Last();
                        switch (statement)
                        {
                            case BoundGotoStatement gotoStatement:
                                {
                                    BasicBlock toBlock = blockFromLabel[gotoStatement.Label];
                                    connect(current, toBlock);
                                }
                                break;
                            case BoundEventGotoStatement eventGotoStatement:
                                {
                                    BasicBlock thenBlock = blockFromLabel[eventGotoStatement.Label];
                                    BasicBlock elseBlock = next;
                                    connect(current, thenBlock, null);
                                    connect(current, elseBlock, null);
                                }
                                break;
                            case BoundConditionalGotoStatement conditionalGotoStatement:
                                {
                                    BasicBlock thenBlock = blockFromLabel[conditionalGotoStatement.Label];
                                    BasicBlock elseBlock = next;

                                    BoundExpression negatedCondition = Negate(conditionalGotoStatement.Condition);
                                    BoundExpression thenCondition = conditionalGotoStatement.JumpIfTrue ? conditionalGotoStatement.Condition : negatedCondition;
                                    BoundExpression elseCondition = conditionalGotoStatement.JumpIfTrue ? negatedCondition : conditionalGotoStatement.Condition;
                                    connect(current, thenBlock, thenCondition);
                                    connect(current, elseBlock, elseCondition);
                                }
                                break;
                            case BoundReturnStatement:
                                connect(current, end);
                                break;
                            case BoundNopStatement:
                            case BoundEmitterHintStatement:
                            case BoundVariableDeclarationStatement:
                            case BoundAssignmentStatement:
                            case BoundPostfixStatement:
                            case BoundPrefixStatement:
                            case BoundLabelStatement:
                            case BoundCallStatement:
                            case BoundExpressionStatement:
                                {
                                    if (isLastStatementInBlock)
                                        connect(current, next);
                                }
                                break;
                            default:
                                throw new UnexpectedBoundNodeException(statement);
                        }
                    }
                }

            ScanAgain:
                foreach (BasicBlock block in blocks)
                {
                    if (!block.Incoming.Any())
                    {
                        removeBlock(blocks, block);
                        goto ScanAgain;
                    }
                }

                blocks.Insert(0, start);
                blocks.Add(end);

                return new ControlFlowGraph(start, end, blocks, branches);
            }

            private void connect(BasicBlock from, BasicBlock to, BoundExpression? condition = null)
            {
                if (condition is BoundLiteralExpression l)
                {
                    bool value = (bool)l.ConstantValue.GetValueOrDefault(TypeSymbol.Bool);
                    if (value)
                        condition = null;
                    else
                        return;
                }

                BasicBlockBranch branch = new BasicBlockBranch(from, to, condition);
                from.Outgoing.Add(branch);
                to.Incoming.Add(branch);
                branches.Add(branch);
            }

            private void removeBlock(List<BasicBlock> blocks, BasicBlock block)
            {
                foreach (BasicBlockBranch branch in block.Incoming)
                {
                    branch.From.Outgoing.Remove(branch);
                    branches.Remove(branch);
                }

                foreach (BasicBlockBranch branch in block.Outgoing)
                {
                    branch.To.Incoming.Remove(branch);
                    branches.Remove(branch);
                }

                blocks.Remove(block);
            }

            private BoundExpression Negate(BoundExpression condition)
            {
                BoundUnaryExpression negated = BoundNodeFactory.Not(condition.Syntax, condition);
                if (negated.ConstantValue is not null)
                    return new BoundLiteralExpression(condition.Syntax, negated.ConstantValue.Value);

                return negated;
            }
        }

        public void WriteTo(TextWriter writer)
        {
            string Quote(string text)
            {
                return "\"" + text.TrimEnd().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(Environment.NewLine, "\\l") + "\"";
            }

            writer.WriteLine("digraph G {");

            Dictionary<BasicBlock, string> blockIds = new Dictionary<BasicBlock, string>();

            for (int i = 0; i < Blocks.Count; i++)
            {
                string id = $"N{i}";
                blockIds.Add(Blocks[i], id);
            }

            foreach (var block in Blocks)
            {
                string id = blockIds[block];
                string label = Quote(block.ToString());
                writer.WriteLine($"    {id} [label = {label}, shape = box]");
            }

            foreach (var branch in Branches)
            {
                string fromId = blockIds[branch.From];
                string toId = blockIds[branch.To];
                string label = Quote(branch.ToString());
                writer.WriteLine($"    {fromId} -> {toId} [label = {label}]");
            }

            writer.WriteLine("}");
        }

        public static ControlFlowGraph Create(BoundBlockStatement body)
        {
            BasicBlockBuilder basicBlockBuilder = new BasicBlockBuilder();
            List<BasicBlock> blocks = basicBlockBuilder.Build(body);

            GraphBuilder graphBuilder = new GraphBuilder();
            return graphBuilder.Build(blocks);
        }

        public static bool AllPathsReturn(BoundBlockStatement body)
        {
            ControlFlowGraph graph = Create(body);

            foreach (BasicBlockBranch branch in graph.End.Incoming)
            {
                BoundStatement? lastStatement = branch.From.Statements.LastOrDefault();
                if (lastStatement is null || lastStatement.Kind != BoundNodeKind.ReturnStatement)
                    return false;
            }

            return true;
        }
    }
}
