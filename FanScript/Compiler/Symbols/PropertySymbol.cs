using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Compiler.Symbols
{
    public sealed class PropertySymbol : LocalVariableSymbol
    {
        internal PropertySymbol(PropertyDefinitionSymbol definition, VariableSymbol baseVariable) : base(definition.Name, definition.Modifiers, definition.Type)
        {
            Initialize(definition.Constant);
            Definition = definition;
            BaseVariable = baseVariable;
        }

        public PropertyDefinitionSymbol Definition { get; }
        public VariableSymbol BaseVariable { get; }
    }

    public sealed class PropertyDefinitionSymbol : LocalVariableSymbol
    {
        internal delegate EmitStore GetDelegate(EmitContext context, VariableSymbol baseVariable);
        internal delegate EmitStore SetDelegate(EmitContext context, VariableSymbol baseVariable, Func<EmitStore> getStore);

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

        public override SymbolKind Kind => SymbolKind.Property;

        internal GetDelegate EmitGet { get; }
        internal SetDelegate? EmitSet { get; }
    }
}
