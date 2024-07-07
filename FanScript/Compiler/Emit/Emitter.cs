using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
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

        // key - label name
        private Dictionary<string, GotoEmitStore> Gotos = new Dictionary<string, GotoEmitStore>();
        private Dictionary<string, LabelEmitStore> Labels = new Dictionary<string, LabelEmitStore>();

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

            processGotos();
            builder.BlockPlacer.ExitStatementBlock();

            return diagnostics.ToImmutableArray();
        }

        private void processGotos()
        {
            // make sure last label works
            foreach (KeyValuePair<string, LabelEmitStore> item in Labels)
                if (item.Value.Out == null)
                {
                    Block nop = builder.AddBlock(Blocks.Nop);

                    item.Value.Out = [nop];
                    item.Value.OutTerminal = [nop.Type.Before];
                }

            foreach (KeyValuePair<string, GotoEmitStore> item in Gotos)
                if (Labels.TryGetValue(item.Key, out LabelEmitStore? label))
                {
                    if (item.Value is ConditionalGotoEmitStore conditional)
                        builder.ConnectBlocks(conditional.OnCondition, conditional.OnConditionConnector,
                            label.Out[0], label.OutTerminal[0]);
                    else
                        builder.ConnectBlocks(item.Value.In, item.Value.InTerminal, label.Out[0], label.OutTerminal[0]);
                }
                else
                    throw new Exception($"Label '{item.Key}' wasn't found.");

            foreach (KeyValuePair<string, LabelEmitStore> item in Labels)
            {
                LabelEmitStore label = item.Value;
                if (label.In != null && label.Out != null)
                    builder.ConnectBlocks(label.In, label.InTerminal, label.Out[0], label.OutTerminal[0]);
            }

            Gotos.Clear();
            Labels.Clear();
        }

        private EmitStore emitStatement(BoundStatement statement)
        {
            EmitStore store = EmitStore.Default;

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
                store = emitConditionalGotoStatement(conditionalGotoStatement);
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
                return EmitStore.Default;
            else if (block.Statements.Length == 1 && block.Statements[0] is BoundBlockStatement inBlock)
                return emitBlockStatement(inBlock);

            EmitStore store = EmitStore.Default;
            EmitStore? _store = null;

            bool newCodeBlock = builder.BlockPlacer.CurrentCodeBlockBlocks > 0;
            if (newCodeBlock)
                builder.BlockPlacer.EnterStatementBlock();

            //if (block.Statements[0] is BoundLabelStatement)
            //{
            //    EmitStore __store = new EmitStore(builder.AddBlock(Blocks.Nop));
            //    store.In = __store.In;
            //    store.InTerminal = __store.InTerminal;
            //    _store = __store;
            //}

            for (int i = 0; i < block.Statements.Length; i++)
            {
                EmitStore __store = emitStatement(block.Statements[i]);
                if (store.In == null/*i == 0*/ && _store == null && __store is not null)
                {
                    store.In = __store.In;
                    store.InTerminal = __store.InTerminal;
                }
                else if (__store is not null)
                    connectBlocks(_store, __store);

                if (__store is not null)
                    _store = __store;
            }

            if (_store == null)
                return EmitStore.Default;

            store.Out = _store.Out;
            store.OutTerminal = _store.OutTerminal;

            if (newCodeBlock)
                builder.BlockPlacer.ExitStatementBlock();

            return store;
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

            connectBlocks(condition, EmitStore.CIn(block, block.Type.Terminals[3]));
            connectBlocks(EmitStore.COut(block, block.Type.Terminals[2]), ifTrue);
            if (ifFalse is not null)
                connectBlocks(EmitStore.COut(block, block.Type.Terminals[1]), ifFalse);

            return new EmitStore(block);
        }

        private EmitStore emitGotoStatement(BoundGotoStatement gotoStatement)
        {
            GotoEmitStore store = new GotoEmitStore();
            Gotos.Add(gotoStatement.Label.Name, store);
            return store;
        }

        private EmitStore emitConditionalGotoStatement(BoundConditionalGotoStatement gotoStatement)
        {
            Block block = builder.AddBlock(Blocks.Control.If);

            builder.BlockPlacer.EnterExpression();
            EmitStore condition = emitExpression(gotoStatement.Condition);
            builder.BlockPlacer.ExitExpression();
            connectBlocks(condition, EmitStore.CIn(block, block.Type.Terminals[3]));

            ConditionalGotoEmitStore store = new ConditionalGotoEmitStore(block, block.Type.Before,
                block, block.Type.Terminals[gotoStatement.JumpIfTrue ? 2 : 1], block,
                block.Type.Terminals[gotoStatement.JumpIfTrue ? 1 : 2]);
            Gotos.Add(gotoStatement.Label.Name, store);
            return store;

        }

        private EmitStore emitLabelStatement(BoundLabelStatement labelStatement)
        {
            LabelEmitStore store = new LabelEmitStore(labelStatement.Label.Name);
            Labels.Add(labelStatement.Label.Name, store);
            return store;
        }

        private EmitStore emitExpression(BoundExpression expression)
        {
            EmitStore store = EmitStore.Default;

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

            connectBlocks(_store, EmitStore.CIn(block, block.Type.Terminals[1]));

            return new EmitStore(block);
        }

        private EmitStore emitLiteralExpression(BoundLiteralExpression literal)
        {
            Block block = builder.AddBlock(Blocks.Values.GetValue(literal.Value));

            builder.SetBlockValue(block, 0, literal.Value);

            return EmitStore.COut(block, block.Type.Terminals[0]);
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

                        connectBlocks(_store, EmitStore.CIn(block, block.Type.Terminals[1]));

                        return EmitStore.COut(block, block.Type.Terminals[0]);
                    }
                case BoundUnaryOperatorKind.LogicalNegation:
                    {
                        Block block = builder.AddBlock(Blocks.Math.Not);

                        builder.BlockPlacer.EnterExpression();
                        EmitStore _store = emitExpression(unary.Operand);
                        builder.BlockPlacer.ExitExpression();

                        connectBlocks(_store, EmitStore.CIn(block, block.Type.Terminals[1]));

                        return EmitStore.COut(block, block.Type.Terminals[0]);
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

                connectBlocks(EmitStore.COut(block, block.Type.Terminals[0]),
                    EmitStore.CIn(not, not.Type.Terminals[1]));
                connectBlocks(store0, EmitStore.CIn(block, block.Type.Terminals[2]));
                connectBlocks(store1, EmitStore.CIn(block, block.Type.Terminals[1]));

                return EmitStore.COut(not, not.Type.Terminals[0]);
            }
            else
            {
                Block block = builder.AddBlock(op);
                builder.BlockPlacer.EnterExpression();
                EmitStore store0 = emitExpression(binary.Left);
                EmitStore store1 = emitExpression(binary.Right);
                builder.BlockPlacer.ExitExpression();

                connectBlocks(store0, EmitStore.CIn(block, block.Type.Terminals[2]));
                connectBlocks(store1, EmitStore.CIn(block, block.Type.Terminals[1]));

                return EmitStore.COut(block, block.Type.Terminals[0]);
            }
        }

        private EmitStore emitVariableExpression(BoundVariableExpression name)
        {
            VariableSymbol symbol = name.Variable;
            Block block = builder.AddBlock(Blocks.Variables.Get_Variable(symbol.Type!.ToWireType()));

            builder.SetBlockValue(block, 0, symbol.Name);

            return EmitStore.COut(block, block.Type.Terminals[0]);
        }

        private EmitStore emitCallExpression(BoundCallExpression call)
        {
            switch (call.Function.Name)
            {
                default:
                    {
                        diagnostics.ReportUndefinedFunction(call.Syntax.Location, call.Function.Name);
                        return EmitStore.Default;
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
            if (to is GotoEmitStore gotoEmit && to is not ConditionalGotoEmitStore)
            {
                gotoEmit.In = from.Out[0];
                gotoEmit.InTerminal = from.OutTerminal[0];
            }
            else if (to is LabelEmitStore toLabel)
            {
                if (from.Out != null)
                {
                    toLabel.In = from.Out[0];
                    toLabel.InTerminal = from.OutTerminal[0];
                }
            }
            else if (from is LabelEmitStore fromLabel)
            {
                fromLabel.Out = [to.In];
                fromLabel.OutTerminal = [to.InTerminal];
            }
            else
                builder.ConnectBlocks(from, to);
        }
    }
}
