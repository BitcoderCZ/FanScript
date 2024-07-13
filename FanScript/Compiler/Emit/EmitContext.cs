using FanScript.Compiler.Binding;
using FanScript.Compiler.Diagnostics;

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

        public object[]? ValidateConstants(IList<BoundExpression> expressions)
        {
            object[] values = new object[expressions.Count];
            bool invalid = false;

            for (int i = 0; i < values.Length; i++)
            {
                BoundConstant? constant = expressions[i].ConstantValue;
                if (constant is null)
                {
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
    }
}
