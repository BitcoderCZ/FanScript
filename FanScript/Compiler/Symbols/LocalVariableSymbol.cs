using FanScript.Compiler.Binding;

namespace FanScript.Compiler.Symbols
{
    public class LocalVariableSymbol : VariableSymbol
    {
        internal LocalVariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name, modifiers, type)
        {
        }

        public override SymbolKind Kind => SymbolKind.LocalVariable;
    }
}
