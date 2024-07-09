namespace FanScript.Compiler.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("?");
        public static readonly TypeSymbol Any = new TypeSymbol("any");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol Float = new TypeSymbol("float");
        public static readonly TypeSymbol Vector3 = new TypeSymbol("vec3");
        public static readonly TypeSymbol Rotation = new TypeSymbol("rot");
        //public static readonly TypeSymbol String = new TypeSymbol("string");
        public static readonly TypeSymbol Void = new TypeSymbol("void");

        private TypeSymbol(string name)
            : base(name)
        {
        }

        public override SymbolKind Kind => SymbolKind.Type;
    }
}
