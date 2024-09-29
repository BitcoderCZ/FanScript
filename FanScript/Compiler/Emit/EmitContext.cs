using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols.Variables;
using FanScript.FCInfo;

namespace FanScript.Compiler.Emit
{
    internal sealed class EmitContext
    {
        private Func<BoundStatement, EmitStore> emitStatement;
        private Func<BoundExpression, EmitStore> emitExpression;

        private Func<IDisposable> statementBlock;
        private Func<IDisposable> expressionBlock;

        private Func<BlockDef, Block> addBlock;
        private Action<EmitStore, EmitStore> connect;
        private Action<Block, int, object> setBlockValue;

        private Func<object?, EmitStore> emitLiteralExpression;

        private Func<VariableSymbol, EmitStore> emitGetVariable;
        private Func<BoundExpression, Func<EmitStore>, EmitStore> emitSetExpression;
        private Func<VariableSymbol, Func<EmitStore>, EmitStore> emitSetVariable;
        private Func<BoundExpression, (EmitStore, EmitStore, EmitStore)> breakVector;
        private Func<BoundExpression, bool[], EmitStore?[]> breakVectorAny;
        private Func<ReadOnlyMemory<BoundExpression>, bool, object?[]?> validateConstants;
        private Action<string> writeComment;

        public readonly BlockBuilder Builder;
        public readonly DiagnosticBag Diagnostics;

        public EmitContext(Emitter emitter)
        {
            Builder = emitter.builder;
            Diagnostics = emitter.diagnostics;

            emitStatement = emitter.emitStatement;
            emitExpression = emitter.emitExpression;

            statementBlock = emitter.statementBlock;
            expressionBlock = emitter.expressionBlock;

            addBlock = emitter.addBlock;
            connect = emitter.connect;
            setBlockValue = emitter.setBlockValue;

            emitLiteralExpression = emitter.emitLiteralExpression;

            emitGetVariable = emitter.emitGetVariable;
            emitSetExpression = emitter.emitSetExpression;
            emitSetVariable = emitter.emitSetVariable;
            breakVector = emitter.breakVector;
            breakVectorAny = emitter.breakVectorAny;
            validateConstants = emitter.validateConstants;
            writeComment = emitter.writeComment;
        }

        public EmitStore EmitStatement(BoundStatement statement)
            => emitStatement(statement);
        public EmitStore EmitExpression(BoundExpression expression)
            => emitExpression(expression);

        public IDisposable StatementBlock()
            => statementBlock();
        public IDisposable ExpressionBlock()
            => expressionBlock();

        public Block AddBlock(BlockDef def)
            => addBlock(def);
        public void Connect(EmitStore from, EmitStore to)
            => connect(from, to);
        public void SetBlockValue(Block block, int valueIndex, object value)
            => setBlockValue(block, valueIndex, value);

        public EmitStore EmitLiteralExpression(object? value)
            => emitLiteralExpression(value);

        public EmitStore EmitGetVariable(VariableSymbol variable)
            => emitGetVariable(variable);
        /// <summary>
        /// Emits setting <paramref name="expression"/> to <paramref name="getValueStore"/>
        /// </summary>
        /// <remarks>
        /// DOES NOT automatically call <see cref="CodePlacer.ExpressionBlock()"/>
        /// </remarks>
        /// <param name="expression"></param>
        /// <param name="getValueStore"></param>
        /// <returns></returns>
        public EmitStore EmitSetExpression(BoundExpression expression, Func<EmitStore> getValueStore)
            => emitSetExpression(expression, getValueStore);
        /// <summary>
        /// Emits setting <paramref name="variable"/> to <paramref name="getValueStore"/>
        /// </summary>
        /// <remarks>
        /// DOES NOT automatically call <see cref="CodePlacer.ExpressionBlock()"/>
        /// </remarks>
        /// <param name="expression"></param>
        /// <param name="getValueStore"></param>
        /// <returns></returns>
        public EmitStore EmitSetVariable(VariableSymbol variable, Func<EmitStore> getValueStore)
            => emitSetVariable(variable, getValueStore);

        public (EmitStore X, EmitStore Y, EmitStore Z) BreakVector(BoundExpression expression)
            => breakVector(expression);
        public EmitStore?[] BreakVectorAny(BoundExpression expression, bool[] useComponent)
            => breakVectorAny(expression, useComponent);

        public object?[]? ValidateConstants(ReadOnlyMemory<BoundExpression> expressions, bool mustBeConstant)
            => validateConstants(expressions, mustBeConstant);

        public void WriteComment(string text)
            => writeComment(text);
    }
}
