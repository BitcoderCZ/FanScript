using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;

namespace FanScript.Compiler.Symbols.Variables
{
    public sealed class PropertySymbol : BasicVariableSymbol
    {
        internal PropertySymbol(PropertyDefinitionSymbol definition, BoundExpression expression)
            : base(definition.Name, definition.Modifiers, definition.Type)
        {
            Initialize(definition.Constant);
            Definition = definition;
            Expression = expression;
        }

        public PropertyDefinitionSymbol Definition { get; }

        internal BoundExpression Expression { get; }
    }

    public sealed class PropertyDefinitionSymbol : BasicVariableSymbol
    {
        internal PropertyDefinitionSymbol(string name, TypeSymbol type, GetDelegate emitGet)
            : base(name, Modifiers.Readonly, type)
        {
            Initialize(null);
            EmitGet = emitGet;
        }

        internal PropertyDefinitionSymbol(string name, TypeSymbol type, GetDelegate emitGet, SetDelegate? emitSet)
            : base(name, 0, type)
        {
            Initialize(null);
            EmitGet = emitGet;
            EmitSet = emitSet;
        }

        internal PropertyDefinitionSymbol(string name, TypeSymbol type, object constantValue)
            : base(name, Modifiers.Constant, type)
        {
            Initialize(new BoundConstant(constantValue));
            EmitGet = (context, _) => context.EmitLiteralExpression(Constant!.Value);
        }

        internal delegate IEmitStore GetDelegate(IEmitContext context, BoundExpression expression);

        internal delegate IEmitStore SetDelegate(IEmitContext context, BoundExpression expression, Func<IEmitStore> getStore);

        public override SymbolKind Kind => SymbolKind.Property;

        internal GetDelegate EmitGet { get; }

        internal SetDelegate? EmitSet { get; }
    }
}
