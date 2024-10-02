using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Symbols.Variables;
using FanScript.FCInfo;

namespace FanScript.Compiler.Emit
{
    internal interface IEmitContext
    {
        DiagnosticBag Diagnostics { get; }
        BlockBuilder Builder { get; }

        EmitStore EmitStatement(BoundStatement statement);
        EmitStore EmitExpression(BoundExpression expression);

        IDisposable StatementBlock();
        IDisposable ExpressionBlock();

        Block AddBlock(BlockDef def);
        void Connect(EmitStore from, EmitStore to);
        void SetBlockValue(Block block, int valueIndex, object value);

        EmitStore EmitLiteralExpression(object? value);

        EmitStore EmitGetVariable(VariableSymbol variable);
        /// <summary>
        /// Emits setting <paramref name="expression"/> to <paramref name="getValueStore"/>
        /// </summary>
        /// <remarks>
        /// DOES NOT automatically call <see cref="CodePlacer.ExpressionBlock()"/>
        /// </remarks>
        /// <param name="expression"></param>
        /// <param name="getValueStore"></param>
        /// <returns></returns>
        EmitStore EmitSetExpression(BoundExpression expression, Func<EmitStore> getValueStore);
        /// <summary>
        /// Emits setting <paramref name="variable"/> to <paramref name="getValueStore"/>
        /// </summary>
        /// <remarks>
        /// DOES NOT automatically call <see cref="CodePlacer.ExpressionBlock()"/>
        /// </remarks>
        /// <param name="expression"></param>
        /// <param name="getValueStore"></param>
        /// <returns></returns>
        EmitStore EmitSetVariable(VariableSymbol variable, Func<EmitStore> getValueStore);

        (EmitStore X, EmitStore Y, EmitStore Z) BreakVector(BoundExpression expression);
        EmitStore?[] BreakVectorAny(BoundExpression expression, bool[] useComponent);

        object?[]? ValidateConstants(ReadOnlyMemory<BoundExpression> expressions, bool mustBeConstant);

        void WriteComment(string text);
    }
}
