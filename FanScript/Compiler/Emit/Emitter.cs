using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Emit
{
    internal sealed class Emitter
    {
        private EmitContext emitContext = null!;

        internal DiagnosticBag diagnostics = new DiagnosticBag();
        internal CodeBuilder builder = null!;
        private BoundProgram program = null!;

        // key - a label before antoher label, item - the label after key
        private Dictionary<string, string> sameTargetLabels = new();
        // key - label name, item - list of goto "origins", not only gotos but also statements just before the label
        private Dictionary<string, List<EmitStore>> gotosToConnect = new();
        // key - label name, item - the store to connect gotos to
        private Dictionary<string, EmitStore> afterLabel = new();

        private Dictionary<VariableSymbol, EmitStore> inlineVariableInits = new();

        private Dictionary<VariableSymbol, BreakBlockCache> vectorBreakCache = new();
        private Dictionary<VariableSymbol, BreakBlockCache> rotationBreakCache = new();

        private Dictionary<FunctionSymbol, EmitStore> functions = new();
        private Dictionary<FunctionSymbol, List<EmitStore>> calls = new();

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

        internal ImmutableArray<Diagnostic> emit(BoundProgram program, CodeBuilder builder)
        {
            this.builder = builder;
            this.program = program;

            emitContext = new EmitContext(this);

            vectorBreakCache.Clear();
            rotationBreakCache.Clear();

            foreach (var (func, body) in this.program.Functions.ToImmutableSortedDictionary())
            {
                using (this.builder.BlockPlacer.StatementBlock())
                {
                    writeComment(func.Name);

                    functions.Add(func, emitStatement(body));

                    processLabelsAndGotos();
                }
            }

            processCalls();

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

        private void processCalls()
        {
            foreach (var (func, callList) in calls)
            {
                if (!functions.TryGetValue(func, out var funcStore))
                    throw new Exception($"Failed to get entry point for function '{func}'.");

                for (var i = 0; i < callList.Count; i++)
                    connect(callList[i], funcStore);
            }

            functions.Clear();
            calls.Clear();
        }

        internal EmitStore emitStatement(BoundStatement statement)
        {
            EmitStore store = statement switch
            {
                BoundBlockStatement => emitBlockStatement((BoundBlockStatement)statement),
                BoundVariableDeclarationStatement declaration when declaration.OptionalAssignment is not null => emitStatement(declaration.OptionalAssignment),
                BoundVariableDeclarationStatement => new NopEmitStore(),
                BoundAssignmentStatement => emitAssigmentStatement((BoundAssignmentStatement)statement),
                BoundPostfixStatement => emitPostfixStatement((BoundPostfixStatement)statement),
                BoundGotoStatement => emitGotoStatement((BoundGotoStatement)statement),
                BoundConditionalGotoStatement conditionalGoto when conditionalGoto.Condition is BoundSpecialBlockCondition condition => emitSpecialBlockStatement(condition.SBType, condition.ArgumentClause?.Arguments, conditionalGoto.Label),
                BoundConditionalGotoStatement => emitConditionalGotoStatement((BoundConditionalGotoStatement)statement),
                BoundLabelStatement => emitLabelStatement((BoundLabelStatement)statement),
                BoundReturnStatement => new RollbackEmitStore(), // TODO: add proper method when non void methods are supported
                BoundEmitterHint => emitHint((BoundEmitterHint)statement),
                BoundExpressionStatement => emitExpression(((BoundExpressionStatement)statement).Expression),
                BoundNopStatement => new NopEmitStore(),

                _ => throw new Exception($"Unknown statement '{statement}'."),
            };

            return store;
        }

        private EmitStore emitBlockStatement(BoundBlockStatement statement)
        {
            if (statement.Statements.Length == 0)
                return new NopEmitStore();
            else if (statement.Statements.Length == 1 && statement.Statements[0] is BoundBlockStatement inBlock)
                return emitBlockStatement(inBlock);

            MultiEmitStore resultStore = MultiEmitStore.Empty;
            EmitStore? lastStore = new NopEmitStore();

            bool newCodeBlock = builder.BlockPlacer.CurrentCodeBlockBlocks > 0;
            if (newCodeBlock)
                builder.BlockPlacer.EnterStatementBlock();

            for (int i = 0; i < statement.Statements.Length; i++)
            {
                EmitStore statementStore = emitStatement(statement.Statements[i]);
                if (resultStore.InStore is NopEmitStore && statementStore is not NopEmitStore)
                    resultStore.InStore = statementStore;
                else if (statementStore is not NopEmitStore)
                    connect(lastStore, statementStore);

                if (statementStore is not NopEmitStore)
                    lastStore = statementStore;
            }

            if (lastStore is NopEmitStore)
                return new NopEmitStore();

            resultStore.OutStore = lastStore;

            if (newCodeBlock)
                builder.BlockPlacer.ExitStatementBlock();

            return resultStore;
        }

        private EmitStore emitSpecialBlockStatement(SpecialBlockType type, ImmutableArray<BoundExpression>? arguments, BoundLabel onTrueLabel)
        {
            BlockDef def;
            switch (type)
            {
                case SpecialBlockType.Play:
                    def = Blocks.Control.PlaySensor;
                    break;
                case SpecialBlockType.LateUpdate:
                    def = Blocks.Control.LateUpdate;
                    break;
                case SpecialBlockType.BoxArt:
                    def = Blocks.Control.BoxArtSensor;
                    break;
                case SpecialBlockType.Touch:
                    def = Blocks.Control.TouchSensor;
                    break;
                case SpecialBlockType.Swipe:
                    def = Blocks.Control.SwipeSensor;
                    break;
                case SpecialBlockType.Button:
                    def = Blocks.Control.Button;
                    break;
                case SpecialBlockType.Collision:
                    def = Blocks.Control.Collision;
                    break;
                case SpecialBlockType.Loop:
                    def = Blocks.Control.Loop;
                    break;
                default:
                    throw new Exception($"Unknown {typeof(SpecialBlockType)}: {type}");
            }

            Block block = builder.AddBlock(def);

            builder.BlockPlacer.EnterStatementBlock();

            switch (type)
            {
                case SpecialBlockType.Touch:
                    {
                        object?[]? values = emitContext.ValidateConstants(arguments!.Value.AsMemory(2..), true);
                        if (values is null)
                            break;

                        for (int i = 0; i < values.Length; i++)
                            builder.SetBlockValue(block, i, (byte)((float?)values[i] ?? 0f)); // unbox, then cast

                        connectToLabel(onTrueLabel.Name, placeAndConnectRefArgs(arguments!.Value.AsSpan(..2)));
                        return new BasicEmitStore(block);
                    }
                case SpecialBlockType.Swipe:
                    {
                        connectToLabel(onTrueLabel.Name, placeAndConnectRefArgs(arguments!.Value.AsSpan()));
                        return new BasicEmitStore(block);
                    }
                case SpecialBlockType.Button:
                    {
                        object?[]? values = emitContext.ValidateConstants(arguments!.Value.AsMemory(), true);
                        if (values is null)
                            break;

                        for (int i = 0; i < values.Length; i++)
                            builder.SetBlockValue(block, i, (byte)((float?)values[i] ?? 0f)); // unbox, then cast
                    }
                    break;
                case SpecialBlockType.Collision:
                    {
                        connectToLabel(onTrueLabel.Name, placeAndConnectRefArgs(arguments!.Value.AsSpan(1..), arguments!.Value.AsMemory(..1)));
                        return new BasicEmitStore(block);
                    }
                case SpecialBlockType.Loop:
                    {
                        connectToLabel(onTrueLabel.Name, placeAndConnectRefArgs(arguments!.Value.AsSpan(2..), arguments!.Value.AsMemory(..2)));
                        return new BasicEmitStore(block);
                    }
            }

            connectToLabel(onTrueLabel.Name, BasicEmitStore.COut(block, block.Type.Terminals[^2]));

            return new BasicEmitStore(block);

            EmitStore placeAndConnectRefArgs(ReadOnlySpan<BoundExpression> outArguments, ReadOnlyMemory<BoundExpression>? arguments = null)
            {
                arguments ??= ReadOnlyMemory<BoundExpression>.Empty;

                EmitStore lastStore = BasicEmitStore.COut(block, block.Type.Terminals[Index.FromEnd(2 + arguments.Value.Length)]);

                if (arguments.Value.Length != 0)
                {
                    builder.BlockPlacer.ExitStatementBlock();

                    using (builder.BlockPlacer.ExpressionBlock())
                    {
                        var argumentsSpan = arguments.Value.Span;

                        for (int i = 0; i < argumentsSpan.Length; i++)
                        {
                            EmitStore store = emitExpression(argumentsSpan[i]);

                            connect(store, BasicEmitStore.CIn(block, block.Type.Terminals[Index.FromEnd(i + 2)]));
                        }
                    }

                    builder.BlockPlacer.EnterStatementBlock();
                }

                for (int i = 0; i < outArguments.Length; i++)
                {
                    VariableSymbol variable = ((BoundVariableExpression)outArguments[i]).Variable;

                    EmitStore varStore = emitSetVariable(variable, () => BasicEmitStore.COut(block, block.Type.Terminals[Index.FromEnd(i + arguments.Value.Length + 3)]));

                    connect(lastStore, varStore);

                    if (varStore is not NopEmitStore)
                        lastStore = varStore;
                }

                return lastStore;
            }
        }

        private EmitStore emitAssigmentStatement(BoundAssignmentStatement statement)
            => emitSetVariable(statement.Variable, statement.Expression);

        private EmitStore emitPostfixStatement(BoundPostfixStatement statement)
        {
            BlockDef def;
            switch (statement.PostfixKind)
            {
                case BoundPostfixKind.Increment:
                    def = Blocks.Variables.PlusPlusFloat;
                    break;
                case BoundPostfixKind.Decrement:
                    def = Blocks.Variables.MinusMinusFloat;
                    break;
                default:
                    throw new InvalidDataException($"Unknown {nameof(BoundPostfixKind)} '{statement.PostfixKind}'");
            }

            Block block = builder.AddBlock(def);

            using (builder.BlockPlacer.ExpressionBlock())
            {
                EmitStore store = emitGetVariable(statement.Variable);

                connect(store, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
            }

            return new BasicEmitStore(block);
        }

        private EmitStore emitGotoStatement(BoundGotoStatement statement)
        {
            if (statement is BoundRollbackGotoStatement) return new RollbackEmitStore();
            else return new GotoEmitStore(statement.Label.Name);
        }

        private EmitStore emitConditionalGotoStatement(BoundConditionalGotoStatement statement)
        {
            Block block = builder.AddBlock(Blocks.Control.If);

            using (builder.BlockPlacer.ExpressionBlock())
            {
                EmitStore condition = emitExpression(statement.Condition);

                connect(condition, BasicEmitStore.CIn(block, block.Type.Terminals[3]));
            }

            ConditionalGotoEmitStore store = new ConditionalGotoEmitStore(block, block.Type.Before,
                block, block.Type.Terminals[statement.JumpIfTrue ? 2 : 1], block,
                block.Type.Terminals[statement.JumpIfTrue ? 1 : 2]);

            connectToLabel(statement.Label.Name, BasicEmitStore.COut(store.OnCondition, store.OnConditionTerminal));
            return store;

        }

        private EmitStore emitLabelStatement(BoundLabelStatement statement)
        {
            return new LabelEmitStore(statement.Label.Name);
        }

        private EmitStore emitHint(BoundEmitterHint statement)
        {
            switch (statement.Hint)
            {
                case BoundEmitterHint.HintKind.StatementBlockStart:
                    builder.BlockPlacer.EnterStatementBlock();
                    break;
                case BoundEmitterHint.HintKind.StatementBlockEnd:
                    builder.BlockPlacer.ExitStatementBlock();
                    break;
                case BoundEmitterHint.HintKind.HighlightStart:
                    builder.BlockPlacer.EnterHighlight();
                    break;
                case BoundEmitterHint.HintKind.HighlightEnd:
                    builder.BlockPlacer.ExitHightlight();
                    break;
                default:
                    throw new InvalidDataException($"Unknown emitter hint: '{statement.Hint}'");
            }

            return new NopEmitStore();
        }

        internal EmitStore emitExpression(BoundExpression expression)
        {
            EmitStore store = expression switch
            {
                BoundLiteralExpression => emitLiteralExpression((BoundLiteralExpression)expression),
                BoundConstructorExpression => emitConstructorExpression((BoundConstructorExpression)expression),
                BoundUnaryExpression => emitUnaryExpression((BoundUnaryExpression)expression),
                BoundBinaryExpression => emitBinaryExpression((BoundBinaryExpression)expression),
                BoundVariableExpression => emitVariableExpression((BoundVariableExpression)expression),
                BoundCallExpression => emitCallExpression((BoundCallExpression)expression),

                _ => throw new Exception($"Unknown expression: '{expression.GetType()}'."),
            };

            return store;
        }

        private EmitStore emitLiteralExpression(BoundLiteralExpression expression)
            => emitLiteralExpression(expression.Value);
        internal EmitStore emitLiteralExpression(object? value)
        {
            if (value is null)
                return new NopEmitStore();

            Block block = builder.AddBlock(Blocks.Values.ValueByType(value));

            if (value is not bool)
                builder.SetBlockValue(block, 0, value);

            return BasicEmitStore.COut(block, block.Type.Terminals[0]);
        }

        private EmitStore emitConstructorExpression(BoundConstructorExpression expression)
        {
            if (expression.ConstantValue is not null)
                return emitLiteralExpression(expression.ConstantValue.Value);

            BlockDef def = Blocks.Math.MakeByType(expression.Type.ToWireType());
            Block block = builder.AddBlock(def);

            using (builder.BlockPlacer.ExpressionBlock())
            {
                EmitStore xStore = emitExpression(expression.ExpressionX);
                EmitStore yStore = emitExpression(expression.ExpressionY);
                EmitStore zStore = emitExpression(expression.ExpressionZ);

                connect(xStore, BasicEmitStore.CIn(block, def.Terminals[3]));
                connect(yStore, BasicEmitStore.CIn(block, def.Terminals[2]));
                connect(zStore, BasicEmitStore.CIn(block, def.Terminals[1]));
            }

            return BasicEmitStore.COut(block, def.Terminals[0]);
        }

        private EmitStore emitUnaryExpression(BoundUnaryExpression expression)
        {
            switch (expression.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return emitExpression(expression.Operand);
                case BoundUnaryOperatorKind.Negation:
                    {
                        if (expression.Type == TypeSymbol.Vector3)
                        {
                            Block block = builder.AddBlock(Blocks.Math.Multiply_Vector);

                            using (builder.BlockPlacer.ExpressionBlock())
                            {
                                EmitStore opStore = emitExpression(expression.Operand);

                                Block numb = builder.AddBlock(Blocks.Values.Number);
                                builder.SetBlockValue(numb, 0, -1f);

                                connect(opStore, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                                connect(BasicEmitStore.COut(numb, numb.Type.Terminals[0]), BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                            }

                            return BasicEmitStore.COut(block, block.Type.Terminals[0]);
                        }
                        else
                        {

                            Block block = builder.AddBlock(expression.Type == TypeSymbol.Float ? Blocks.Math.Negate : Blocks.Math.Inverse);

                            using (builder.BlockPlacer.ExpressionBlock())
                            {
                                EmitStore opStore = emitExpression(expression.Operand);

                                connect(opStore, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                            }

                            return BasicEmitStore.COut(block, block.Type.Terminals[0]);
                        }
                    }
                case BoundUnaryOperatorKind.LogicalNegation:
                    {
                        Block block = builder.AddBlock(Blocks.Math.Not);

                        using (builder.BlockPlacer.ExpressionBlock())
                        {
                            EmitStore opStore = emitExpression(expression.Operand);

                            connect(opStore, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                        }

                        return BasicEmitStore.COut(block, block.Type.Terminals[0]);
                    }
                default:
                    throw new Exception($"Unsuported BoundUnaryOperatorKind: '{expression.Op.Kind}'.");
            }
        }

        private EmitStore emitBinaryExpression(BoundBinaryExpression expression)
        {
            if (expression.Type == TypeSymbol.Bool || expression.Type == TypeSymbol.Float)
                return emitBinaryExpression_FloatOrBool(expression);
            else if (expression.Type == TypeSymbol.Vector3 || expression.Type == TypeSymbol.Rotation)
                return emitBinaryExpression_VecOrRot(expression);
            else
                throw new InvalidDataException($"Unknown TypeSymbol '{expression.Type}'");
        }
        private EmitStore emitBinaryExpression_FloatOrBool(BoundBinaryExpression expression)
        {
            BlockDef op;
            switch (expression.Op.Kind)
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
                    op = Blocks.Math.EqualsByType(expression.Left.Type!.ToWireType());
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
                    throw new Exception($"Unexpected BoundBinaryOperatorKind: '{expression.Op.Kind}'.");
            }

            if (expression.Op.Kind == BoundBinaryOperatorKind.NotEquals
                || expression.Op.Kind == BoundBinaryOperatorKind.LessOrEquals
                || expression.Op.Kind == BoundBinaryOperatorKind.GreaterOrEquals)
            {
                // invert output, >= or <=, >= can be accomplished as inverted <
                Block not = builder.AddBlock(Blocks.Math.Not);
                using (builder.BlockPlacer.ExpressionBlock())
                {
                    Block block = builder.AddBlock(op);

                    connect(BasicEmitStore.COut(block, block.Type.Terminals[0]),
                        BasicEmitStore.CIn(not, not.Type.Terminals[1]));

                    using (builder.BlockPlacer.ExpressionBlock())
                    {
                        EmitStore store0 = emitExpression(expression.Left);
                        EmitStore store1 = emitExpression(expression.Right);

                        connect(store0, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                        connect(store1, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                    }
                }

                return BasicEmitStore.COut(not, not.Type.Terminals[0]);
            }
            else
            {
                Block block = builder.AddBlock(op);
                using (builder.BlockPlacer.ExpressionBlock())
                {
                    EmitStore store0 = emitExpression(expression.Left);
                    EmitStore store1 = emitExpression(expression.Right);

                    connect(store0, BasicEmitStore.CIn(block, block.Type.Terminals[2]));
                    connect(store1, BasicEmitStore.CIn(block, block.Type.Terminals[1]));
                }

                return BasicEmitStore.COut(block, block.Type.Terminals[0]);
            }
        }
        private EmitStore emitBinaryExpression_VecOrRot(BoundBinaryExpression expression)
        {
            BlockDef? defOp = null;
            switch (expression.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    if (expression.Left.Type == TypeSymbol.Vector3)
                        defOp = Blocks.Math.Add_Vector;
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                    if (expression.Left.Type == TypeSymbol.Vector3)
                        defOp = Blocks.Math.Subtract_Vector;
                    break;
                case BoundBinaryOperatorKind.Multiplication:
                    if (expression.Left.Type == TypeSymbol.Vector3 && expression.Right.Type == TypeSymbol.Float)
                        defOp = Blocks.Math.Multiply_Vector;
                    else if (expression.Left.Type == TypeSymbol.Rotation && expression.Right.Type == TypeSymbol.Rotation)
                        defOp = Blocks.Math.Multiply_Rotation;
                    else if (expression.Left.Type == TypeSymbol.Vector3 && expression.Right.Type == TypeSymbol.Rotation)
                        defOp = Blocks.Math.Rotate_Vector;
                    else
                        throw new Exception($"Unexpected BoundBinaryOperatorKind: '{expression.Op.Kind}'.");
                    break;
                case BoundBinaryOperatorKind.Division:
                case BoundBinaryOperatorKind.Modulo:
                    break; // supported, but not one block
                case BoundBinaryOperatorKind.Equals:
                    if (expression.Left.Type == TypeSymbol.Vector3)
                        defOp = Blocks.Math.Equals_Vector;
                    else
                        throw new Exception($"Unexpected BoundBinaryOperatorKind: '{expression.Op.Kind}'.");
                    // rotation doesn't have equals???
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    break; // supported, but not one block
                default:
                    throw new Exception($"Unexpected BoundBinaryOperatorKind: '{expression.Op.Kind}'.");
            }

            if (defOp is null)
            {
                switch (expression.Op.Kind)
                {
                    case BoundBinaryOperatorKind.Addition: // Rotation
                        return buildOperatorWithBreak(Blocks.Math.Make_Rotation, Blocks.Math.Add_Number);
                    case BoundBinaryOperatorKind.Subtraction: // Rotation
                        return buildOperatorWithBreak(Blocks.Math.Make_Rotation, Blocks.Math.Subtract_Number);
                    case BoundBinaryOperatorKind.Division: // Vector3
                        return buildOperatorWithBreak(Blocks.Math.Make_Vector, Blocks.Math.Divide_Number);
                    case BoundBinaryOperatorKind.Modulo: // Vector3
                        return buildOperatorWithBreak(Blocks.Math.Make_Vector, Blocks.Math.Modulo_Number);
                    case BoundBinaryOperatorKind.NotEquals:
                        {
                            if (expression.Left.Type == TypeSymbol.Vector3)
                                defOp = Blocks.Math.Equals_Vector;
                            else
                                throw new Exception($"Unexpected BoundBinaryOperatorKind: '{expression.Op.Kind}'.");

                            Block not = builder.AddBlock(Blocks.Math.Not);
                            using (builder.BlockPlacer.ExpressionBlock())
                            {
                                Block op = builder.AddBlock(defOp);

                                connect(BasicEmitStore.COut(op, op.Type.Terminals[0]),
                                    BasicEmitStore.CIn(not, not.Type.Terminals[1]));

                                using (builder.BlockPlacer.ExpressionBlock())
                                {
                                    EmitStore store0 = emitExpression(expression.Left);
                                    EmitStore store1 = emitExpression(expression.Right);

                                    connect(store0, BasicEmitStore.CIn(op, op.Type.Terminals[2]));
                                    connect(store1, BasicEmitStore.CIn(op, op.Type.Terminals[1]));
                                }
                            }

                            return BasicEmitStore.COut(not, not.Type.Terminals[0]);
                        }
                    default:
                        throw new Exception($"Unexpected BoundBinaryOperatorKind: '{expression.Op.Kind}'.");
                }
            }
            else
            {
                Block op = builder.AddBlock(defOp);
                using (builder.BlockPlacer.ExpressionBlock())
                {
                    EmitStore store0 = emitExpression(expression.Left);
                    EmitStore store1 = emitExpression(expression.Right);

                    connect(store0, BasicEmitStore.CIn(op, op.Type.Terminals[2]));
                    connect(store1, BasicEmitStore.CIn(op, op.Type.Terminals[1]));
                }

                return BasicEmitStore.COut(op, op.Type.Terminals[0]);
            }

            EmitStore buildOperatorWithBreak(BlockDef defMake, BlockDef defOp)
            {
                Block make = builder.AddBlock(defMake);

                using (builder.BlockPlacer.ExpressionBlock())
                {
                    Block op1 = builder.AddBlock(defOp);

                    EmitStore leftX = null!;
                    EmitStore leftY = null!;
                    EmitStore leftZ = null!;
                    EmitStore rightX = null!;
                    EmitStore rightY = null!;
                    EmitStore rightZ = null!;
                    using (builder.BlockPlacer.ExpressionBlock())
                    {
                        (leftX, leftY, leftZ) = breakVector(expression.Left);

                        if (expression.Right.Type == TypeSymbol.Vector3 || expression.Right.Type == TypeSymbol.Rotation)
                            (rightX, rightY, rightZ) = breakVector(expression.Right);
                        else
                            rightZ = rightY = rightX = emitExpression(expression.Right);
                    }

                    Block op2 = builder.AddBlock(defOp);
                    Block op3 = builder.AddBlock(defOp);

                    // left to op
                    connect(leftX,
                        BasicEmitStore.CIn(op1, op1.Type.Terminals[2]));
                    connect(leftY,
                        BasicEmitStore.CIn(op2, op2.Type.Terminals[2]));
                    connect(leftZ,
                        BasicEmitStore.CIn(op3, op3.Type.Terminals[2]));

                    // right to op
                    connect(rightX,
                        BasicEmitStore.CIn(op1, op1.Type.Terminals[1]));
                    connect(rightY,
                        BasicEmitStore.CIn(op2, op2.Type.Terminals[1]));
                    connect(rightZ,
                        BasicEmitStore.CIn(op3, op3.Type.Terminals[1]));

                    // op to make
                    connect(BasicEmitStore.COut(op1, op1.Type.Terminals[0]),
                        BasicEmitStore.CIn(make, make.Type.Terminals[3]));
                    connect(BasicEmitStore.COut(op2, op2.Type.Terminals[0]),
                        BasicEmitStore.CIn(make, make.Type.Terminals[2]));
                    connect(BasicEmitStore.COut(op3, op3.Type.Terminals[0]),
                        BasicEmitStore.CIn(make, make.Type.Terminals[1]));
                }

                return BasicEmitStore.COut(make, make.Type.Terminals[0]);
            }
        }

        private EmitStore emitVariableExpression(BoundVariableExpression expression)
            => emitGetVariable(expression.Variable);

        private EmitStore emitCallExpression(BoundCallExpression expression)
        {
            FunctionSymbol func = expression.Function;

            if (func is BuiltinFunctionSymbol builtinFunction)
                return builtinFunction.Emit(expression, emitContext);

            EmitStore? firstStore = null;
            EmitStore? lastStore = null;

            for (int i = 0; i < func.Parameters.Length; i++)
            {
                Modifiers mods = func.Parameters[i].Modifiers;

                if (mods.HasFlag(Modifiers.Out))
                    continue;

                EmitStore setStore = emitSetVariable(func.Parameters[i], expression.ArgumentClause.Arguments[i]);

                if (setStore is not NopEmitStore)
                {
                    if (lastStore is not null)
                        connect(lastStore, setStore);

                    firstStore ??= setStore;
                    lastStore = setStore;
                }
            }

            Block callBlock = builder.AddBlock(Blocks.Control.If);

            using (builder.BlockPlacer.ExpressionBlock())
            {
                Block trueBlock = builder.AddBlock(Blocks.Values.True);
                connect(BasicEmitStore.COut(trueBlock, trueBlock.Type.Terminals[0]), BasicEmitStore.CIn(callBlock, callBlock.Type.Terminals[3]));
            }

            calls.AddMultiValue(func, BasicEmitStore.COut(callBlock, callBlock.Type.Terminals[2]));

            if (lastStore is not null)
                connect(lastStore, BasicEmitStore.CIn(callBlock));

            firstStore ??= BasicEmitStore.CIn(callBlock);
            lastStore = BasicEmitStore.COut(callBlock);

            for (int i = 0; i < func.Parameters.Length; i++)
            {
                Modifiers mods = func.Parameters[i].Modifiers;

                if (!mods.HasFlag(Modifiers.Out) && !mods.HasFlag(Modifiers.Ref))
                    continue;

                EmitStore setStore = emitSetExpression(expression.ArgumentClause.Arguments[i], () =>
                {
                    using (builder.BlockPlacer.ExpressionBlock())
                        return emitGetVariable(func.Parameters[i]);
                });

                if (setStore is not NopEmitStore)
                {
                    if (lastStore is not null)
                        connect(lastStore, setStore);

                    lastStore = setStore;
                }
            }

            return new MultiEmitStore(firstStore, lastStore);
        }

        internal void connect(EmitStore from, EmitStore to)
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
            gotosToConnect.AddMultiValue(labelName, store);
        }

        internal EmitStore emitGetVariable(VariableSymbol variable)
        {
            switch (variable)
            {
                case PropertySymbol property:
                    return property.Definition.EmitGet.Invoke(emitContext, property.Expression);
                case NullVariableSymbol:
                    {
                        return new NopEmitStore();
                    }
                default:
                    {
                        if (variable.Modifiers.HasFlag(Modifiers.Inline))
                        {
                            if (inlineVariableInits.TryGetValue(variable, out EmitStore? store))
                                return store;
                            else
                                return new NopEmitStore();
                        }

                        Block block = builder.AddBlock(Blocks.Variables.VariableByType(variable.Type!.ToWireType()));

                        builder.SetBlockValue(block, 0, variable.ResultName);

                        return BasicEmitStore.COut(block, block.Type.Terminals[0]);
                    }
            }
        }
        internal EmitStore emitSetExpression(BoundExpression expression, BoundExpression valueExpression)
            => emitSetExpression(expression, () =>
            {
                builder.BlockPlacer.EnterExpressionBlock();
                EmitStore store = emitExpression(valueExpression);
                builder.BlockPlacer.ExitExpressionBlock();
                return store;
            });
        internal EmitStore emitSetExpression(BoundExpression expression, Func<EmitStore> getValueStore)
        {
            switch (expression)
            {
                case BoundVariableExpression var:
                    return emitSetVariable(var.Variable, getValueStore);
                default:
                    {
                        Block set = builder.AddBlock(Blocks.Variables.Set_PtrByType(expression.Type.ToWireType()));

                        EmitStore exStore = emitExpression(expression);
                        EmitStore valStore = getValueStore();

                        connect(exStore, BasicEmitStore.CIn(set, set.Type.Terminals[2]));
                        connect(valStore, BasicEmitStore.CIn(set, set.Type.Terminals[1]));

                        return new BasicEmitStore(set);
                    }
            }
        }
        internal EmitStore emitSetVariable(VariableSymbol variable, BoundExpression expression)
            => emitSetVariable(variable, () =>
            {
                builder.BlockPlacer.EnterExpressionBlock();
                EmitStore store = emitExpression(expression);
                builder.BlockPlacer.ExitExpressionBlock();
                return store;
            });
        internal EmitStore emitSetVariable(VariableSymbol variable, Func<EmitStore> getValueStore)
        {
            switch (variable)
            {
                case PropertySymbol property:
                    return property.Definition.EmitSet!.Invoke(emitContext, property.Expression, getValueStore);
                case NullVariableSymbol:
                    return new NopEmitStore();
                default:
                    {
                        if (variable.Modifiers.HasFlag(Modifiers.Constant))
                            return new NopEmitStore();
                        else if (variable.Modifiers.HasFlag(Modifiers.Inline))
                        {
                            inlineVariableInits[variable] = getValueStore();
                            return new NopEmitStore();
                        }

                        Block block = builder.AddBlock(Blocks.Variables.Set_VariableByType(variable.Type.ToWireType()));

                        builder.SetBlockValue(block, 0, variable.ResultName);

                        EmitStore valueStore = getValueStore();

                        connect(valueStore, BasicEmitStore.CIn(block, block.Type.Terminals[1]));

                        return new BasicEmitStore(block);
                    }
            }
        }

        /// <summary>
        /// Breaks a vector expression into (x, y, z)
        /// </summary>
        /// <remarks>This method is optimised and may not use the <see cref="Blocks.Math.Break_Vector"/>/Rotation blocks</remarks>
        /// <param name="expression">The vector expression; <see cref="BoundLiteralExpression"/>, <see cref="BoundConstructorExpression"/> or <see cref="BoundVariableExpression"/></param>
        /// <returns>(x, y, z)</returns>
        /// <exception cref="InvalidDataException"></exception>
        internal (EmitStore X, EmitStore Y, EmitStore Z) breakVector(BoundExpression expression)
        {
            var result = breakVectorAny(expression, [true, true, true]);
            return (result[0]!, result[1]!, result[2]!);
        }
        internal EmitStore?[] breakVectorAny(BoundExpression expression, bool[] useComponent)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(useComponent);
            if (useComponent.Length != 3)
                throw new ArgumentException(nameof(useComponent), $"{nameof(useComponent)}.Length must be 3, not '{useComponent.Length}'");

            Vector3F? vector = null;
            if (expression is BoundLiteralExpression literal)
            {
                if (literal.Value is Vector3F vec)
                    vector = vec;
                else if (literal.Value is Rotation rot)
                    vector = rot.Value;
                else
                    throw new InvalidDataException($"Invalid value type '{literal.Value?.GetType()}'");
            }
            else if (expression is BoundConstructorExpression contructor && contructor.ConstantValue is not null)
                vector = contructor.ConstantValue.Value is Vector3F ?
                    (Vector3F)contructor.ConstantValue.Value :
                    ((Rotation)contructor.ConstantValue.Value!).Value;
            else if (expression is BoundVariableExpression variable && variable.ConstantValue is not null)
                vector = variable.ConstantValue.Value is Vector3F ?
                    (Vector3F)variable.ConstantValue.Value :
                    ((Rotation)variable.ConstantValue.Value!).Value;

            if (vector is not null)
                return [
                    useComponent[0] ? emitLiteralExpression(vector.Value.X) : null,
                    useComponent[1] ? emitLiteralExpression(vector.Value.Y) : null,
                    useComponent[2] ? emitLiteralExpression(vector.Value.Z) : null,
                ];
            else if (expression is BoundConstructorExpression contructor)
                return [
                    useComponent[0] ? emitExpression(contructor.ExpressionX) : null,
                    useComponent[1] ? emitExpression(contructor.ExpressionY) : null,
                    useComponent[2] ? emitExpression(contructor.ExpressionZ) : null,
                ];
            else if (expression is BoundVariableExpression var)
            {
                BreakBlockCache cache = (var.Type == TypeSymbol.Vector3 ? vectorBreakCache : rotationBreakCache)
                    .AddIfAbsent(var.Variable, new BreakBlockCache());
                if (!cache.TryGet(out Block? block))
                {
                    block = builder.AddBlock(var.Type == TypeSymbol.Vector3 ? Blocks.Math.Break_Vector : Blocks.Math.Break_Rotation);
                    cache.SetNewBlock(block);

                    using (builder.BlockPlacer.ExpressionBlock())
                    {
                        EmitStore store = emitVariableExpression(var);
                        connect(store, BasicEmitStore.CIn(block, block.Type.Terminals[3]));
                    }
                }

                return [
                    useComponent[0] ? BasicEmitStore.COut(block, block.Type.Terminals[2]) : null,
                    useComponent[1] ? BasicEmitStore.COut(block, block.Type.Terminals[1]) : null,
                    useComponent[2] ? BasicEmitStore.COut(block, block.Type.Terminals[0]) : null,
                ];
            }
            else
            { // just break it
                Block block = builder.AddBlock(expression.Type == TypeSymbol.Vector3 ? Blocks.Math.Break_Vector : Blocks.Math.Break_Rotation);

                using (builder.BlockPlacer.ExpressionBlock())
                {
                    EmitStore store = emitExpression(expression);
                    connect(store, BasicEmitStore.CIn(block, block.Type.Terminals[3]));
                }

                return [
                    useComponent[0] ? BasicEmitStore.COut(block, block.Type.Terminals[2]) : null,
                    useComponent[1] ? BasicEmitStore.COut(block, block.Type.Terminals[1]) : null,
                    useComponent[2] ? BasicEmitStore.COut(block, block.Type.Terminals[0]) : null,
                ];
            }
        }

        internal object?[]? validateConstants(ReadOnlyMemory<BoundExpression> expressions, bool mustBeConstant)
        {
            ReadOnlySpan<BoundExpression> expressionsMem = expressions.Span;

            object?[] values = new object?[expressionsMem.Length];
            bool invalid = false;

            for (int i = 0; i < expressionsMem.Length; i++)
            {
                BoundConstant? constant = expressionsMem[i].ConstantValue;
                if (constant is null)
                {
                    if (mustBeConstant)
                        diagnostics.ReportValueMustBeConstant(expressionsMem[i].Syntax.Location);
                    invalid = true;
                }
                else
                    values[i] = constant.Value;
            }

            if (invalid)
                return null;
            else
                return values;
        }

        internal void writeComment(string text)
        {
            for (int i = 0; i < text.Length; i += FancadeConstants.MaxCommentLength)
            {
                Block block = builder.AddBlock(Blocks.Values.Comment);
                builder.SetBlockValue(block, 0, text.Substring(i, Math.Min(FancadeConstants.MaxCommentLength, text.Length - i)));
            }
        }
    }
}
