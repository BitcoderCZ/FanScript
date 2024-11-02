using FanScript.Compiler.Symbols;

namespace FanScript.Compiler.Binding
{
    internal sealed class Conversion
    {
        public static readonly Conversion None = new Conversion(exists: false, isIdentity: false, type: ConversionType.None);
        public static readonly Conversion Identity = new Conversion(exists: true, isIdentity: true, type: ConversionType.Direct);
        public static readonly Conversion Explicit = new Conversion(exists: true, isIdentity: false, type: ConversionType.None);
        public static readonly Conversion Implicit = new Conversion(exists: true, isIdentity: false, type: ConversionType.Implicit);
        public static readonly Conversion Direct = new Conversion(exists: true, isIdentity: false, type: ConversionType.Direct);

        private Conversion(bool exists, bool isIdentity, ConversionType type)
        {
            Exists = exists;
            IsIdentity = isIdentity;
            Type = type;
        }

        public enum ConversionType
        {
            None,
            Explicit,
            Implicit,
            Direct,
        }

        public bool Exists { get; }

        public bool IsIdentity { get; }

        public ConversionType Type { get; }

        public static Conversion Classify(TypeSymbol? from, TypeSymbol? to)
            => from is null || to is null
                ? None
                : from.GenericEquals(to)
                ? Identity
                : from == TypeSymbol.Null && to != TypeSymbol.Void
                ? Direct
                : None;
    }
}
