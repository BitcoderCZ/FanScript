﻿using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols;

namespace FanScript.Compiler.Emit
{
    internal sealed class EmitContext
    {
        private Func<BoundStatement, EmitStore> emitStatement;
        private Func<BoundExpression, EmitStore> emitExpression;
        private Action<EmitStore, EmitStore> connect;

        private Func<object?, EmitStore> emitLiteralExpression;

        private Func<VariableSymbol, EmitStore> emitGetVariable;
        private Func<BoundExpression, Func<EmitStore>, EmitStore> emitSetExpression;
        private Func<VariableSymbol, Func<EmitStore>, EmitStore> emitSetVariable;
        private Func<BoundExpression, (EmitStore, EmitStore, EmitStore)> breakVector;
        private Func<BoundExpression, bool[], EmitStore?[]> breakVectorAny;
        private Func<ReadOnlyMemory<BoundExpression>, bool, object?[]?> validateConstants;
        private Action<string> writeComment;

        public readonly CodeBuilder Builder;
        public readonly DiagnosticBag Diagnostics;

        public EmitContext(Emitter emitter)
        {
            Builder = emitter.builder;
            Diagnostics = emitter.diagnostics;

            emitStatement = emitter.emitStatement;
            emitExpression = emitter.emitExpression;
            connect = emitter.connect;

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

        public void Connect(EmitStore from, EmitStore to)
            => connect(from, to);

        public EmitStore EmitLiteralExpression(object? value)
            => emitLiteralExpression(value);

        public EmitStore EmitGetVariable(VariableSymbol variable)
            => emitGetVariable(variable);
        /// <summary>
        /// Emits setting <paramref name="expression"/> to <paramref name="getValueStore"/>
        /// </summary>
        /// <remarks>
        /// DOES NOT automatically <see cref="IBlockPlacer.ExpressionBlock(Action)"/>
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
        /// DOES NOT automatically <see cref="IBlockPlacer.ExpressionBlock(Action)"/>
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
