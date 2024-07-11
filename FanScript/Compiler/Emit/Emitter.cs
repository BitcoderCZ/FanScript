using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.FCInfo;
using FanScript.Utils;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Emit
{
    internal sealed class Emitter
    {
        private EmitContext emitContext = null!;

        private DiagnosticBag diagnostics = new DiagnosticBag();
        private List<VariableSymbol> variables = new List<VariableSymbol>();
        private CodeBuilder builder = null!;
        private BoundProgram program = null!;

        // key - a label before antoher label, item - the label after key
        private Dictionary<string, string> sameTargetLabels = new();
        // key - label name, item - list of goto "origins", not only gotos but also statements just before the label
        private Dictionary<string, List<EmitStore>> gotosToConnect = new();
        // key - label name, item - the store to connect gotos to
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

            emitContext = new EmitContext(builder, diagnostics, emitStatement, emitExpression, connect);

            builder.BlockPlacer.StatementBlock(() =>
            {
                emitStatement(program.Functions.First().Value);

                processLabelsAndGotos();
            });

            return diagnostics.ToImmutableArray();
        }

        private void processLabelsAndGotos()
        {
            foreach (var (labelName, stores) in gotosToConnect)
            {
                if (!tryGetAfterLabel(labelName, out EmitStore? afterLabel))
                    continue;

                foreach (EmitStore store in stores)
                    builder.Connect(store, afterLabel);
            }

            sameTargetLabels.Clear();
            gotosToConnect.Clear();
            afterLabel.Clear();

            // TODO: check for infinite loops? *technically* shouldn't be neccesary because we don't allow user labels and gotos, but...
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
            else if (statement is BoundNopStatement)
                store = new NopEmitStore();
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
                    connect(lastStore, __store);

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
            BlockDef def;
            switch (keyword)
            {
                case SyntaxKind.KeywordOnPlay:
                    def = Blocks.Control.PlaySensor;
                    break;
                default:
                    throw new Exception($"Unsupported Keyword: {keyword}");
            }

            Block block = builder.AddBlock(def);

            connectToLabel(onTrueLabel.Name, BasicEmitStore.COut(block, block.Type.Terminals[1]));

            return new BasicEmitStore(block);
        }

        private EmitStore emitGotoStatement(BoundGotoStatement gotoStatement)
        {
            if (gotoStatement is BoundRollbackGotoStatement) return new RollbackEmitStore();
            else return new GotoEmitStore(gotoStatement.Label.Name);
        }

        private EmitStore emitConditionalGotoStatement(BoundConditionalGotoStatement gotoStatement)
        {
            Block block = builder.AddBlock(Blocks.Control.If);

            builder.BlockPlacer.ExpressionBlock(() =>
            {
                EmitStore condition = emitExpression(gotoStatement.Condition);

                connect(condition, BasicEmitStore.CIn(block, block.Type.Terminals[3]));
            });

            ConditionalGotoEmitStore store = new ConditionalGotoEmitStore(block, block.Type.Before,
                block, block.Type.Terminals[gotoStatement.JumpIfTrue ? 2 : 1], block,
                block.Type.Terminals[gotoStatement.JumpIfTrue ? 1 : 2]);

            connectToLabel(gotoStatement.Label.Name, BasicEmitStore.COut(store.OnCondition, store.OnConditionTerminal));
            return store;

        }

        private EmitStore emitLabelStatement(BoundLabelStatement labelStatement)
        {
            return new LabelEmitStore(labelStatement.Label.Name);
        }

        private EmitStore emitExpression(BoundExpression expression)
        {
            EmitStore store = new NopEmitStore();

            if (expression is BoundAssignmentExpression assigment)
                store = emitAssigmentExpression(assigment);
            else if (expression is BoundLiteralExpression literal)
                store = emitLiteralExpression(literal);
            else if (expression is BoundConstructorExpression constructor)
                store = emitConstructorExpression(constructor);
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
            Block block = builder.AddBlock(Blocks.Variables.Set_VariableByType(assignment.Variable.Type!.ToWireType()));

            builder.SetBlockValue(block, 0, assignment.Variable.Name);

            builder.BlockPlacer.ExpressionBlock(() =>
            {
                EmitStore _store = emitExpression(assignment.Expression);

                connect(_store, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
            });

            return new BasicEmitStore(block);
        }

        private EmitStore emitLiteralExpression(BoundLiteralExpression literal)
            => emitLiteralExpression(literal.Value);
        private EmitStore emitLiteralExpression(object value)
        {
            Block block = builder.AddBlock(Blocks.Values.ValueByType(value));

            if (value is not bool)
                builder.SetBlockValue(block, 0, value);

            return BasicEmitStore.COut(block, block.Type.Terminals[0]);
        }

        private EmitStore emitConstructorExpression(BoundConstructorExpression constructor)
        {
            if (constructor.ConstantValue is not null)
                return emitLiteralExpression(constructor.ConstantValue.Value);

            BlockDef def = Blocks.Math.MakeByType(constructor.Type.ToWireType());
            Block block = builder.AddBlock(def);

            builder.BlockPlacer.ExpressionBlock(() =>
            {
                EmitStore xStore = emitExpression(constructor.ExpressionX);
                EmitStore yStore = emitExpression(constructor.ExpressionY);
                EmitStore zStore = emitExpression(constructor.ExpressionZ);

                connect(xStore, BasicEmitStore.CIn(block, def.Terminals[3]));
                connect(yStore, BasicEmitStore.CIn(block, def.Terminals[2]));
                connect(zStore, BasicEmitStore.CIn(block, def.Terminals[1]));
            });

            return BasicEmitStore.COut(block, def.Terminals[0]);
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

                        builder.BlockPlacer.ExpressionBlock(() =>
                        {
                            EmitStore _store = emitExpression(unary.Operand);

                            connect(_store, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                        });

                        return BasicEmitStore.COut(block, block.Type.Terminals[0]);
                    }
                case BoundUnaryOperatorKind.LogicalNegation:
                    {
                        Block block = builder.AddBlock(Blocks.Math.Not);

                        builder.BlockPlacer.ExpressionBlock(() =>
                        {
                            EmitStore _store = emitExpression(unary.Operand);

                            connect(_store, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                        });

                        return BasicEmitStore.COut(block, block.Type.Terminals[0]);
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
                return emitBinaryExpression_VecOrRot(binary);
        }
        private EmitStore emitBinaryExpression_FloatOrBool(BoundBinaryExpression binary)
        {
            BlockDef op;
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
                    op = Blocks.Math.EqualsByType(binary.Left.Type!.ToWireType());
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
                builder.BlockPlacer.ExpressionBlock(() =>
                {
                    Block block = builder.AddBlock(op);

                    connect(BasicEmitStore.COut(block, block.Type.Terminals[0]),
                        BasicEmitStore.CIn(not, not.Type.Terminals[1]));

                    builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        EmitStore store0 = emitExpression(binary.Left);
                        EmitStore store1 = emitExpression(binary.Right);

                        connect(store0, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                        connect(store1, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                    });
                });

                return BasicEmitStore.COut(not, not.Type.Terminals[0]);
            }
            else
            {
                Block block = builder.AddBlock(op);
                builder.BlockPlacer.ExpressionBlock(() =>
                {
                    EmitStore store0 = emitExpression(binary.Left);
                    EmitStore store1 = emitExpression(binary.Right);

                    connect(store0, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                    connect(store1, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                });

                return BasicEmitStore.COut(block, block.Type.Terminals[0]);
            }
        }
        private EmitStore emitBinaryExpression_VecOrRot(BoundBinaryExpression binary)
        {
            BlockDef? defOp = null;
            switch (binary.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (binary.Left.Type == TypeSymbol.Vector3)
                        defOp = Blocks.Math.Add_Vector;
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                    if (binary.Left.Type == TypeSymbol.Vector3)
                        defOp = Blocks.Math.Subtract_Vector;
                    break;
                case BoundBinaryOperatorKind.Multiplication:
                    if (binary.Left.Type == TypeSymbol.Vector3)
                        defOp = Blocks.Math.Multiply_Vector;
                    else if (binary.Left.Type == TypeSymbol.Rotation)
                        defOp = Blocks.Math.Multiply_Rotation;
                    else
                        throw new Exception($"Unexpected BoundBinaryOperatorKind: '{binary.Op.Kind}'.");
                    break;
                case BoundBinaryOperatorKind.Division:
                case BoundBinaryOperatorKind.Modulo:
                    break; // supported, but not one block
                case BoundBinaryOperatorKind.Equals:
                    if (binary.Left.Type == TypeSymbol.Vector3)
                        defOp = Blocks.Math.Equals_Vector;
                    else
                        throw new Exception($"Unexpected BoundBinaryOperatorKind: '{binary.Op.Kind}'.");
                    // rotation doesn't have equals???
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    break; // supported, but not one block
                default:
                    throw new Exception($"Unexpected BoundBinaryOperatorKind: '{binary.Op.Kind}'.");
            }

            if (defOp is null)
            {
                switch (binary.Op.Kind)
                {
                    case BoundBinaryOperatorKind.Addition: // Rotation
                        return buildOperatorWithBreak(Blocks.Math.Break_Rotation, Blocks.Math.Make_Rotation,
                            Blocks.Math.Add_Number);
                    case BoundBinaryOperatorKind.Subtraction: // Rotation
                        return buildOperatorWithBreak(Blocks.Math.Break_Rotation, Blocks.Math.Make_Rotation,
                            Blocks.Math.Subtract_Number);
                    case BoundBinaryOperatorKind.Division: // Vector3
                        return buildOperatorWithBreak(Blocks.Math.Break_Vector, Blocks.Math.Make_Vector,
                            Blocks.Math.Divide_Number);
                    case BoundBinaryOperatorKind.Modulo: // Vector3
                        return buildOperatorWithBreak(Blocks.Math.Break_Vector, Blocks.Math.Make_Vector,
                            Blocks.Math.Modulo_Number);
                    case BoundBinaryOperatorKind.NotEquals:
                        {
                            if (binary.Left.Type == TypeSymbol.Vector3)
                                defOp = Blocks.Math.Equals_Vector;
                            else
                                throw new Exception($"Unexpected BoundBinaryOperatorKind: '{binary.Op.Kind}'.");

                            Block not = builder.AddBlock(Blocks.Math.Not);
                            builder.BlockPlacer.ExpressionBlock(() =>
                            {
                                Block op = builder.AddBlock(defOp);

                                connect(BasicEmitStore.COut(op, op.Type.Terminals[0]),
                                    BasicEmitStore.CIn(not, not.Type.Terminals[1]));

                                builder.BlockPlacer.ExpressionBlock(() =>
                                {
                                    EmitStore store0 = emitExpression(binary.Left);
                                    EmitStore store1 = emitExpression(binary.Right);

                                    connect(store0, BasicEmitStore.CIn(op, op.Type.Terminals[2]));
                                    connect(store1, BasicEmitStore.CIn(op, op.Type.Terminals[1]));
                                });
                            });

                            return BasicEmitStore.COut(not, not.Type.Terminals[0]);
                        }
                    default:
                        throw new Exception($"Unexpected BoundBinaryOperatorKind: '{binary.Op.Kind}'.");
                }
            }
            else
            {
                Block op = builder.AddBlock(defOp);
                builder.BlockPlacer.ExpressionBlock(() =>
                {
                    EmitStore store0 = emitExpression(binary.Left);
                    EmitStore store1 = emitExpression(binary.Right);

                    connect(store0, BasicEmitStore.CIn(op, op.Type.Terminals[2]));
                    connect(store1, BasicEmitStore.CIn(op, op.Type.Terminals[1]));
                });

                return BasicEmitStore.COut(op, op.Type.Terminals[0]);
            }

            // TODO: cache of break (vector, rotation) blocks
            EmitStore buildOperatorWithBreak(BlockDef defBreak, BlockDef defMake, BlockDef defOp)
            {
                Block make = builder.AddBlock(defMake);

                builder.BlockPlacer.ExpressionBlock(() =>
                {
                    Block op1 = builder.AddBlock(defOp);

                    Block break1 = null!;
                    Block? break2 = null;
                    EmitStore right = null!;
                    builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        break1 = builder.AddBlock(defBreak);

                        builder.BlockPlacer.ExpressionBlock(() =>
                        {
                            EmitStore left = emitExpression(binary.Left);

                            connect(left, BasicEmitStore.CIn(break1, break1.Type.Terminals[3]));

                            right = emitExpression(binary.Right);
                        });

                        if (binary.Right.Type == TypeSymbol.Vector3 || binary.Right.Type == TypeSymbol.Rotation)
                        {
                            break2 = builder.AddBlock(defBreak);

                            connect(right, BasicEmitStore.CIn(break2, break2.Type.Terminals[3]));
                        }
                    });

                    Block op2 = builder.AddBlock(defOp);
                    Block op3 = builder.AddBlock(defOp);

                    // left to op
                    connect(BasicEmitStore.COut(break1, break1.Type.Terminals[2]),
                        BasicEmitStore.CIn(op1, op1.Type.Terminals[2]));
                    connect(BasicEmitStore.COut(break1, break1.Type.Terminals[1]),
                        BasicEmitStore.CIn(op2, op2.Type.Terminals[2]));
                    connect(BasicEmitStore.COut(break1, break1.Type.Terminals[0]),
                        BasicEmitStore.CIn(op3, op3.Type.Terminals[2]));

                    // right to op
                    connect(break2 is null ? right : BasicEmitStore.COut(break2, break2.Type.Terminals[2]),
                        BasicEmitStore.CIn(op1, op1.Type.Terminals[1]));
                    connect(break2 is null ? right : BasicEmitStore.COut(break2, break2.Type.Terminals[1]),
                        BasicEmitStore.CIn(op2, op2.Type.Terminals[1]));
                    connect(break2 is null ? right : BasicEmitStore.COut(break2, break2.Type.Terminals[0]),
                        BasicEmitStore.CIn(op3, op3.Type.Terminals[1]));

                    // op to make
                    connect(BasicEmitStore.COut(op1, op1.Type.Terminals[0]),
                        BasicEmitStore.CIn(make, make.Type.Terminals[3]));
                    connect(BasicEmitStore.COut(op2, op2.Type.Terminals[0]),
                        BasicEmitStore.CIn(make, make.Type.Terminals[2]));
                    connect(BasicEmitStore.COut(op3, op3.Type.Terminals[0]),
                        BasicEmitStore.CIn(make, make.Type.Terminals[1]));
                });

                return BasicEmitStore.COut(make, make.Type.Terminals[0]);
            }
        }

        private EmitStore emitVariableExpression(BoundVariableExpression name)
        {
            VariableSymbol symbol = name.Variable;
            Block block = builder.AddBlock(Blocks.Variables.VariableByType(symbol.Type!.ToWireType()));

            builder.SetBlockValue(block, 0, symbol.Name);

            return BasicEmitStore.COut(block, block.Type.Terminals[0]);
        }

        private EmitStore emitCallExpression(BoundCallExpression call)
        {
            if (BuiltinFunctions.tryEmitFunction(call, emitContext, out EmitStore? funcStore))
                return funcStore;

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

        private void connect(EmitStore from, EmitStore to)
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
                builder.Connect(from, to);
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
