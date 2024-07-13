using FanScript.Compiler.Symbols;

namespace FanScript.Compiler.Binding
{
    internal sealed class Conversion
    {
        public static readonly Conversion None = new Conversion(exists: false, isIdentity: false, isImplicit: false);
        public static readonly Conversion Identity = new Conversion(exists: true, isIdentity: true, isImplicit: true);
        public static readonly Conversion Implicit = new Conversion(exists: true, isIdentity: false, isImplicit: true);
        public static readonly Conversion Explicit = new Conversion(exists: true, isIdentity: false, isImplicit: false);

        private Conversion(bool exists, bool isIdentity, bool isImplicit)
        {
            Exists = exists;
            IsIdentity = isIdentity;
            IsImplicit = isImplicit;
        }

        public bool Exists { get; }
        public bool IsIdentity { get; }
        public bool IsImplicit { get; }
        public bool IsExplicit => Exists && !IsImplicit;

        public static Conversion Classify(TypeSymbol? from, TypeSymbol? to)
        {
            if (from is null || to is null)
                return None;

            if (from.GenericEquals(to))
                return Identity;

            if (from != TypeSymbol.Void && to == TypeSymbol.Any)
                return Implicit;

            if (from == TypeSymbol.Any && to != TypeSymbol.Void)
                return Explicit;

            //if (from == TypeSymbol.Bool || from == TypeSymbol.Float)
            //    if (to == TypeSymbol.String)
            //        return Explicit;

            //if (from == TypeSymbol.String)
            //    if (to == TypeSymbol.Bool || to == TypeSymbol.Float)
            //        return Explicit;

            return None;
        }
    }
}
