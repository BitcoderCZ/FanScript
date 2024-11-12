using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit.BlockBuilders;
using FanScript.Compiler.Emit.CodePlacers;
using FanScript.Compiler.Emit.Utils;
using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Functions;
using FanScript.Compiler.Symbols.Variables;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;

namespace FanScript.Compiler.Emit;

internal sealed class Emitter : IEmitContext
{
    // key - a label before antoher label, item - the label after key
    private readonly Dictionary<string, string> _sameTargetLabels = [];

    // key - label name, item - list of goto "origins", not only gotos but also statements just before the label
    private readonly ListMultiValueDictionary<string, IEmitStore> _gotosToConnect = [];

    // key - label name, item - the store to connect gotos to
    private readonly Dictionary<string, IEmitStore> _afterLabel = [];

    private readonly InlineVarManager _inlineVarManager = new();

    private readonly Dictionary<VariableSymbol, BreakBlockCache> _vectorBreakCache = [];
    private readonly Dictionary<VariableSymbol, BreakBlockCache> _rotationBreakCache = [];

    private readonly Stack<List<IEmitStore>> _beforeReturnStack = new();
    private readonly Dictionary<FunctionSymbol, IEmitStore> _functions = [];
    private readonly ListMultiValueDictionary<FunctionSymbol, IEmitStore> _calls = [];

    private BoundProgram _program = null!;
    private CodePlacer _placer = null!;

    private FunctionSymbol _currentFunciton = null!;

    private Emitter()
    {
    }

    public DiagnosticBag Diagnostics { get; private set; } = new DiagnosticBag();

    public BlockBuilder Builder { get; private set; } = null!;

    public static ImmutableArray<Diagnostic> Emit(BoundProgram program, CodePlacer placer, BlockBuilder builder)
    {
        if (program.Diagnostics.HasErrors())
        {
            return program.Diagnostics;
        }
        else
        {
            Emitter emitter = new Emitter();
            return emitter.EmitInternal(program, placer, builder);
        }
    }

    private ImmutableArray<Diagnostic> EmitInternal(BoundProgram program, CodePlacer placer, BlockBuilder builder)
    {
        _program = program;
        _placer = placer;
        Builder = builder;

        _vectorBreakCache.Clear();
        _rotationBreakCache.Clear();

        foreach (var (func, body) in _program.Functions.ToImmutableSortedDictionary())
        {
            if (func != program.ScriptFunction && program.Analysis.ShouldFunctionGetInlined(func))
            {
                continue;
            }

            _currentFunciton = func;

            using (StatementBlock())
            {
                WriteComment(func.Name);

                _beforeReturnStack.Push([]);
                _functions.Add(func, EmitStatement(body));
                _beforeReturnStack.Pop();

                ProcessLabelsAndGotos();
            }

            _currentFunciton = null!;
        }

        ProcessCalls();

        return [.. Diagnostics, .. _program.Diagnostics];
    }

    private void ProcessLabelsAndGotos()
    {
        foreach (var (labelName, stores) in _gotosToConnect)
        {
            if (!TryGetAfterLabel(labelName, out IEmitStore? afterLabel))
            {
                continue;
            }

            foreach (IEmitStore store in stores)
            {
                Builder.Connect(store, afterLabel);
            }
        }

        _sameTargetLabels.Clear();
        _gotosToConnect.Clear();
        _afterLabel.Clear();

        bool TryGetAfterLabel(string name, [NotNullWhen(true)] out IEmitStore? emitStore)
        {
            if (_afterLabel.TryGetValue(name, out emitStore))
            {
                return emitStore is not GotoEmitStore gotoEmit || TryGetAfterLabel(gotoEmit.LabelName, out emitStore);
            }
            else if (_sameTargetLabels.TryGetValue(name, out string? target))
            {
                return TryGetAfterLabel(target, out emitStore);
            }
            else
            {
                emitStore = null;
                return false;
            }
        }
    }

    private void ProcessCalls()
    {
        foreach (var (func, callList) in _calls)
        {
            if (!_functions.TryGetValue(func, out var funcStore))
            {
                throw new Exception($"Failed to get entry point for function '{func}'.");
            }

            for (int i = 0; i < callList.Count; i++)
            {
                Connect(callList[i], funcStore);
            }
        }

        _functions.Clear();
        _calls.Clear();
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    public IEmitStore EmitStatement(BoundStatement statement)
    {
        IEmitStore store = statement switch
        {
            BoundBlockStatement blockStatement => EmitBlockStatement(blockStatement),
            BoundVariableDeclarationStatement variableDeclarationStatement when variableDeclarationStatement.OptionalAssignment is not null => EmitStatement(variableDeclarationStatement.OptionalAssignment),
            BoundVariableDeclarationStatement => NopEmitStore.Instance,
            BoundAssignmentStatement assignmentStatement => EmitAssigmentStatement(assignmentStatement),
            BoundPostfixStatement postfixStatement => EmitPostfixStatement(postfixStatement),
            BoundPrefixStatement prefixStatement => EmitPrefixStatement(prefixStatement),
            BoundGotoStatement gotoStatement => EmitGotoStatement(gotoStatement),
            BoundEventGotoStatement eventGotoStatement => EmitEventGotoStatement(eventGotoStatement),
            BoundConditionalGotoStatement conditionalGotoStatement => EmitConditionalGotoStatement(conditionalGotoStatement),
            BoundLabelStatement labelStatement => EmitLabelStatement(labelStatement),
            BoundReturnStatement returnStatement => EmitReturnStatement(returnStatement),
            BoundEmitterHintStatement emitterHintStatement => EmitHint(emitterHintStatement),
            BoundCallStatement callStatement => EmitCallStatement(callStatement),
            BoundExpressionStatement expressionStatement => EmitExpressionStatement(expressionStatement),
            BoundNopStatement => NopEmitStore.Instance,

            _ => throw new UnexpectedBoundNodeException(statement),
        };

        return store;
    }

    private IEmitStore EmitBlockStatement(BoundBlockStatement statement)
    {
        if (statement.Statements.Length == 0)
        {
            return NopEmitStore.Instance;
        }
        else if (statement.Statements.Length == 1 && statement.Statements[0] is BoundBlockStatement inBlock)
        {
            return EmitBlockStatement(inBlock);
        }

        EmitConnector connector = new EmitConnector(Connect);

        bool newCodeBlock = _placer.CurrentCodeBlockBlocks > 0;
        if (newCodeBlock)
        {
            EnterStatementBlock();
        }

        for (int i = 0; i < statement.Statements.Length; i++)
        {
            IEmitStore statementStore = EmitStatement(statement.Statements[i]);

            connector.Add(statementStore);
        }

        if (newCodeBlock)
        {
            ExitStatementBlock();
        }

        return connector.Store;
    }

    private IEmitStore EmitAssigmentStatement(BoundAssignmentStatement statement)
        => EmitSetVariable(statement.Variable, statement.Expression);

    private BasicEmitStore EmitPostfixStatement(BoundPostfixStatement statement)
    {
        BlockDef def = statement.PostfixKind switch
        {
            PostfixKind.Increment => Blocks.Variables.PlusPlusFloat,
            PostfixKind.Decrement => Blocks.Variables.MinusMinusFloat,
            _ => throw new UnknownEnumValueException<PostfixKind>(statement.PostfixKind),
        };
        Block block = AddBlock(def);

        using (ExpressionBlock())
        {
            IEmitStore store = EmitGetVariable(statement.Variable);

            Connect(store, BasicEmitStore.CIn(block, block.Type.Terminals["Variable"]));
        }

        return new BasicEmitStore(block);
    }

    private BasicEmitStore EmitPrefixStatement(BoundPrefixStatement statement)
    {
        BlockDef def = statement.PrefixKind switch
        {
            PrefixKind.Increment => Blocks.Variables.PlusPlusFloat,
            PrefixKind.Decrement => Blocks.Variables.MinusMinusFloat,
            _ => throw new UnknownEnumValueException<PrefixKind>(statement.PrefixKind),
        };
        Block block = AddBlock(def);

        using (ExpressionBlock())
        {
            IEmitStore store = EmitGetVariable(statement.Variable);

            Connect(store, BasicEmitStore.CIn(block, block.Type.Terminals["Variable"]));
        }

        return new BasicEmitStore(block);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "whomp whomp")]
    private static IEmitStore EmitGotoStatement(BoundGotoStatement statement)
        => statement.IsRollback ? RollbackEmitStore.Instance : new GotoEmitStore(statement.Label.Name);

    private BasicEmitStore EmitEventGotoStatement(BoundEventGotoStatement statement)
    {
        BlockDef def = statement.EventType switch
        {
            EventType.Play => Blocks.Control.PlaySensor,
            EventType.LateUpdate => Blocks.Control.LateUpdate,
            EventType.BoxArt => Blocks.Control.BoxArtSensor,
            EventType.Touch => Blocks.Control.TouchSensor,
            EventType.Swipe => Blocks.Control.SwipeSensor,
            EventType.Button => Blocks.Control.Button,
            EventType.Collision => Blocks.Control.Collision,
            EventType.Loop => Blocks.Control.Loop,
            _ => throw new UnknownEnumValueException<EventType>(statement.EventType),
        };
        Block block = AddBlock(def);

        ImmutableArray<BoundExpression>? arguments = statement.ArgumentClause?.Arguments;

        EnterStatementBlock();

        switch (statement.EventType)
        {
            case EventType.Touch:
                {
                    object?[]? values = ValidateConstants(arguments!.Value.AsMemory(2..), true);
                    if (values is null)
                    {
                        break;
                    }

                    for (int i = 0; i < values.Length; i++)
                    {
                        SetBlockValue(block, i, (byte)((float?)values[i] ?? 0f)); // unbox, then cast
                    }

                    ConnectToLabel(statement.Label.Name, PlaceAndConnectRefArgs(arguments!.Value.AsSpan(..2)));
                    return new BasicEmitStore(block);
                }

            case EventType.Swipe:
                {
                    ConnectToLabel(statement.Label.Name, PlaceAndConnectRefArgs(arguments!.Value.AsSpan()));
                    return new BasicEmitStore(block);
                }

            case EventType.Button:
                {
                    object?[]? values = ValidateConstants(arguments!.Value.AsMemory(), true);
                    if (values is null)
                    {
                        break;
                    }

                    for (int i = 0; i < values.Length; i++)
                    {
                        SetBlockValue(block, i, (byte)((float?)values[i] ?? 0f)); // unbox, then cast
                    }
                }

                break;
            case EventType.Collision:
                {
                    ConnectToLabel(statement.Label.Name, PlaceAndConnectRefArgs(arguments!.Value.AsSpan(1..), arguments!.Value.AsMemory(..1)));
                    return new BasicEmitStore(block);
                }

            case EventType.Loop:
                {
                    ConnectToLabel(statement.Label.Name, PlaceAndConnectRefArgs(arguments!.Value.AsSpan(2..), arguments!.Value.AsMemory(..2)));
                    return new BasicEmitStore(block);
                }
        }

        ConnectToLabel(statement.Label.Name, BasicEmitStore.COut(block, block.Type.TerminalArray[^2]));

        return new BasicEmitStore(block);

        IEmitStore PlaceAndConnectRefArgs(ReadOnlySpan<BoundExpression> outArguments, ReadOnlyMemory<BoundExpression>? arguments = null)
        {
            arguments ??= ReadOnlyMemory<BoundExpression>.Empty;

            EmitConnector connector = new EmitConnector(Connect);
            connector.Add(BasicEmitStore.COut(block, block.Type.TerminalArray[Index.FromEnd(2 + arguments.Value.Length)]));

            if (arguments.Value.Length != 0)
            {
                ExitStatementBlock();

                using (ExpressionBlock())
                {
                    var argumentsSpan = arguments.Value.Span;

                    for (int i = 0; i < argumentsSpan.Length; i++)
                    {
                        IEmitStore store = EmitExpression(argumentsSpan[i]);

                        Connect(store, BasicEmitStore.CIn(block, block.Type.TerminalArray[Index.FromEnd(i + 2)]));
                    }
                }

                EnterStatementBlock();
            }

            for (int i = 0; i < outArguments.Length; i++)
            {
                VariableSymbol variable = ((BoundVariableExpression)outArguments[i]).Variable;

                connector.Add(EmitSetVariable(variable, () => BasicEmitStore.COut(block, block.Type.TerminalArray[Index.FromEnd(i + arguments.Value.Length + 3)])));
            }

            return connector.Store;
        }
    }

    private ConditionalGotoEmitStore EmitConditionalGotoStatement(BoundConditionalGotoStatement statement)
    {
        Block block = AddBlock(Blocks.Control.If);

        using (ExpressionBlock())
        {
            IEmitStore condition = EmitExpression(statement.Condition);

            Connect(condition, BasicEmitStore.CIn(block, block.Type.Terminals["Condition"]));
        }

        ConditionalGotoEmitStore store = new ConditionalGotoEmitStore(
            block,
            block.Type.Before,
            block,
            block.Type.Terminals[statement.JumpIfTrue ? "True" : "False"],
            block,
            block.Type.Terminals[statement.JumpIfTrue ? "False" : "True"]);

        ConnectToLabel(statement.Label.Name, BasicEmitStore.COut(store.OnCondition, store.OnConditionTerminal));

        return store;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "whomp whomp")]
    private static LabelEmitStore EmitLabelStatement(BoundLabelStatement statement)
        => new LabelEmitStore(statement.Label.Name);

    private IEmitStore EmitReturnStatement(BoundReturnStatement statement)
        => statement.Expression is null
            ? new ReturnEmitStore()
            : EmitSetVariable(ReservedCompilerVariableSymbol.CreateFunctionRes(_currentFunciton), statement.Expression);

    private NopEmitStore EmitHint(BoundEmitterHintStatement statement)
    {
        switch (statement.Hint)
        {
            case BoundEmitterHintStatement.HintKind.StatementBlockStart:
                EnterStatementBlock();
                break;
            case BoundEmitterHintStatement.HintKind.StatementBlockEnd:
                ExitStatementBlock();
                break;
            case BoundEmitterHintStatement.HintKind.HighlightStart:
                _placer.EnterHighlight();
                break;
            case BoundEmitterHintStatement.HintKind.HighlightEnd:
                _placer.ExitHightlight();
                break;
            default:
                throw new UnknownEnumValueException<BoundEmitterHintStatement.HintKind>(statement.Hint);
        }

        return NopEmitStore.Instance;
    }

    private IEmitStore EmitCallStatement(BoundCallStatement statement)
    {
        if (statement.Function is BuiltinFunctionSymbol builtinFunction)
        {
            return builtinFunction.Emit(new BoundCallExpression(statement.Syntax, statement.Function, statement.ArgumentClause, statement.ReturnType, statement.GenericType), this);
        }

        Debug.Assert(!statement.Function.Modifiers.HasFlag(Modifiers.Inline), "Calls to inline funcitons should get inlined.");

        FunctionSymbol func = statement.Function;

        EmitConnector connector = new EmitConnector(Connect);

        for (int i = 0; i < func.Parameters.Length; i++)
        {
            Modifiers mods = func.Parameters[i].Modifiers;

            if (mods.HasFlag(Modifiers.Out))
            {
                continue;
            }

            IEmitStore setStore = EmitSetVariable(func.Parameters[i], statement.ArgumentClause.Arguments[i]);

            connector.Add(setStore);
        }

        Block callBlock = AddBlock(Blocks.Control.If);

        using (ExpressionBlock())
        {
            Block trueBlock = AddBlock(Blocks.Values.True);
            Connect(BasicEmitStore.COut(trueBlock, trueBlock.Type.Terminals["True"]), BasicEmitStore.CIn(callBlock, callBlock.Type.Terminals["Condition"]));
        }

        _calls.Add(func, BasicEmitStore.COut(callBlock, callBlock.Type.Terminals["True"]));

        connector.Add(new BasicEmitStore(callBlock));

        for (int i = 0; i < func.Parameters.Length; i++)
        {
            Modifiers mods = func.Parameters[i].Modifiers;

            if (!mods.HasFlag(Modifiers.Out) && !mods.HasFlag(Modifiers.Ref))
            {
                continue;
            }

            IEmitStore setStore = EmitSetExpression(statement.ArgumentClause.Arguments[i], () =>
            {
                using (ExpressionBlock())
                {
                    return EmitGetVariable(func.Parameters[i]);
                }
            });

            connector.Add(setStore);
        }

        if (func.Type != TypeSymbol.Void && statement.ResultVariable is not null)
        {
            IEmitStore setStore = EmitSetVariable(statement.ResultVariable, () =>
            {
                using (ExpressionBlock())
                {
                    return EmitGetVariable(ReservedCompilerVariableSymbol.CreateFunctionRes(func));
                }
            });

            connector.Add(setStore);
        }

        return connector.Store;
    }

    private IEmitStore EmitExpressionStatement(BoundExpressionStatement statement)
        => statement.Expression is BoundNopExpression ? NopEmitStore.Instance : EmitExpression(statement.Expression);

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    public IEmitStore EmitExpression(BoundExpression expression)
    {
        IEmitStore store = expression switch
        {
            BoundLiteralExpression literalExpression => EmitLiteralExpression(literalExpression),
            BoundConstructorExpression constructorExpression => EmitConstructorExpression(constructorExpression),
            BoundUnaryExpression unaryExpression => EmitUnaryExpression(unaryExpression),
            BoundBinaryExpression binaryExpression => EmitBinaryExpression(binaryExpression),
            BoundVariableExpression variableExpression => EmitVariableExpression(variableExpression),
            BoundCallExpression callExpression => EmitCallExpression(callExpression),

            _ => throw new UnexpectedBoundNodeException(expression),
        };

        return store;
    }

    private IEmitStore EmitLiteralExpression(BoundLiteralExpression expression)
        => EmitLiteralExpression(expression.Value);

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    public IEmitStore EmitLiteralExpression(object? value)
    {
        if (value is null)
        {
            return NopEmitStore.Instance;
        }

        Block block = AddBlock(Blocks.Values.ValueByType(value));

        if (value is not bool)
        {
            SetBlockValue(block, 0, value);
        }

        return BasicEmitStore.COut(block, block.Type.TerminalArray[0]);
    }

    private IEmitStore EmitConstructorExpression(BoundConstructorExpression expression)
    {
        if (expression.ConstantValue is not null)
        {
            return EmitLiteralExpression(expression.ConstantValue.Value);
        }

        BlockDef def = Blocks.Math.MakeByType(expression.Type.ToWireType());
        Block block = AddBlock(def);

        using (ExpressionBlock())
        {
            IEmitStore xStore = EmitExpression(expression.ExpressionX);
            IEmitStore yStore = EmitExpression(expression.ExpressionY);
            IEmitStore zStore = EmitExpression(expression.ExpressionZ);

            Connect(xStore, BasicEmitStore.CIn(block, def.Terminals["X"]));
            Connect(yStore, BasicEmitStore.CIn(block, def.Terminals["Y"]));
            Connect(zStore, BasicEmitStore.CIn(block, def.Terminals["Z"]));
        }

        return BasicEmitStore.COut(block, def.TerminalArray[0]);
    }

    private IEmitStore EmitUnaryExpression(BoundUnaryExpression expression)
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
                            IEmitStore opStore = EmitExpression(expression.Operand);

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
                            IEmitStore opStore = EmitExpression(expression.Operand);

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
                        IEmitStore opStore = EmitExpression(expression.Operand);

                        Connect(opStore, BasicEmitStore.CIn(block, block.Type.Terminals["Tru"]));
                    }

                    return BasicEmitStore.COut(block, block.Type.Terminals["Not Tru"]);
                }

            default:
                throw new UnknownEnumValueException<BoundUnaryOperatorKind>(expression.Op.Kind);
        }
    }

    private IEmitStore EmitBinaryExpression(BoundBinaryExpression expression)
        => expression.Type == TypeSymbol.Bool || expression.Type == TypeSymbol.Float
            ? EmitBinaryExpression_FloatOrBool(expression)
            : expression.Type == TypeSymbol.Vector3 || expression.Type == TypeSymbol.Rotation
            ? EmitBinaryExpression_VecOrRot(expression)
            : throw new UnexpectedSymbolException(expression.Type);

    private BasicEmitStore EmitBinaryExpression_FloatOrBool(BoundBinaryExpression expression)
    {
        BlockDef op = expression.Op.Kind switch
        {
            BoundBinaryOperatorKind.Addition => Blocks.Math.Add_Number,
            BoundBinaryOperatorKind.Subtraction => Blocks.Math.Subtract_Number,
            BoundBinaryOperatorKind.Multiplication => Blocks.Math.Multiply_Number,
            BoundBinaryOperatorKind.Division => Blocks.Math.Divide_Number,
            BoundBinaryOperatorKind.Modulo => Blocks.Math.Modulo_Number,
            BoundBinaryOperatorKind.Equals or BoundBinaryOperatorKind.NotEquals => Blocks.Math.EqualsByType(expression.Left.Type!.ToWireType()),
            BoundBinaryOperatorKind.LogicalAnd => Blocks.Math.LogicalAnd,
            BoundBinaryOperatorKind.LogicalOr => Blocks.Math.LogicalOr,
            BoundBinaryOperatorKind.Less or BoundBinaryOperatorKind.GreaterOrEquals => Blocks.Math.Less,
            BoundBinaryOperatorKind.Greater or BoundBinaryOperatorKind.LessOrEquals => Blocks.Math.Greater,
            _ => throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind),
        };

        if (expression.Op.Kind == BoundBinaryOperatorKind.NotEquals
            || expression.Op.Kind == BoundBinaryOperatorKind.LessOrEquals
            || expression.Op.Kind == BoundBinaryOperatorKind.GreaterOrEquals)
        {
            // invert output, >= or <=, >= can be accomplished as inverted <
            Block not = AddBlock(Blocks.Math.Not);
            using (ExpressionBlock())
            {
                Block block = AddBlock(op);

                Connect(BasicEmitStore.COut(block, block.Type.TerminalArray[0]), BasicEmitStore.CIn(not, not.Type.Terminals["Tru"]));

                using (ExpressionBlock())
                {
                    IEmitStore store0 = EmitExpression(expression.Left);
                    IEmitStore store1 = EmitExpression(expression.Right);

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
                IEmitStore store0 = EmitExpression(expression.Left);
                IEmitStore store1 = EmitExpression(expression.Right);

                Connect(store0, BasicEmitStore.CIn(block, block.Type.TerminalArray[2]));
                Connect(store1, BasicEmitStore.CIn(block, block.Type.TerminalArray[1]));
            }

            return BasicEmitStore.COut(block, block.Type.TerminalArray[0]);
        }
    }

    private IEmitStore EmitBinaryExpression_VecOrRot(BoundBinaryExpression expression)
    {
        BlockDef? defOp = null;
        switch (expression.Op.Kind)
        {
            case BoundBinaryOperatorKind.Addition:
                if (expression.Left.Type == TypeSymbol.Vector3)
                {
                    defOp = Blocks.Math.Add_Vector;
                }

                break;
            case BoundBinaryOperatorKind.Subtraction:
                if (expression.Left.Type == TypeSymbol.Vector3)
                {
                    defOp = Blocks.Math.Subtract_Vector;
                }

                break;
            case BoundBinaryOperatorKind.Multiplication:
                defOp = expression.Left.Type == TypeSymbol.Vector3 && expression.Right.Type == TypeSymbol.Float
                    ? Blocks.Math.Multiply_Vector
                    : expression.Left.Type == TypeSymbol.Rotation && expression.Right.Type == TypeSymbol.Rotation
                    ? Blocks.Math.Multiply_Rotation
                    : expression.Left.Type == TypeSymbol.Vector3 && expression.Right.Type == TypeSymbol.Rotation
                    ? Blocks.Math.Rotate_Vector
                    : throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);
                break;
            case BoundBinaryOperatorKind.Division:
            case BoundBinaryOperatorKind.Modulo:
                break; // supported, but not one block
            case BoundBinaryOperatorKind.Equals:
                defOp = expression.Left.Type == TypeSymbol.Vector3
                    ? Blocks.Math.Equals_Vector
                    : throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);

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
                    return BuildOperatorWithBreak(Blocks.Math.Make_Rotation, Blocks.Math.Add_Number);
                case BoundBinaryOperatorKind.Subtraction: // Rotation
                    return BuildOperatorWithBreak(Blocks.Math.Make_Rotation, Blocks.Math.Subtract_Number);
                case BoundBinaryOperatorKind.Division: // Vector3
                    return BuildOperatorWithBreak(Blocks.Math.Make_Vector, Blocks.Math.Divide_Number);
                case BoundBinaryOperatorKind.Modulo: // Vector3
                    return BuildOperatorWithBreak(Blocks.Math.Make_Vector, Blocks.Math.Modulo_Number);
                case BoundBinaryOperatorKind.NotEquals:
                    {
                        defOp = expression.Left.Type == TypeSymbol.Vector3
                            ? Blocks.Math.Equals_Vector
                            : throw new UnknownEnumValueException<BoundBinaryOperatorKind>(expression.Op.Kind);

                        Block not = AddBlock(Blocks.Math.Not);
                        using (ExpressionBlock())
                        {
                            Block op = AddBlock(defOp);

                            Connect(BasicEmitStore.COut(op, op.Type.TerminalArray[0]), BasicEmitStore.CIn(not, not.Type.Terminals["Tru"]));

                            using (ExpressionBlock())
                            {
                                IEmitStore store0 = EmitExpression(expression.Left);
                                IEmitStore store1 = EmitExpression(expression.Right);

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
                IEmitStore store0 = EmitExpression(expression.Left);
                IEmitStore store1 = EmitExpression(expression.Right);

                Connect(store0, BasicEmitStore.CIn(op, op.Type.TerminalArray[2]));
                Connect(store1, BasicEmitStore.CIn(op, op.Type.TerminalArray[1]));
            }

            return BasicEmitStore.COut(op, op.Type.TerminalArray[0]);
        }

        IEmitStore BuildOperatorWithBreak(BlockDef defMake, BlockDef defOp)
        {
            Block make = AddBlock(defMake);

            using (ExpressionBlock())
            {
                Block op1 = AddBlock(defOp);

                IEmitStore leftX;
                IEmitStore leftY;
                IEmitStore leftZ;
                IEmitStore rightX;
                IEmitStore rightY;
                IEmitStore rightZ;
                using (ExpressionBlock())
                {
                    (leftX, leftY, leftZ) = BreakVector(expression.Left);

                    if (expression.Right.Type == TypeSymbol.Vector3 || expression.Right.Type == TypeSymbol.Rotation)
                    {
                        (rightX, rightY, rightZ) = BreakVector(expression.Right);
                    }
                    else
                    {
                        rightZ = rightY = rightX = EmitExpression(expression.Right);
                    }
                }

                Block op2 = AddBlock(defOp);
                Block op3 = AddBlock(defOp);

                // left to op
                Connect(leftX, BasicEmitStore.CIn(op1, op1.Type.TerminalArray[2]));
                Connect(leftY, BasicEmitStore.CIn(op2, op2.Type.TerminalArray[2]));
                Connect(leftZ, BasicEmitStore.CIn(op3, op3.Type.TerminalArray[2]));

                // right to op
                Connect(rightX, BasicEmitStore.CIn(op1, op1.Type.TerminalArray[1]));
                Connect(rightY, BasicEmitStore.CIn(op2, op2.Type.TerminalArray[1]));
                Connect(rightZ, BasicEmitStore.CIn(op3, op3.Type.TerminalArray[1]));

                // op to make
                Connect(BasicEmitStore.COut(op1, op1.Type.TerminalArray[0]), BasicEmitStore.CIn(make, make.Type.TerminalArray[3]));
                Connect(BasicEmitStore.COut(op2, op2.Type.TerminalArray[0]), BasicEmitStore.CIn(make, make.Type.TerminalArray[2]));
                Connect(BasicEmitStore.COut(op3, op3.Type.TerminalArray[0]), BasicEmitStore.CIn(make, make.Type.TerminalArray[1]));
            }

            return BasicEmitStore.COut(make, make.Type.TerminalArray[0]);
        }
    }

    private IEmitStore EmitVariableExpression(BoundVariableExpression expression)
        => EmitGetVariable(expression.Variable);

    private IEmitStore EmitCallExpression(BoundCallExpression expression)
    {
        if (expression.Function is BuiltinFunctionSymbol builtinFunction)
        {
            return builtinFunction.Emit(expression, this);
        }

        Debug.Fail("User defined function calls should be extracted to statement calls.");
        return NopEmitStore.Instance;
    }

    #region Utils
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    public IEmitStore EmitGetVariable(VariableSymbol variable)
    {
        switch (variable)
        {
            case PropertySymbol property:
                return property.Definition.EmitGet.Invoke(this, property.Expression);
            case NullVariableSymbol:
                return NopEmitStore.Instance;
            default:
                {
                    if (variable.Modifiers.HasFlag(Modifiers.Inline))
                    {
                        if (_inlineVarManager.TryGet(variable, this, out var store))
                        {
                            return store;
                        }
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

    private IEmitStore EmitSetExpression(BoundExpression expression, BoundExpression valueExpression)
        => EmitSetExpression(expression, () =>
        {
            using (ExpressionBlock())
            {
                IEmitStore store = EmitExpression(valueExpression);

                return store;
            }
        });

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    public IEmitStore EmitSetExpression(BoundExpression expression, Func<IEmitStore> getValueStore)
    {
        switch (expression)
        {
            case BoundVariableExpression var:
                return EmitSetVariable(var.Variable, getValueStore);
            default:
                {
                    Block set = AddBlock(Blocks.Variables.Set_PtrByType(expression.Type.ToWireType()));

                    IEmitStore exStore = EmitExpression(expression);
                    IEmitStore valStore = getValueStore();

                    Connect(exStore, BasicEmitStore.CIn(set, set.Type.Terminals["Variable"]));
                    Connect(valStore, BasicEmitStore.CIn(set, set.Type.Terminals["Value"]));

                    return new BasicEmitStore(set);
                }
        }
    }

    private IEmitStore EmitSetVariable(VariableSymbol variable, BoundExpression expression)
        => EmitSetVariable(variable, () =>
        {
            using (ExpressionBlock())
            {
                IEmitStore store = EmitExpression(expression);
                return store;
            }
        });

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    public IEmitStore EmitSetVariable(VariableSymbol variable, Func<IEmitStore> getValueStore)
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
                    {
                        return NopEmitStore.Instance;
                    }
                    else if (variable.Modifiers.HasFlag(Modifiers.Inline))
                    {
                        _inlineVarManager.Set(variable, getValueStore());
                        return NopEmitStore.Instance;
                    }

                    Block block = AddBlock(Blocks.Variables.Set_VariableByType(variable.Type.ToWireType()));

                    SetBlockValue(block, 0, variable.ResultName);

                    IEmitStore valueStore = getValueStore();

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
    public (IEmitStore X, IEmitStore Y, IEmitStore Z) BreakVector(BoundExpression expression)
    {
        var result = BreakVectorAny(expression, [true, true, true]);
        return (result[0]!, result[1]!, result[2]!);
    }

    public IEmitStore?[] BreakVectorAny(BoundExpression expression, bool[] useComponent)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(useComponent);
        if (useComponent.Length != 3)
        {
            throw new ArgumentException(nameof(useComponent), $"{nameof(useComponent)}.Length must be 3, not '{useComponent.Length}'");
        }

        Vector3F? vector = null;
        if (expression is BoundLiteralExpression literal)
        {
            vector = literal.Value is Vector3F vec
                ? vec
                : literal.Value is Rotation rot
                ? (Vector3F?)rot.Value
                : throw new InvalidDataException($"Invalid value type '{literal.Value?.GetType()}'");
        }
        else if (expression is BoundConstructorExpression contructor && contructor.ConstantValue is not null)
        {
            vector = contructor.ConstantValue.Value is Vector3F ?
                (Vector3F)contructor.ConstantValue.Value :
                ((Rotation)contructor.ConstantValue.Value!).Value;
        }
        else if (expression is BoundVariableExpression variable && variable.Variable.Modifiers.HasFlag(Modifiers.Constant) && variable.ConstantValue is not null)
        {
            vector = variable.ConstantValue.Value is Vector3F ?
                (Vector3F)variable.ConstantValue.Value :
                ((Rotation)variable.ConstantValue.Value!).Value;
        }

        if (vector is not null)
        {
            return [
                useComponent[0] ? EmitLiteralExpression(vector.Value.X) : null,
                useComponent[1] ? EmitLiteralExpression(vector.Value.Y) : null,
                useComponent[2] ? EmitLiteralExpression(vector.Value.Z) : null,
            ];
        }
        else if (expression is BoundConstructorExpression contructor)
        {
            return [
                useComponent[0] ? EmitExpression(contructor.ExpressionX) : null,
                useComponent[1] ? EmitExpression(contructor.ExpressionY) : null,
                useComponent[2] ? EmitExpression(contructor.ExpressionZ) : null,
            ];
        }
        else if (expression is BoundVariableExpression var)
        {
            BreakBlockCache cache = (var.Type == TypeSymbol.Vector3 ? _vectorBreakCache : _rotationBreakCache)
                .AddIfAbsent(var.Variable, new BreakBlockCache());
            if (!cache.TryGet(out Block? block))
            {
                block = AddBlock(var.Type == TypeSymbol.Vector3 ? Blocks.Math.Break_Vector : Blocks.Math.Break_Rotation);
                cache.SetNewBlock(block);

                using (ExpressionBlock())
                {
                    IEmitStore store = EmitVariableExpression(var);
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
                IEmitStore store = EmitExpression(expression);
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
                {
                    Diagnostics.ReportValueMustBeConstant(expressionsMem[i].Syntax.Location);
                }

                invalid = true;
            }
            else
            {
                values[i] = constant.Value;
            }
        }

        return invalid ? null : values;
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

    public IEmitStore EmitSetArraySegment(BoundArraySegmentExpression segment, BoundExpression arrayVariable, BoundExpression startIndex)
    {
        Debug.Assert(arrayVariable.Type.InnerType == segment.ElementType, "Inner type of array variable");
        Debug.Assert(startIndex.Type == TypeSymbol.Float, $"The type of {nameof(startIndex)} must be float.");

        WireType type = arrayVariable.Type.ToWireType();

        EmitConnector connector = new EmitConnector(Connect);

        IEmitStore? lastElementStore = null;

        for (int i = 0; i < segment.Elements.Length; i++)
        {
            if (i == 0 && startIndex.ConstantValue is not null && (float)startIndex.ConstantValue.GetValueOrDefault(TypeSymbol.Float) == 0f)
            {
                connector.Add(EmitSetExpression(arrayVariable, segment.Elements[i]));
            }
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
        => _placer.PlaceBlock(def);

    public void Connect(IEmitStore from, IEmitStore to)
    {
        while (from is MultiEmitStore multi)
        {
            from = multi.OutStore;
        }

        while (to is MultiEmitStore multi)
        {
            to = multi.InStore;
        }

        if (from is RollbackEmitStore || to is RollbackEmitStore)
        {
            if (to is ReturnEmitStore && from is not RollbackEmitStore && _beforeReturnStack.Count > 0)
            {
                _beforeReturnStack.Peek().Add(from);
            }

            return;
        }

        if (from is LabelEmitStore sameFrom && to is LabelEmitStore sameTo)
        {
            _sameTargetLabels.Add(sameFrom.Name, sameTo.Name);
        }
        else if (from is GotoEmitStore)
        {
            // ignore, the going to is handeled if to is GotoEmitStore
        }
        else if (from is LabelEmitStore fromLabel)
        {
            _afterLabel.Add(fromLabel.Name, to);
        }
        else if (to is LabelEmitStore toLabel)
        {
            ConnectToLabel(toLabel.Name, from); // normal block before label, connect to block after the label
        }
        else if (to is GotoEmitStore toGoto)
        {
            ConnectToLabel(toGoto.LabelName, from);
        }
        else
        {
            Builder.Connect(from, to);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ConnectToLabel(string labelName, IEmitStore store)
        => _gotosToConnect.Add(labelName, store);

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlockValue(Block block, int valueIndex, object value)
        => Builder.SetBlockValue(block, valueIndex, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnterStatementBlock()
        => _placer.EnterStatementBlock();

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable StatementBlock()
        => _placer.StatementBlock();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExitStatementBlock()
        => _placer.ExitStatementBlock();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnterExpressionBlock()
        => _placer.EnterExpressionBlock();

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "whomp whomp")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IDisposable ExpressionBlock()
        => _placer.ExpressionBlock();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExitExpressionBlock()
        => _placer.ExitExpressionBlock();
    #endregion
}
