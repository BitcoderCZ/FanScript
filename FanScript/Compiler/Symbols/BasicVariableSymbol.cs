namespace FanScript.Compiler.Symbols
{
    public class BasicVariableSymbol : VariableSymbol
    {
        internal BasicVariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name, modifiers, type)
        {
        }

        public override SymbolKind Kind => SymbolKind.BasicVariable;
    }
}
