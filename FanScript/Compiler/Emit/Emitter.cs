using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.FCInfo;
using FanScript.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Emit
{
    internal sealed class Emitter
    {
        private DiagnosticBag diagnostics = new DiagnosticBag();
        private List<VariableSymbol> variables = new List<VariableSymbol>();
        private CodeBuilder builder = null!;
        private BoundProgram program = null!;

        // key - a label before antoher label, item - the label after key
        private Dictionary<string, string> sameTargetLabels = new();
        // key - label name
        private Dictionary<string, List<EmitStore>> gotosToConnect = new(); // not necessarily gotos
        private Dictionary<string, EmitStore> afterLabel = new();

        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, CodeBuilder builder)
        {
            if (program.Diagnostics.HasErrors())
                return program.Diagnostics;
            else
            {
                Emitter emitter = new Emitter();
                return emitter.emit(program, builder);
            }
        }

        internal ImmutableArray<Diagnostic> emit(BoundProgram _program, CodeBuilder _builder)
        {
            builder = _builder;
            program = _program;

            builder.BlockPlacer.EnterStatementBlock();
            emitStatement(program.Functions.First().Value);

            processLabelsAndGotos();
            builder.BlockPlacer.ExitStatementBlock();

            return diagnostics.ToImmutableArray();
        }

        private void processLabelsAndGotos()
        {
            foreach (var (labelName, stores) in gotosToConnect)
            {
                if (!tryGetAfterLabel(labelName, out EmitStore? afterLabel))
                    continue;

                foreach (EmitStore store in stores)
                    builder.ConnectBlocks(store, afterLabel);
            }

            sameTargetLabels.Clear();
            gotosToConnect.Clear();
            afterLabel.Clear();

            bool tryGetAfterLabel(string name, [NotNullWhen(true)] out EmitStore? emitStore)
            {
                if (afterLabel.TryGetValue(name, out emitStore))
                {
                    if (emitStore is GotoEmitStore gotoEmit)
                        return tryGetAfterLabel(gotoEmit.LabelName, out emitStore);
                    else
                        return true;
                }
                else if (sameTargetLabels.TryGetValue(name, out string? target))
                    return tryGetAfterLabel(target, out emitStore);
                else
                {
                    emitStore = null;
                    return false;
                }
            }
        }

        private EmitStore emitStatement(BoundStatement statement)
        {
            EmitStore store = new NopEmitStore();

            if (statement is BoundBlockStatement block)
                store = emitBlockStatement(block);
            else if (statement is BoundVariableDeclaration variableDeclaration)
            {
                if (variableDeclaration.OptionalAssignment is not null)
                    store = emitAssigmentExpression(variableDeclaration.OptionalAssignment);
                //else
                //    store = null;
            }
            else if (statement is BoundIfStatement isStatement)
                store = emitIfStatement(isStatement);
            else if (statement is BoundGotoStatement gotoStatement)
                store = emitGotoStatement(gotoStatement);
            else if (statement is BoundConditionalGotoStatement conditionalGotoStatement)
            {
                if (conditionalGotoStatement.Condition is BoundSpecialBlockCondition condition)
                    store = emitSpecialBlockStatement(condition.Keyword, conditionalGotoStatement.Label);
                else
                    store = emitConditionalGotoStatement(conditionalGotoStatement);
            }
            else if (statement is BoundLabelStatement labelStatement)
                store = emitLabelStatement(labelStatement);
            else if (statement is BoundExpressionStatement expression)
                store = emitExpression(expression.Expression);
            else
                throw new Exception($"Unsuported statement '{statement}'.");

            return store;
        }

        private EmitStore emitBlockStatement(BoundBlockStatement block)
        {
            if (block.Statements.Length == 0)
                return new NopEmitStore();
            else if (block.Statements.Length == 1 && block.Statements[0] is BoundBlockStatement inBlock)
                return emitBlockStatement(inBlock);

            MultiEmitStore store = MultiEmitStore.Empty;
            EmitStore? lastStore = new NopEmitStore();

            bool newCodeBlock = builder.BlockPlacer.CurrentCodeBlockBlocks > 0;
            if (newCodeBlock)
                builder.BlockPlacer.EnterStatementBlock();

            for (int i = 0; i < block.Statements.Length; i++)
            {
                EmitStore __store = emitStatement(block.Statements[i]);
                if (store.InStore is NopEmitStore && __store is not NopEmitStore)
                    store.InStore = __store;
                else if (__store is not NopEmitStore)
                    connectBlocks(lastStore, __store);

                if (__store is not NopEmitStore)
                    lastStore = __store;
            }

            if (lastStore is NopEmitStore)
                return new NopEmitStore();

            store.OutStore = lastStore;

            if (newCodeBlock)
                builder.BlockPlacer.ExitStatementBlock();

            return store;
        }

        private EmitStore emitSpecialBlockStatement(SyntaxKind keyword, BoundLabel onTrueLabel)
        {
            DefBlock def;
            switch (keyword)
            {
                case SyntaxKind.KeywordOnPlay:
                    def = Blocks.Control.PlaySensor;
                    break;
                default:
                    throw new Exception($"Unsupported Keyword: {keyword}");
            }

            Block block = builder.AddBlock(def);

            connectToLabel(onTrueLabel.Name, BlockEmitStore.COut(block, block.Type.Terminals[1]));

            return new BlockEmitStore(block);
        }

        private EmitStore emitIfStatement(BoundIfStatement ifStatement)
        {
            Block block = builder.AddBlock(Blocks.Control.If);

            builder.BlockPlacer.EnterExpression();
            EmitStore condition = emitExpression(ifStatement.Condition);
            builder.BlockPlacer.ExitExpression();
            builder.BlockPlacer.EnterStatementBlock();
            EmitStore ifTrue = emitStatement(ifStatement.ThenStatement);

            EmitStore? ifFalse = null;
            if (ifStatement.ElseStatement is not null)
                ifFalse = emitStatement(ifStatement.ElseStatement);

            builder.BlockPlacer.ExitStatementBlock();

            connectBlocks(condition, BlockEmitStore.CIn(block, block.Type.Terminals[3]));
            connectBlocks(BlockEmitStore.COut(block, block.Type.Terminals[2]), ifTrue);
            if (ifFalse is not null)
                connectBlocks(BlockEmitStore.COut(block, block.Type.Terminals[1]), ifFalse);

            return new BlockEmitStore(block);
        }

        private EmitStore emitGotoStatement(BoundGotoStatement gotoStatement)
        {
            if (gotoStatement is BoundRollbackGotoStatement) return new RollbackEmitStore();
            else return new GotoEmitStore(gotoStatement.Label.Name);
        }

        private EmitStore emitConditionalGotoStatement(BoundConditionalGotoStatement gotoStatement)
        {
            Block block = builder.AddBlock(Blocks.Control.If);

            builder.BlockPlacer.EnterExpression();
            EmitStore condition = emitExpression(gotoStatement.Condition);
            builder.BlockPlacer.ExitExpression();
            connectBlocks(condition, BlockEmitStore.CIn(block, block.Type.Terminals[3]));

            ConditionalGotoEmitStore store = new ConditionalGotoEmitStore(block, block.Type.Before,
                block, block.Type.Terminals[gotoStatement.JumpIfTrue ? 2 : 1], block,
                block.Type.Terminals[gotoStatement.JumpIfTrue ? 1 : 2]);

            connectToLabel(gotoStatement.Label.Name, BlockEmitStore.COut(store.OnCondition, store.OnConditionTerminal));
            return store;

        }

        private EmitStore emitLabelStatement(BoundLabelStatement labelStatement)
        {
            LabelEmitStore store = new LabelEmitStore(labelStatement.Label.Name);
            return store;
        }

        private EmitStore emitExpression(BoundExpression expression)
        {
            EmitStore store = new NopEmitStore();

            if (expression is BoundAssignmentExpression assigment)
                store = emitAssigmentExpression(assigment);
            else if (expression is BoundLiteralExpression literal)
                store = emitLiteralExpression(literal);
            else if (expression is BoundUnaryExpression unary)
                store = emitUnaryExpression(unary);
            else if (expression is BoundBinaryExpression binary)
                store = emitBinaryExpression(binary);
            else if (expression is BoundVariableExpression name)
                store = emitVariableExpression(name);
            else if (expression is BoundCallExpression call)
                store = emitCallExpression(call);
            else
                throw new Exception($"Unsuported expression: '{expression.GetType()}'.");

            return store;
        }

        private EmitStore emitAssigmentExpression(BoundAssignmentExpression assignment)
        {
            Block block = builder.AddBlock(Blocks.Variables.GetSet_Variable(assignment.Variable.Type!.ToWireType()));

            builder.SetBlockValue(block, 0, assignment.Variable.Name);

            builder.BlockPlacer.EnterExpression();
            EmitStore _store = emitExpression(assignment.Expression);
            builder.BlockPlacer.ExitExpression();

            connectBlocks(_store, BlockEmitStore.CIn(block, block.Type.Terminals[1]));

            return new BlockEmitStore(block);
        }

        private EmitStore emitLiteralExpression(BoundLiteralExpression literal)
        {
            Block block = builder.AddBlock(Blocks.Values.GetValue(literal.Value));

            builder.SetBlockValue(block, 0, literal.Value);

            return BlockEmitStore.COut(block, block.Type.Terminals[0]);
        }

        private EmitStore emitUnaryExpression(BoundUnaryExpression unary)
        {
            switch (unary.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return emitExpression(unary.Operand);
                case BoundUnaryOperatorKind.Negation:
                    {
                        Block block = builder.AddBlock(Blocks.Math.Negate);

                        builder.BlockPlacer.EnterExpression();
                        EmitStore _store = emitExpression(unary.Operand);
                        builder.BlockPlacer.ExitExpression();

                        connectBlocks(_store, BlockEmitStore.CIn(block, block.Type.Terminals[1]));

                        return BlockEmitStore.COut(block, block.Type.Terminals[0]);
                    }
                case BoundUnaryOperatorKind.LogicalNegation:
                    {
                        Block block = builder.AddBlock(Blocks.Math.Not);

                        builder.BlockPlacer.EnterExpression();
                        EmitStore _store = emitExpression(unary.Operand);
                        builder.BlockPlacer.ExitExpression();

                        connectBlocks(_store, BlockEmitStore.CIn(block, block.Type.Terminals[1]));

                        return BlockEmitStore.COut(block, block.Type.Terminals[0]);
                    }
                default:
                    throw new Exception($"Unsuported BoundUnaryOperatorKind: '{unary.Op.Kind}'.");
            }
        }

        private EmitStore emitBinaryExpression(BoundBinaryExpression binary)
        {
            if ((binary.Left.Type == TypeSymbol.Float || binary.Left.Type == TypeSymbol.Bool)
                && binary.Left.Type == binary.Right.Type)
                return emitBinaryExpression_FloatOrBool(binary);
            else
                throw new NotImplementedException();//return emitBinaryExpression_VecOrRot(binary);
        }
        private EmitStore emitBinaryExpression_FloatOrBool(BoundBinaryExpression binary)
        {
            DefBlock op;
            switch (binary.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    op = Blocks.Math.Add_Number;
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                    op = Blocks.Math.Subtract_Number;
                    break;
                case BoundBinaryOperatorKind.Multiplication:
                    op = Blocks.Math.Multiply_Number;
                    break;
                case BoundBinaryOperatorKind.Division:
                    op = Blocks.Math.Divide_Number;
                    break;
                case BoundBinaryOperatorKind.Modulo:
                    op = Blocks.Math.Modulo_Number;
                    break;
                case BoundBinaryOperatorKind.Equals:
                case BoundBinaryOperatorKind.NotEquals:
                    op = Blocks.Math.GetEquals(binary.Left.Type!.ToWireType());
                    break;
                case BoundBinaryOperatorKind.LogicalAnd:
                    op = Blocks.Math.LogicalAnd;
                    break;
                case BoundBinaryOperatorKind.LogicalOr:
                    op = Blocks.Math.LogicalOr;
                    break;
                case BoundBinaryOperatorKind.Less:
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    op = Blocks.Math.Less;
                    break;
                case BoundBinaryOperatorKind.Greater:
                case BoundBinaryOperatorKind.LessOrEquals:
                    op = Blocks.Math.Greater;
                    break;
                default:
                    throw new Exception($"Unexpected BoundBinaryOperatorKind: '{binary.Op.Kind}'.");
            }

            if (binary.Op.Kind == BoundBinaryOperatorKind.NotEquals
                || binary.Op.Kind == BoundBinaryOperatorKind.LessOrEquals
                || binary.Op.Kind == BoundBinaryOperatorKind.GreaterOrEquals)
            {
                // invert output, >= or <=, >= can be accomplished as inverted <
                Block not = builder.AddBlock(Blocks.Math.Not);
                builder.BlockPlacer.EnterExpression();

                Block block = builder.AddBlock(op);
                builder.BlockPlacer.EnterExpression();
                EmitStore store0 = emitExpression(binary.Left);
                EmitStore store1 = emitExpression(binary.Right);
                builder.BlockPlacer.ExitExpression();
                builder.BlockPlacer.ExitExpression();

                connectBlocks(BlockEmitStore.COut(block, block.Type.Terminals[0]),
                    BlockEmitStore.CIn(not, not.Type.Terminals[1]));
                connectBlocks(store0, BlockEmitStore.CIn(block, block.Type.Terminals[2]));
                connectBlocks(store1, BlockEmitStore.CIn(block, block.Type.Terminals[1]));

                return BlockEmitStore.COut(not, not.Type.Terminals[0]);
            }
            else
            {
                Block block = builder.AddBlock(op);
                builder.BlockPlacer.EnterExpression();
                EmitStore store0 = emitExpression(binary.Left);
                EmitStore store1 = emitExpression(binary.Right);
                builder.BlockPlacer.ExitExpression();

                connectBlocks(store0, BlockEmitStore.CIn(block, block.Type.Terminals[2]));
                connectBlocks(store1, BlockEmitStore.CIn(block, block.Type.Terminals[1]));

                return BlockEmitStore.COut(block, block.Type.Terminals[0]);
            }
        }

        private EmitStore emitVariableExpression(BoundVariableExpression name)
        {
            VariableSymbol symbol = name.Variable;
            Block block = builder.AddBlock(Blocks.Variables.Get_Variable(symbol.Type!.ToWireType()));

            builder.SetBlockValue(block, 0, symbol.Name);

            return BlockEmitStore.COut(block, block.Type.Terminals[0]);
        }

        private EmitStore emitCallExpression(BoundCallExpression call)
        {
            switch (call.Function.Name)
            {
                default:
                    {
                        diagnostics.ReportUndefinedFunction(call.Syntax.Location, call.Function.Name);
                        return new NopEmitStore();
                    }
            }
        }

        private bool VariableExists(string name, [NotNullWhen(true)] out VariableSymbol? symbol)
        {
            for (int i = 0; i < variables.Count; i++)
                if (variables[i].Name == name)
                {
                    symbol = variables[i];
                    return true;
                }

            symbol = null;
            return false;
        }

        private void connectBlocks(EmitStore from, EmitStore to)
        {
            while (from is MultiEmitStore multi)
                from = multi.OutStore;
            while (to is MultiEmitStore multi)
                to = multi.InStore;

            if (from is RollbackEmitStore || to is RollbackEmitStore)
                return;

            if (from is LabelEmitStore sameFrom && to is LabelEmitStore sameTo)
                sameTargetLabels.Add(sameFrom.Name, sameTo.Name);
            else if (from is GotoEmitStore fromGoto)
            {
                // ignore, the going to is handeled if to is GotoEmitStore
            }
            else if (from is LabelEmitStore fromLabel)
                afterLabel.Add(fromLabel.Name, to);
            else if (to is LabelEmitStore toLabel)
                connectToLabel(toLabel.Name, from); // normal block before label, connect to block after the label
            else if (to is GotoEmitStore toGoto)
                connectToLabel(toGoto.LabelName, from);
            else
                builder.ConnectBlocks(from, to);
        }
        private void connectToLabel(string labelName, EmitStore store)
        {
            if (!gotosToConnect.TryGetValue(labelName, out var stores))
            {
                stores = new List<EmitStore>();
                gotosToConnect.Add(labelName, stores);
            }

            stores.Add(store);
        }
    }
}
