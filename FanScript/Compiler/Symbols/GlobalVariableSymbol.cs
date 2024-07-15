using FanScript.Compiler.Binding;

namespace FanScript.Compiler.Symbols
{
    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        internal GlobalVariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name, modifiers, type)
        {
        }

        public override SymbolKind Kind => SymbolKind.GlobalVariable;
    }
}
