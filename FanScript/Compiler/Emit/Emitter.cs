using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit.Utils;
using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace FanScript.Compiler.Emit
{
    internal sealed class Emitter : IEmitContext
    {
        public DiagnosticBag Diagnostics { get; private set; } = new DiagnosticBag();
        private BoundProgram program = null!;
        private CodePlacer placer = null!;
        public BlockBuilder Builder { get; private set; } = null!;

        private FunctionSymbol currentFunciton = null!;

        // key - a label before antoher label, item - the label after key
        private Dictionary<string, string> sameTargetLabels = new();
        // key - label name, item - list of goto "origins", not only gotos but also statements just before the label
        private ListMultiValueDictionary<string, EmitStore> gotosToConnect = new();
        // key - label name, item - the store to connect gotos to
        private Dictionary<string, EmitStore> afterLabel = new();

        private readonly InlineVarManager inlineVarManager = new();

        private Dictionary<VariableSymbol, BreakBlockCache> vectorBreakCache = new();
        private Dictionary<VariableSymbol, BreakBlockCache> rotationBreakCache = new();

        private Stack<List<EmitStore>> beforeReturnStack = new();
        private Dictionary<FunctionSymbol, EmitStore> functions = new();
        private ListMultiValueDictionary<FunctionSymbol, EmitStore> calls = new();

        private Emitter()
        {
        }

        public static ImmutableArray<Diagnostic> Emit(BoundProgram program, CodePlacer placer, BlockBuilder builder)
        {
            if (program.Diagnostics.HasErrors())
                return program.Diagnostics;
            else
            {
                Emitter emitter = new Emitter();
                return emitter.emit(program, placer, builder);
            }
        }

        private ImmutableArray<Diagnostic> emit(BoundProgram program, CodePlacer placer, BlockBuilder builder)
        {
            this.program = program;
            this.placer = placer;
            this.Builder = builder;

            vectorBreakCache.Clear();
            rotationBreakCache.Clear();

            foreach (var (func, body) in this.program.Functions.ToImmutableSortedDictionary())
            {
                if (func != program.ScriptFunction && program.Analysis.ShouldFunctionGetInlined(func))
                    continue;

                currentFunciton = func;

                using (StatementBlock())
                {
                    WriteComment(func.Name);

                    beforeReturnStack.Push(new List<EmitStore>());
                    functions.Add(func, EmitStatement(body));
                    beforeReturnStack.Pop();

                    processLabelsAndGotos();
                }

                currentFunciton = null!;
            }

            processCalls();

            return Diagnostics
                .Concat(this.program.Diagnostics)
                .ToImmutableArray();
        }

        private void processLabelsAndGotos()
        {
            foreach (var (labelName, stores) in gotosToConnect)
            {
                if (!tryGetAfterLabel(labelName, out EmitStore? afterLabel))
                    continue;

                foreach (EmitStore store in stores)
                    Builder.Connect(store, afterLabel);
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

        private void processCalls()
        {
            foreach (var (func, callList) in calls)
            {
                if (!functions.TryGetValue(func, out var funcStore))
                    throw new Exception($"Failed to get entry point for function '{func}'.");

                for (var i = 0; i < callList.Count; i++)
                    Connect(callList[i], funcStore);
            }

            functions.Clear();
            calls.Clear();
        }

        public EmitStore EmitStatement(BoundStatement statement)
        {
            EmitStore store = statement switch
            {
                BoundBlockStatement blockStatement => emitBlockStatement(blockStatement),
                BoundVariableDeclarationStatement variableDeclarationStatement when variableDeclarationStatement.OptionalAssignment is not null => EmitStatement(variableDeclarationStatement.OptionalAssignment),
                BoundVariableDeclarationStatement => NopEmitStore.Instance,
                BoundAssignmentStatement assignmentStatement => emitAssigmentStatement(assignmentStatement),
                BoundPostfixStatement postfixStatement => emitPostfixStatement(postfixStatement),
                BoundPrefixStatement prefixStatement => emitPrefixStatement(prefixStatement),
                BoundGotoStatement gotoStatement => emitGotoStatement(gotoStatement),
                BoundRollbackGotoStatement rollbackGotoStatement => emitRollbackGotoStatement(rollbackGotoStatement),
                BoundConditionalGotoStatement conditionalGotoStatement when conditionalGotoStatement.Condition is BoundEventCondition condition => emitEventStatement(condition.EventType, condition.ArgumentClause?.Arguments, conditionalGotoStatement.Label),
                BoundConditionalGotoStatement conditionalGotoStatement => emitConditionalGotoStatement(conditionalGotoStatement),
                BoundLabelStatement labelStatement => emitLabelStatement(labelStatement),
                BoundReturnStatement returnStatement => emitReturnStatement(returnStatement),
                BoundEmitterHintStatement emitterHintStatement => emitHint(emitterHintStatement),
                BoundCallStatement callStatement => emitCallStatement(callStatement),
                BoundExpressionStatement expressionStatement => emitExpressionStatement(expressionStatement),
                BoundNopStatement => NopEmitStore.Instance,

                _ => throw new UnexpectedBoundNodeException(statement),
            };

            return store;
        }

        private EmitStore emitBlockStatement(BoundBlockStatement statement)
        {
            if (statement.Statements.Length == 0)
                return NopEmitStore.Instance;
            else if (statement.Statements.Length == 1 && statement.Statements[0] is BoundBlockStatement inBlock)
                return emitBlockStatement(inBlock);

            EmitConnector connector = new EmitConnector(Connect);

            bool newCodeBlock = placer.CurrentCodeBlockBlocks > 0;
            if (newCodeBlock)
                enterStatementBlock();

            for (int i = 0; i < statement.Statements.Length; i++)
            {
                EmitStore statementStore = EmitStatement(statement.Statements[i]);

                connector.Add(statementStore);
            }

            if (newCodeBlock)
                exitStatementBlock();

            return connector.Store;
        }

        private EmitStore emitEventStatement(EventType type, ImmutableArray<BoundExpression>? arguments, BoundLabel onTrueLabel)
        {
            BlockDef def;
            switch (type)
            {
                case EventType.Play:
                    def = Blocks.Control.PlaySensor;
                    break;
                case EventType.LateUpdate:
                    def = Blocks.Control.LateUpdate;
                    break;
                case EventType.BoxArt:
                    def = Blocks.Control.BoxArtSensor;
                    break;
                case EventType.Touch:
                    def = Blocks.Control.TouchSensor;
                    break;
                case EventType.Swipe:
                    def = Blocks.Control.SwipeSensor;
                    break;
                case EventType.Button:
                    def = Blocks.Control.Button;
                    break;
                case EventType.Collision:
                    def = Blocks.Control.Collision;
                    break;
                case EventType.Loop:
                    def = Blocks.Control.Loop;
                    break;
                default:
                    throw new UnknownEnumValueException<EventType>(type);
            }

            Block block = AddBlock(def);

            enterStatementBlock();

            switch (type)
            {
                case EventType.Touch:
                    {
                        object?[]? values = ValidateConstants(arguments!.Value.AsMemory(2..), true);
                        if (values is null)
                            break;

                        for (int i = 0; i < values.Length; i++)
                            SetBlockValue(block, i, (byte)((float?)values[i] ?? 0f)); // unbox, then cast

                        connectToLabel(onTrueLabel.Name, placeAndConnectRefArgs(arguments!.Value.AsSpan(..2)));
                        return new BasicEmitStore(block);
                    }
                case EventType.Swipe:
                    {
                        connectToLabel(onTrueLabel.Name, placeAndConnectRefArgs(arguments!.Value.AsSpan()));
                        return new BasicEmitStore(block);
                    }
                case EventType.Button:
                    {
                        object?[]? values = ValidateConstants(arguments!.Value.AsMemory(), true);
                        if (values is null)
                            break;

                        for (int i = 0; i < values.Length; i++)
                            SetBlockValue(block, i, (byte)((float?)values[i] ?? 0f)); // unbox, then cast
                    }
                    break;
                case EventType.Collision:
                    {
                        connectToLabel(onTrueLabel.Name, placeAndConnectRefArgs(arguments!.Value.AsSpan(1..), arguments!.Value.AsMemory(..1)));
                        return new BasicEmitStore(block);
                    }
                case EventType.Loop:
                    {
                        connectToLabel(onTrueLabel.Name, placeAndConnectRefArgs(arguments!.Value.AsSpan(2..), arguments!.Value.AsMemory(..2)));
                        return new BasicEmitStore(block);
                    }
            }

            connectToLabel(onTrueLabel.Name, BasicEmitStore.COut(block, block.Type.TerminalArray[^2]));

            return new BasicEmitStore(block);

            EmitStore placeAndConnectRefArgs(ReadOnlySpan<BoundExpression> outArguments, ReadOnlyMemory<BoundExpression>? arguments = null)
            {
                arguments ??= ReadOnlyMemory<BoundExpression>.Empty;

                EmitStore lastStore = BasicEmitStore.COut(block, block.Type.TerminalArray[Index.FromEnd(2 + arguments.Value.Length)]);

                if (arguments.Value.Length != 0)
                {
                    exitStatementBlock();

                    using (ExpressionBlock())
                    {
                        var argumentsSpan = arguments.Value.Span;

                        for (int i = 0; i < argumentsSpan.Length; i++)
                        {
                            EmitStore store = EmitExpression(argumentsSpan[i]);

                            Connect(store, BasicEmitStore.CIn(block, block.Type.TerminalArray[Index.FromEnd(i + 2)]));
                        }
                    }

                    enterStatementBlock();
                }

                for (int i = 0; i < outArguments.Length; i++)
                {
                    VariableSymbol variable = ((BoundVariableExpression)outArguments[i]).Variable;

                    EmitStore varStore = EmitSetVariable(variable, () => BasicEmitStore.COut(block, block.Type.TerminalArray[Index.FromEnd(i + arguments.Value.Length + 3)]));

                    Connect(lastStore, varStore);

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
                case PostfixKind.Increment:
                    def = Blocks.Variables.PlusPlusFloat;
                    break;
                case PostfixKind.Decrement:
                    def = Blocks.Variables.MinusMinusFloat;
                    break;
                default:
                    throw new UnknownEnumValueException<PostfixKind>(statement.PostfixKind);
            }

            Block block = AddBlock(def);

            using (ExpressionBlock())
            {
                EmitStore store = EmitGetVariable(statement.Variable);

                Connect(store, BasicEmitStore.CIn(block, block.Type.Terminals["Variable"]));
            }

            return new BasicEmitStore(block);
        }

        private EmitStore emitPrefixStatement(BoundPrefixStatement statement)
        {
            BlockDef def;
            switch (statement.PrefixKind)
            {
                case PrefixKind.Increment:
                    def = Blocks.Variables.PlusPlusFloat;
                    break;
                case PrefixKind.Decrement:
                    def = Blocks.Variables.MinusMinusFloat;
                    break;
                default:
                    throw new UnknownEnumValueException<PrefixKind>(statement.PrefixKind);
            }

            Block block = AddBlock(def);

            using (ExpressionBlock())
            {
                EmitStore store = EmitGetVariable(statement.Variable);

                Connect(store, BasicEmitStore.CIn(block, block.Type.Terminals["Variable"]));
            }

            return new BasicEmitStore(block);
        }

        private EmitStore emitGotoStatement(BoundGotoStatement statement)
            => new GotoEmitStore(statement.Label.Name);

        private EmitStore emitRollbackGotoStatement(BoundRollbackGotoStatement statement)
            => RollbackEmitStore.Instance;

        private EmitStore emitConditionalGotoStatement(BoundConditionalGotoStatement statement)
        {
            Block block = AddBlock(Blocks.Control.If);

            using (ExpressionBlock())
            {
                EmitStore condition = EmitExpression(statement.Condition);

                Connect(condition, BasicEmitStore.CIn(block, block.Type.Terminals["Condition"]));
            }

            ConditionalGotoEmitStore store = new ConditionalGotoEmitStore(block, block.Type.Before,
                block, block.Type.Terminals[statement.JumpIfTrue ? "True" : "False"], block,
                block.Type.Terminals[statement.JumpIfTrue ? "False" : "True"]);

            connectToLabel(statement.Label.Name, BasicEmitStore.COut(store.OnCondition, store.OnConditionTerminal));
            return store;

        }

        private EmitStore emitLabelStatement(BoundLabelStatement statement)
        {
            return new LabelEmitStore(statement.Label.Name);
        }

        private EmitStore emitReturnStatement(BoundReturnStatement statement)
        {
            if (statement.Expression is null)
                return new ReturnEmitStore();

            return emitSetVariable(ReservedCompilerVariableSymbol.CreateFunctionRes(currentFunciton), statement.Expression);
        }

        private EmitStore emitHint(BoundEmitterHintStatement statement)
        {
            switch (statement.Hint)
            {
                case BoundEmitterHintStatement.HintKind.StatementBlockStart:
                    enterStatementBlock();
                    break;
                case BoundEmitterHintStatement.HintKind.StatementBlockEnd:
                    exitStatementBlock();
                    break;
                case BoundEmitterHintStatement.HintKind.HighlightStart:
                    placer.EnterHighlight();
                    break;
                case BoundEmitterHintStatement.HintKind.HighlightEnd:
                    placer.ExitHightlight();
                    break;
                default:
                    throw new UnknownEnumValueException<BoundEmitterHintStatement.HintKind>(statement.Hint);
            }

            return NopEmitStore.Instance;
        }

        private EmitStore emitCallStatement(BoundCallStatement statement)
        {
            if (statement.Function is BuiltinFunctionSymbol builtinFunction)
                return builtinFunction.Emit(new BoundCallExpression(statement.Syntax, statement.Function, statement.ArgumentClause, statement.ReturnType, statement.GenericType), this);

            Debug.Assert(!statement.Function.Modifiers.HasFlag(Modifiers.Inline));

            FunctionSymbol func = statement.Function;

            EmitConnector connector = new EmitConnector(Connect);

            for (int i = 0; i < func.Parameters.Length; i++)
            {
                Modifiers mods = func.Parameters[i].Modifiers;

                if (mods.HasFlag(Modifiers.Out))
                    continue;

                EmitStore setStore = emitSetVariable(func.Parameters[i], statement.ArgumentClause.Arguments[i]);

                connector.Add(setStore);
            }

            Block callBlock = AddBlock(Blocks.Control.If);

            using (ExpressionBlock())
            {
                Block trueBlock = AddBlock(Blocks.Values.True);
                Connect(BasicEmitStore.COut(trueBlock, trueBlock.Type.Terminals["True"]), BasicEmitStore.CIn(callBlock, callBlock.Type.Terminals["Condition"]));
            }

            calls.Add(func, BasicEmitStore.COut(callBlock, callBlock.Type.Terminals["True"]));

            connector.Add(new BasicEmitStore(callBlock));

            for (int i = 0; i < func.Parameters.Length; i++)
            {
                Modifiers mods = func.Parameters[i].Modifiers;

                if (!mods.HasFlag(Modifiers.Out) && !mods.HasFlag(Modifiers.Ref))
                    continue;

                EmitStore setStore = EmitSetExpression(statement.ArgumentClause.Arguments[i], () =>
                {
                    using (ExpressionBlock())
                        return EmitGetVariable(func.Parameters[i]);
                });

                connector.Add(setStore);
            }

            if (func.Type != TypeSymbol.Void && statement.ResultVariable is not null)
            {
                EmitStore setStore = EmitSetVariable(statement.ResultVariable, () =>
                {
                    using (ExpressionBlock())
                        return EmitGetVariable(ReservedCompilerVariableSymbol.CreateFunctionRes(func));
                });

                connector.Add(setStore);
            }

            return connector.Store;
        }

        private EmitStore emitExpressionStatement(BoundExpressionStatement statement)
        {
            if (statement.Expression is BoundNopExpression)
                return NopEmitStore.Instance;

            return EmitExpression(statement.Expression);
        }

        public EmitStore EmitExpression(BoundExpression expression)
        {
            EmitStore store = expression switch
            {
                BoundLiteralExpression literalExpression => emitLiteralExpression(literalExpression),
                BoundConstructorExpression constructorExpression => emitConstructorExpression(constructorExpression),
                BoundUnaryExpression unaryExpression => emitUnaryExpression(unaryExpression),
                BoundBinaryExpression binaryExpression => emitBinaryExpression(binaryExpression),
                BoundVariableExpression variableExpression => emitVariableExpression(variableExpression),
                BoundCallExpression callExpression => emitCallExpression(callExpression),

                _ => throw new UnexpectedBoundNodeException(expression),
            };

            return store;
        }

        private EmitStore emitLiteralExpression(BoundLiteralExpression expression)
            => EmitLiteralExpression(expression.Value);
        public EmitStore EmitLiteralExpression(object? value)
        {
            if (value is null)
                return NopEmitStore.Instance;

            Block block = AddBlock(Blocks.Values.ValueByType(value));

            if (value is not bool)
                SetBlockValue(block, 0, value);

            return BasicEmitStore.COut(block, block.Type.TerminalArray[0]);
        }

        private EmitStore emitConstructorExpression(BoundConstructorExpression expression)
        {
            if (expression.ConstantValue is not null)
                return EmitLiteralExpression(expression.ConstantValue.Value);

            BlockDef def = Blocks.Math.MakeByType(expression.Type.ToWireType());
            Block block = AddBlock(def);

            using (ExpressionBlock())
            {
                EmitStore xStore = EmitExpression(expression.ExpressionX);
                EmitStore yStore = EmitExpression(expression.ExpressionY);
                EmitStore zStore = EmitExpression(expression.ExpressionZ);

                Connect(xStore, BasicEmitStore.CIn(block, def.Terminals["X"]));
                Connect(yStore, BasicEmitStore.CIn(block, def.Terminals["Y"]));
                Connect(zStore, BasicEmitStore.CIn(block, def.Terminals["Z"]));
            }

            return BasicEmitStore.COut(block, def.TerminalArray[0]);
        }

        private EmitStore emitUnaryExpression(BoundUnaryExpression expression)
        {
            switch (expression.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return EmitExpression(expression.Operand);
                case BoundUnaryOperatorKind.Negation:
                    {
                        if (expression.Type == TypeSymbol.Vector3)
                        {
                            Block block = AddBlock(Blocks.Math.Multiply_Vector);

                            using (ExpressionBlock())
                            {
                                EmitStore opStore = EmitExpression(expression.Operand);

                                Block numb = AddBlock(Blocks.Values.Number);
                                SetBlockValue(numb, 0, -1f);

                                Connect(opStore, BasicEmitStore.CIn(block, block.Type.Terminals["Vec"]));
                                Connect(BasicEmitStore.COut(numb, numb.Type.Terminals["Number"]), BasicEmitStore.CIn(block, block.Type.Terminals["Num"]));
                            }

                            return BasicEmitStore.COut(block, block.Type.Terminals["Vec * Num"]);
                        }
                        else
                        {

                            Block block = AddBlock(expression.Type == TypeSymbol.Float ? Blocks.Math.Negate : Blocks.Math.Inverse);

                            using (ExpressionBlock())
                            {
                                EmitStore opStore = EmitExpression(expression.Operand);

                                Connect(opStore, BasicEmitStore.CIn(block, block.Type.TerminalArray[1]));
                            }

                            return BasicEmitStore.COut(block, block.Type.TerminalArray[0]);
                        }
                    }
                case BoundUnaryOperatorKind.LogicalNegation:
                    {
                        Block block = AddBlock(Blocks.Math.Not);

                        using (ExpressionBlock())
                        {
                            EmitStore opStore = EmitExpression(expression.Operand);

                            Connect(opStore, BasicEmitStore.CIn(block, block.Type.Terminals["Tru"]));
                        }

                        return BasicEmitStore.COut(block, block.Type.Terminals["Not Tru"]);
                    }
                default:
                    throw new UnknownEnumValueException<BoundUnaryOperatorKind>(expression.Op.Kind);
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
                    throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
            }

            if (expression.Op.Kind == BoundBinaryOperatorKind.NotEquals
                || expression.Op.Kind == BoundBinaryOperatorKind.LessOrEquals
                || expression.Op.Kind == BoundBinaryOperatorKind.GreaterOrEquals)
            {
                // invert output, >= or <=, >= can be accomplished as inverted <
                Block not = AddBlock(Blocks.Math.Not);
                using (ExpressionBlock())
                {
                    Block block = AddBlock(op);

                    Connect(BasicEmitStore.COut(block, block.Type.TerminalArray[0]),
                        BasicEmitStore.CIn(not, not.Type.Terminals["Tru"]));

                    using (ExpressionBlock())
                    {
                        EmitStore store0 = EmitExpression(expression.Left);
                        EmitStore store1 = EmitExpression(expression.Right);

                        Connect(store0, BasicEmitStore.CIn(block, block.Type.TerminalArray[2]));
                        Connect(store1, BasicEmitStore.CIn(block, block.Type.TerminalArray[1]));
                    }
                }

                return BasicEmitStore.COut(not, not.Type.Terminals["Not Tru"]);
            }
            else
            {
                Block block = AddBlock(op);
                using (ExpressionBlock())
                {
                    EmitStore store0 = EmitExpression(expression.Left);
                    EmitStore store1 = EmitExpression(expression.Right);

                    Connect(store0, BasicEmitStore.CIn(block, block.Type.TerminalArray[2]));
                    Connect(store1, BasicEmitStore.CIn(block, block.Type.TerminalArray[1]));
                }

                return BasicEmitStore.COut(block, block.Type.TerminalArray[0]);
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
                        throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
                    break;
                case BoundBinaryOperatorKind.Division:
                case BoundBinaryOperatorKind.Modulo:
                    break; // supported, but not one block
                case BoundBinaryOperatorKind.Equals:
                    if (expression.Left.Type == TypeSymbol.Vector3)
                        defOp = Blocks.Math.Equals_Vector;
                    else
                        throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
                    // rotation doesn't have equals???
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    break; // supported, but not one block
                default:
                    throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
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
                                throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);

                            Block not = AddBlock(Blocks.Math.Not);
                            using (ExpressionBlock())
                            {
                                Block op = AddBlock(defOp);

                                Connect(BasicEmitStore.COut(op, op.Type.TerminalArray[0]),
                                    BasicEmitStore.CIn(not, not.Type.Terminals["Tru"]));

                                using (ExpressionBlock())
                                {
                                    EmitStore store0 = EmitExpression(expression.Left);
                                    EmitStore store1 = EmitExpression(expression.Right);

                                    Connect(store0, BasicEmitStore.CIn(op, op.Type.TerminalArray[2]));
                                    Connect(store1, BasicEmitStore.CIn(op, op.Type.TerminalArray[1]));
                                }
                            }

                            return BasicEmitStore.COut(not, not.Type.Terminals["Not Tru"]);
                        }
                    default:
                        throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
                }
            }
            else
            {
                Block op = AddBlock(defOp);
                using (ExpressionBlock())
                {
                    EmitStore store0 = EmitExpression(expression.Left);
                    EmitStore store1 = EmitExpression(expression.Right);

                    Connect(store0, BasicEmitStore.CIn(op, op.Type.TerminalArray[2]));
                    Connect(store1, BasicEmitStore.CIn(op, op.Type.TerminalArray[1]));
                }

                return BasicEmitStore.COut(op, op.Type.TerminalArray[0]);
            }

            EmitStore buildOperatorWithBreak(BlockDef defMake, BlockDef defOp)
            {
                Block make = AddBlock(defMake);

                using (ExpressionBlock())
                {
                    Block op1 = AddBlock(defOp);

                    EmitStore leftX;
                    EmitStore leftY;
                    EmitStore leftZ;
                    EmitStore rightX;
                    EmitStore rightY;
                    EmitStore rightZ;
                    using (ExpressionBlock())
                    {
                        (leftX, leftY, leftZ) = BreakVector(expression.Left);

                        if (expression.Right.Type == TypeSymbol.Vector3 || expression.Right.Type == TypeSymbol.Rotation)
                            (rightX, rightY, rightZ) = BreakVector(expression.Right);
                        else
                            rightZ = rightY = rightX = EmitExpression(expression.Right);
                    }

                    Block op2 = AddBlock(defOp);
                    Block op3 = AddBlock(defOp);

                    // left to op
                    Connect(leftX,
                        BasicEmitStore.CIn(op1, op1.Type.TerminalArray[2]));
                    Connect(leftY,
                        BasicEmitStore.CIn(op2, op2.Type.TerminalArray[2]));
                    Connect(leftZ,
                        BasicEmitStore.CIn(op3, op3.Type.TerminalArray[2]));

                    // right to op
                    Connect(rightX,
                        BasicEmitStore.CIn(op1, op1.Type.TerminalArray[1]));
                    Connect(rightY,
                        BasicEmitStore.CIn(op2, op2.Type.TerminalArray[1]));
                    Connect(rightZ,
                        BasicEmitStore.CIn(op3, op3.Type.TerminalArray[1]));

                    // op to make
                    Connect(BasicEmitStore.COut(op1, op1.Type.TerminalArray[0]),
                        BasicEmitStore.CIn(make, make.Type.TerminalArray[3]));
                    Connect(BasicEmitStore.COut(op2, op2.Type.TerminalArray[0]),
                        BasicEmitStore.CIn(make, make.Type.TerminalArray[2]));
                    Connect(BasicEmitStore.COut(op3, op3.Type.TerminalArray[0]),
                        BasicEmitStore.CIn(make, make.Type.TerminalArray[1]));
                }

                return BasicEmitStore.COut(make, make.Type.TerminalArray[0]);
            }
        }

        private EmitStore emitVariableExpression(BoundVariableExpression expression)
            => EmitGetVariable(expression.Variable);

        private EmitStore emitCallExpression(BoundCallExpression expression)
        {
            if (expression.Function is BuiltinFunctionSymbol builtinFunction)
                return builtinFunction.Emit(expression, this);

            Debug.Fail("User defined function calls should be extracted to statement calls.");
            return NopEmitStore.Instance;
        }

        #region Utils
        public EmitStore EmitGetVariable(VariableSymbol variable)
        {
            switch (variable)
            {
                case PropertySymbol property:
                    return property.Definition.EmitGet.Invoke(this, property.Expression);
                case NullVariableSymbol:
                    {
                        return NopEmitStore.Instance;
                    }
                default:
                    {
                        if (variable.Modifiers.HasFlag(Modifiers.Inline))
                        {
                            if (inlineVarManager.TryGet(variable, this, out var store))
                                return store;
                            else
                            {
                                Diagnostics.ReportTooManyInlineVariableUses(Text.TextLocation.None, variable.Name);
                                return NopEmitStore.Instance;
                            }
                        }

                        Block block = AddBlock(Blocks.Variables.VariableByType(variable.Type!.ToWireType()));

                        SetBlockValue(block, 0, variable.ResultName);

                        return BasicEmitStore.COut(block, block.Type.TerminalArray[0]);
                    }
            }
        }
        private EmitStore emitSetExpression(BoundExpression expression, BoundExpression valueExpression)
            => EmitSetExpression(expression, () =>
            {
                using (ExpressionBlock())
                {
                    EmitStore store = EmitExpression(valueExpression);

                    return store;
                }
            });
        public EmitStore EmitSetExpression(BoundExpression expression, Func<EmitStore> getValueStore)
        {
            switch (expression)
            {
                case BoundVariableExpression var:
                    return EmitSetVariable(var.Variable, getValueStore);
                default:
                    {
                        Block set = AddBlock(Blocks.Variables.Set_PtrByType(expression.Type.ToWireType()));

                        EmitStore exStore = EmitExpression(expression);
                        EmitStore valStore = getValueStore();

                        Connect(exStore, BasicEmitStore.CIn(set, set.Type.Terminals["Variable"]));
                        Connect(valStore, BasicEmitStore.CIn(set, set.Type.Terminals["Value"]));

                        return new BasicEmitStore(set);
                    }
            }
        }
        private EmitStore emitSetVariable(VariableSymbol variable, BoundExpression expression)
            => EmitSetVariable(variable, () =>
            {
                using (ExpressionBlock())
                {
                    EmitStore store = EmitExpression(expression);
                    return store;
                }
            });
        public EmitStore EmitSetVariable(VariableSymbol variable, Func<EmitStore> getValueStore)
        {
            switch (variable)
            {
                case PropertySymbol property:
                    return property.Definition.EmitSet!.Invoke(this, property.Expression, getValueStore);
                case NullVariableSymbol:
                    return NopEmitStore.Instance;
                default:
                    {
                        if (variable.Modifiers.HasFlag(Modifiers.Constant))
                            return NopEmitStore.Instance;
                        else if (variable.Modifiers.HasFlag(Modifiers.Inline))
                        {
                            inlineVarManager.Set(variable, getValueStore());
                            return NopEmitStore.Instance;
                        }

                        Block block = AddBlock(Blocks.Variables.Set_VariableByType(variable.Type.ToWireType()));

                        SetBlockValue(block, 0, variable.ResultName);

                        EmitStore valueStore = getValueStore();

                        Connect(valueStore, BasicEmitStore.CIn(block, block.Type.TerminalArray[1]));

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
        public (EmitStore X, EmitStore Y, EmitStore Z) BreakVector(BoundExpression expression)
        {
            var result = BreakVectorAny(expression, [true, true, true]);
            return (result[0]!, result[1]!, result[2]!);
        }
        public EmitStore?[] BreakVectorAny(BoundExpression expression, bool[] useComponent)
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
            else if (expression is BoundVariableExpression variable && variable.Variable.Modifiers.HasFlag(Modifiers.Constant) && variable.ConstantValue is not null)
                vector = variable.ConstantValue.Value is Vector3F ?
                    (Vector3F)variable.ConstantValue.Value :
                    ((Rotation)variable.ConstantValue.Value!).Value;

            if (vector is not null)
                return [
                    useComponent[0] ? EmitLiteralExpression(vector.Value.X) : null,
                    useComponent[1] ? EmitLiteralExpression(vector.Value.Y) : null,
                    useComponent[2] ? EmitLiteralExpression(vector.Value.Z) : null,
                ];
            else if (expression is BoundConstructorExpression contructor)
                return [
                    useComponent[0] ? EmitExpression(contructor.ExpressionX) : null,
                    useComponent[1] ? EmitExpression(contructor.ExpressionY) : null,
                    useComponent[2] ? EmitExpression(contructor.ExpressionZ) : null,
                ];
            else if (expression is BoundVariableExpression var)
            {
                BreakBlockCache cache = (var.Type == TypeSymbol.Vector3 ? vectorBreakCache : rotationBreakCache)
                    .AddIfAbsent(var.Variable, new BreakBlockCache());
                if (!cache.TryGet(out Block? block))
                {
                    block = AddBlock(var.Type == TypeSymbol.Vector3 ? Blocks.Math.Break_Vector : Blocks.Math.Break_Rotation);
                    cache.SetNewBlock(block);

                    using (ExpressionBlock())
                    {
                        EmitStore store = emitVariableExpression(var);
                        Connect(store, BasicEmitStore.CIn(block, block.Type.TerminalArray[3]));
                    }
                }

                return [
                    useComponent[0] ? BasicEmitStore.COut(block, block.Type.TerminalArray[2]) : null,
                    useComponent[1] ? BasicEmitStore.COut(block, block.Type.TerminalArray[1]) : null,
                    useComponent[2] ? BasicEmitStore.COut(block, block.Type.TerminalArray[0]) : null,
                ];
            }
            else
            {
                // just break it
                Block block = AddBlock(expression.Type == TypeSymbol.Vector3 ? Blocks.Math.Break_Vector : Blocks.Math.Break_Rotation);

                using (ExpressionBlock())
                {
                    EmitStore store = EmitExpression(expression);
                    Connect(store, BasicEmitStore.CIn(block, block.Type.TerminalArray[3]));
                }

                return [
                    useComponent[0] ? BasicEmitStore.COut(block, block.Type.TerminalArray[2]) : null,
                    useComponent[1] ? BasicEmitStore.COut(block, block.Type.TerminalArray[1]) : null,
                    useComponent[2] ? BasicEmitStore.COut(block, block.Type.TerminalArray[0]) : null,
                ];
            }
        }

        public object?[]? ValidateConstants(ReadOnlyMemory<BoundExpression> expressions, bool mustBeConstant)
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
                        Diagnostics.ReportValueMustBeConstant(expressionsMem[i].Syntax.Location);
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

        public void WriteComment(string text)
        {
            foreach (string line in StringExtensions.SplitByMaxLength(text, FancadeConstants.MaxCommentLength))
            {
                Block block = AddBlock(Blocks.Values.Comment);
                SetBlockValue(block, 0, line);
            }

            //for (int i = 0; i < text.Length; i += FancadeConstants.MaxCommentLength)
            //{
            //    Block block = addBlock(Blocks.Values.Comment);
            //    setBlockValue(block, 0, text.Substring(i, Math.Min(FancadeConstants.MaxCommentLength, text.Length - i)));
            //}
        }

        public EmitStore EmitSetArraySegment(BoundArraySegmentExpression segment, BoundExpression arrayVariable, BoundExpression startIndex)
        {
            Debug.Assert(arrayVariable.Type.InnerType == segment.ElementType);
            Debug.Assert(startIndex.Type == TypeSymbol.Float);

            WireType type = arrayVariable.Type.ToWireType();

            EmitConnector connector = new EmitConnector(Connect);

            EmitStore? lastElementStore = null;

            for (int i = 0; i < segment.Elements.Length; i++)
            {
                if (i == 0 && startIndex.ConstantValue is not null && (float)startIndex.ConstantValue.GetValueOrDefault(TypeSymbol.Float) == 0f)
                    connector.Add(emitSetExpression(arrayVariable, segment.Elements[i]));
                else
                {
                    Block setBlock = AddBlock(Blocks.Variables.Set_PtrByType(type));

                    connector.Add(new BasicEmitStore(setBlock));

                    using (ExpressionBlock())
                    {
                        Block listBlock = AddBlock(Blocks.Variables.ListByType(type));

                        Connect(BasicEmitStore.COut(listBlock, listBlock.Type.Terminals["Element"]), BasicEmitStore.CIn(setBlock, setBlock.Type.Terminals["Variable"]));

                        using (ExpressionBlock())
                        {
                            lastElementStore ??= EmitExpression(arrayVariable);

                            Connect(lastElementStore, BasicEmitStore.CIn(listBlock, listBlock.Type.Terminals["Variable"]));

                            lastElementStore = BasicEmitStore.COut(listBlock, listBlock.Type.Terminals["Element"]);

                            Connect(i == 0 ? EmitExpression(startIndex) : EmitLiteralExpression(1f), BasicEmitStore.CIn(listBlock, listBlock.Type.Terminals["Index"]));
                        }

                        Connect(EmitExpression(segment.Elements[i]), BasicEmitStore.CIn(setBlock, setBlock.Type.Terminals["Value"]));
                    }
                }
            }

            return connector.Store;
        }
        #endregion

        #region CodePlacer and BlockBuilder redirects
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block AddBlock(BlockDef def)
            => placer.PlaceBlock(def);
        public void Connect(EmitStore from, EmitStore to)
        {
            while (from is MultiEmitStore multi)
                from = multi.OutStore;
            while (to is MultiEmitStore multi)
                to = multi.InStore;

            if (from is RollbackEmitStore || to is RollbackEmitStore)
            {
                if (to is ReturnEmitStore && from is not RollbackEmitStore && beforeReturnStack.Count > 0)
                    beforeReturnStack.Peek().Add(from);

                return;
            }

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
                Builder.Connect(from, to);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void connectToLabel(string labelName, EmitStore store)
            => gotosToConnect.Add(labelName, store);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlockValue(Block block, int valueIndex, object value)
            => Builder.SetBlockValue(block, valueIndex, value);

        private void enterStatementBlock()
            => placer.EnterStatementBlock();
        public IDisposable StatementBlock()
            => placer.StatementBlock();
        private void exitStatementBlock()
            => placer.ExitStatementBlock();

        private void enterExpressionBlock()
            => placer.EnterExpressionBlock();
        public IDisposable ExpressionBlock()
            => placer.ExpressionBlock();
        private void exitExpressionBlock()
            => placer.ExitExpressionBlock();
        #endregion
    }
}
