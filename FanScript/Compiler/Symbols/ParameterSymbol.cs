namespace FanScript.Compiler.Symbols
{
    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        internal ParameterSymbol(string name, TypeSymbol type)
            : this(name, 0, type)
        {
        }
        internal ParameterSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name, modifiers, type)
        {
            Initialize(null);
        }

        public override SymbolKind Kind => SymbolKind.Parameter;
    }
}
