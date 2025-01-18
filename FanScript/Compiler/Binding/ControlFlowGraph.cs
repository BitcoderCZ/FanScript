// <copyright file="ControlFlowGraph.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using System.CodeDom.Compiler;

namespace FanScript.Compiler.Binding;

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
			{
				return false;
			}
		}

		return true;
	}

	public void WriteTo(TextWriter writer)
	{
		string Quote(string text)
		{
			return "\"" + text.TrimEnd().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(Environment.NewLine, "\\l") + "\"";
		}

		writer.WriteLine("digraph G {");

		Dictionary<BasicBlock, string> blockIds = [];

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

		public List<BoundStatement> Statements { get; } = [];

		public List<BasicBlockBranch> Incoming { get; } = [];

		public List<BasicBlockBranch> Outgoing { get; } = [];

		public override string ToString()
		{
			if (IsStart)
			{
				return "<Start>";
			}

			if (IsEnd)
			{
				return "<End>";
			}

			using (StringWriter writer = new StringWriter())
			using (IndentedTextWriter indentedWriter = new IndentedTextWriter(writer))
			{
				foreach (BoundStatement statement in Statements)
				{
					statement.WriteTo(indentedWriter);
				}

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
			=> Condition is null ? string.Empty : Condition.ToString();
	}

	public sealed class BasicBlockBuilder
	{
		private readonly List<BoundStatement> _statements = [];
		private readonly List<BasicBlock> _blocks = [];

		public List<BasicBlock> Build(BoundBlockStatement block)
		{
			foreach (var statement in block.Statements)
			{
				switch (statement)
				{
					case BoundLabelStatement:
						StartBlock();
						_statements.Add(statement);
						break;
					case BoundGotoStatement:
					case BoundEventGotoStatement:
					case BoundConditionalGotoStatement:
					case BoundReturnStatement:
						_statements.Add(statement);
						StartBlock();
						break;
					case BoundNopStatement:
					case BoundEmitterHintStatement:
					case BoundVariableDeclarationStatement:
					case BoundAssignmentStatement:
					case BoundPostfixStatement:
					case BoundPrefixStatement:
					case BoundCallStatement:
					case BoundExpressionStatement:
						_statements.Add(statement);
						break;
					default:
						throw new UnexpectedBoundNodeException(statement);
				}
			}

			EndBlock();

			return [.. _blocks];
		}

		private void StartBlock()
			=> EndBlock();

		private void EndBlock()
		{
			if (_statements.Count > 0)
			{
				BasicBlock block = new BasicBlock();
				block.Statements.AddRange(_statements);
				_blocks.Add(block);
				_statements.Clear();
			}
		}
	}

	public sealed class GraphBuilder
	{
		private readonly Dictionary<BoundStatement, BasicBlock> _blockFromStatement = [];
		private readonly Dictionary<BoundLabel, BasicBlock> _blockFromLabel = [];
		private readonly List<BasicBlockBranch> _branches = [];
		private readonly BasicBlock _start = new BasicBlock(isStart: true);
		private readonly BasicBlock _end = new BasicBlock(isStart: false);

		public ControlFlowGraph Build(List<BasicBlock> blocks)
		{
			if (!blocks.Any())
			{
				Connect(_start, _end);
			}
			else
			{
				Connect(_start, blocks.First());
			}

			foreach (BasicBlock block in blocks)
			{
				foreach (BoundStatement statement in block.Statements)
				{
					_blockFromStatement.Add(statement, block);
					if (statement is BoundLabelStatement labelStatement)
					{
						_blockFromLabel.Add(labelStatement.Label, block);
					}
				}
			}

			for (int i = 0; i < blocks.Count; i++)
			{
				BasicBlock current = blocks[i];
				BasicBlock next = i == blocks.Count - 1 ? _end : blocks[i + 1];

				foreach (BoundStatement statement in current.Statements)
				{
					bool isLastStatementInBlock = statement == current.Statements.Last();
					switch (statement)
					{
						case BoundGotoStatement gotoStatement:
							{
								BasicBlock toBlock = _blockFromLabel[gotoStatement.Label];
								Connect(current, toBlock);
							}

							break;
						case BoundEventGotoStatement eventGotoStatement:
							{
								BasicBlock thenBlock = _blockFromLabel[eventGotoStatement.Label];
								BasicBlock elseBlock = next;
								Connect(current, thenBlock, null);
								Connect(current, elseBlock, null);
							}

							break;
						case BoundConditionalGotoStatement conditionalGotoStatement:
							{
								BasicBlock thenBlock = _blockFromLabel[conditionalGotoStatement.Label];
								BasicBlock elseBlock = next;

								BoundExpression negatedCondition = Negate(conditionalGotoStatement.Condition);
								BoundExpression thenCondition = conditionalGotoStatement.JumpIfTrue ? conditionalGotoStatement.Condition : negatedCondition;
								BoundExpression elseCondition = conditionalGotoStatement.JumpIfTrue ? negatedCondition : conditionalGotoStatement.Condition;
								Connect(current, thenBlock, thenCondition);
								Connect(current, elseBlock, elseCondition);
							}

							break;
						case BoundReturnStatement:
							Connect(current, _end);
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
							if (isLastStatementInBlock)
							{
								Connect(current, next);
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
					RemoveBlock(blocks, block);
					goto ScanAgain;
				}
			}

			blocks.Insert(0, _start);
			blocks.Add(_end);

			return new ControlFlowGraph(_start, _end, blocks, _branches);
		}

		private static BoundExpression Negate(BoundExpression condition)
		{
			BoundUnaryExpression negated = BoundNodeFactory.Not(condition.Syntax, condition);
			return negated.ConstantValue is not null ? new BoundLiteralExpression(condition.Syntax, negated.ConstantValue.Value) : negated;
		}

		private void Connect(BasicBlock from, BasicBlock to, BoundExpression? condition = null)
		{
			if (condition is BoundLiteralExpression l)
			{
				bool value = (bool)l.ConstantValue.GetValueOrDefault(TypeSymbol.Bool);
				if (value)
				{
					condition = null;
				}
				else
				{
					return;
				}
			}

			BasicBlockBranch branch = new BasicBlockBranch(from, to, condition);
			from.Outgoing.Add(branch);
			to.Incoming.Add(branch);
			_branches.Add(branch);
		}

		private void RemoveBlock(List<BasicBlock> blocks, BasicBlock block)
		{
			foreach (BasicBlockBranch branch in block.Incoming)
			{
				branch.From.Outgoing.Remove(branch);
				_branches.Remove(branch);
			}

			foreach (BasicBlockBranch branch in block.Outgoing)
			{
				branch.To.Incoming.Remove(branch);
				_branches.Remove(branch);
			}

			blocks.Remove(block);
		}
	}
}
