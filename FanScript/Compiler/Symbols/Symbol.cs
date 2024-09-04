namespace FanScript.Compiler.Symbols
{
    public abstract class Symbol
    {
        private protected Symbol(string name)
        {
            Name = name;
        }

        public abstract SymbolKind Kind { get; }
        public string Name { get; protected set; }

        public void WriteTo(TextWriter writer)
            => SymbolPrinter.WriteTo(this, writer);

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }

        public override int GetHashCode()
            => Name.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (obj is Symbol other) return Name == other.Name;
            else return false;
        }
    }
}
