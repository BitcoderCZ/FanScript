namespace FanScript.Compiler.Symbols
{
    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        internal ParameterSymbol(string name, TypeSymbol type, int ordinal)
            : this(name, 0, type, ordinal)
        {
        }
        internal ParameterSymbol(string name, Modifiers modifiers, TypeSymbol type, int ordinal)
            : base(name, Modifiers.Readonly | modifiers, type)
        {
            Ordinal = ordinal;

            Initialize(null);
        }

        public override SymbolKind Kind => SymbolKind.Parameter;
        public int Ordinal { get; }
    }
}
