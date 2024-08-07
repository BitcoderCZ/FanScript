namespace FanScript.Compiler.Symbols
{
    public sealed class NullVariableSymbol : VariableSymbol
    {
        public NullVariableSymbol() : base("_", Modifiers.Readonly, TypeSymbol.Null)
        {
        }

        public override SymbolKind Kind => SymbolKind.NullVariable;
    }
}
