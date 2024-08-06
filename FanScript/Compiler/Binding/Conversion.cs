using FanScript.Compiler.Symbols;

namespace FanScript.Compiler.Binding
{
    internal sealed class Conversion
    {
        public static readonly Conversion None = new Conversion(exists: false, isIdentity: false, type: TypeEnum.None);
        public static readonly Conversion Identity = new Conversion(exists: true, isIdentity: true, type: TypeEnum.Direct);
        public static readonly Conversion Explicit = new Conversion(exists: true, isIdentity: false, type: TypeEnum.None);
        public static readonly Conversion Implicit = new Conversion(exists: true, isIdentity: false, type: TypeEnum.Implicit);
        public static readonly Conversion Direct = new Conversion(exists: true, isIdentity: false, type: TypeEnum.Direct);

        private Conversion(bool exists, bool isIdentity, TypeEnum type)
        {
            Exists = exists;
            IsIdentity = isIdentity;
            Type = type;
        }

        public bool Exists { get; }
        public bool IsIdentity { get; }
        public TypeEnum Type { get; }

        public static Conversion Classify(TypeSymbol? from, TypeSymbol? to)
        {
            if (from is null || to is null)
                return None;

            if (from.GenericEquals(to))
                return Identity;

            if (from == TypeSymbol.Null && to != TypeSymbol.Void)
                return Direct;

            //if (from == TypeSymbol.Bool || from == TypeSymbol.Float)
            //    if (to == TypeSymbol.String)
            //        return Explicit;

            //if (from == TypeSymbol.String)
            //    if (to == TypeSymbol.Bool || to == TypeSymbol.Float)
            //        return Explicit;

            return None;
        }

        public enum TypeEnum
        {
            None,
            Explicit,
            Implicit,
            Direct,
        }
    }
}
