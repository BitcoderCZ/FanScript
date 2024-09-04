using System.Diagnostics;

namespace FanScript.Compiler.Symbols
{
    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        internal GlobalVariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name, modifiers, type)
        {
            Debug.Assert(modifiers.HasFlag(Modifiers.Global) || modifiers.HasFlag(Modifiers.Saved));
        }

        public override SymbolKind Kind => SymbolKind.GlobalVariable;
    }
}
