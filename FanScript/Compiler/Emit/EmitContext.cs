using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;
using FanScript.FCInfo;

namespace FanScript.Compiler.Emit
{
    internal sealed class EmitContext
    {
        private Func<BoundStatement, EmitStore> emitStatement;
        private Func<BoundExpression, EmitStore> emitExpression;
        private Action<EmitStore, EmitStore> connect;

        public readonly CodeBuilder Builder;
        public readonly DiagnosticBag Diagnostics;

        public EmitContext(CodeBuilder _builder, DiagnosticBag _diagnostics, Func<BoundStatement, EmitStore> _emitStatement, Func<BoundExpression, EmitStore> _emitExpression, Action<EmitStore, EmitStore> _connect)
        {
            Builder = _builder;

            emitStatement = _emitStatement;
            emitExpression = _emitExpression;
            connect = _connect;
            Diagnostics = _diagnostics;
        }

        public EmitStore EmitStatement(BoundStatement statement)
            => emitStatement(statement);
        public EmitStore EmitExpression(BoundExpression expression)
            => emitExpression(expression);

        public void Connect(EmitStore from, EmitStore to)
            => connect(from, to);

        public object[]? ValidateConstants(ReadOnlySpan<BoundExpression> expressions, bool mustBeConstant)
        {
            object[] values = new object[expressions.Length];
            bool invalid = false;

            for (int i = 0; i < expressions.Length; i++)
            {
                BoundConstant? constant = expressions[i].ConstantValue;
                if (constant is null)
                {
                    if (mustBeConstant)
                        Diagnostics.ReportValueMustBeConstant(expressions[i].Syntax.Location);
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
            for (int i = 0; i < text.Length; i += FancadeConstants.MaxCommentLength)
            {
                Block block = Builder.AddBlock(Blocks.Values.Comment);
                Builder.SetBlockValue(block, 0, text.Substring(i, Math.Min(FancadeConstants.MaxCommentLength, text.Length - i)));
            }
        }
    }
}
