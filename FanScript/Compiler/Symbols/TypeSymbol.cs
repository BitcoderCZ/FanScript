using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace FanScript.Compiler.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("?");
        public static readonly TypeSymbol Any = new TypeSymbol("any");
        // used in function return type or parameter
        public static readonly TypeSymbol Generic = new TypeSymbol("generic");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol Float = new TypeSymbol("float");
        public static readonly TypeSymbol Vector3 = new TypeSymbol("vec3");
        public static readonly TypeSymbol Rotation = new TypeSymbol("rot");
        public static readonly TypeSymbol Object = new TypeSymbol("object");
        public static readonly TypeSymbol Array = new TypeSymbol("array", true);
        public static readonly TypeSymbol Void = new TypeSymbol("void");

        public bool IsGeneric => IsGenericDefinition || IsGenericInstance;

        public bool IsGenericDefinition { get; }

        [MemberNotNullWhen(true, nameof(InnerType))]
        public bool IsGenericInstance { get; }
        public TypeSymbol? InnerType { get; }

        public static readonly ImmutableArray<TypeSymbol> BuiltInTypes = [Bool, Float, Vector3, Rotation, Object, Array];
        public static readonly ImmutableArray<TypeSymbol> BuiltInNonGenericTypes = [Bool, Float, Vector3, Rotation, Object];

        private TypeSymbol(string name, bool isGenericDefinition = false)
            : base(name)
        {
            IsGenericDefinition = isGenericDefinition;
        }

        private TypeSymbol(string name, TypeSymbol innerType)
            : base(name)
        {
            IsGenericInstance = true;
            InnerType = innerType;
        }

        public override SymbolKind Kind => SymbolKind.Type;

        public static TypeSymbol CreateGenericInstance(TypeSymbol type, TypeSymbol innerType)
        {
            if (!type.IsGenericDefinition)
                throw new ArgumentException(nameof(type), $"{nameof(type)} must be generic definition");
            else if (innerType.IsGeneric) // we don't allow generic type in a generic type
                throw new ArgumentException(nameof(innerType), $"{nameof(innerType)} must not be generic");

            return new TypeSymbol(type.Name, innerType);
        }

        public static TypeSymbol GetType(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return Error;

            for (int i = 0; i < BuiltInTypes.Length; i++)
                if (BuiltInTypes[i].Name == name)
                    return BuiltInTypes[i];

            return Error;
        }

        public bool IsGenericDefinitionOf(TypeSymbol type)
            => IsGenericDefinition && type.IsGenericInstance && NonGenericEquals(type);
        public bool IsGenericInstanceOf(TypeSymbol type)
            => IsGenericInstance && type.IsGenericDefinition && NonGenericEquals(type);

        public bool GenericEquals(TypeSymbol other)
            => Name == other.Name && Equals(InnerType, other.InnerType);
        public bool NonGenericEquals(TypeSymbol other)
            => Name == other.Name;

        public override string ToString()
        {
            if (IsGenericDefinition)
                return Name + "<>";
            else if (IsGenericInstance)
                return Name + $"<{InnerType}>";
            else
                return Name;
        }

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), InnerType);

        public override bool Equals(object? obj)
        {
            if (obj is TypeSymbol other) return GenericEquals(other);
            else return false;
        }

        public class FuntionParamsComparer : IEqualityComparer<TypeSymbol>
        {
            public bool Equals(TypeSymbol? x, TypeSymbol? y)
            {
                if (x == y) return true;
                else if (x is null || y is null) return false;

                return x.GenericEquals(y) ||
                    (x.Equals(Generic) || y.Equals(Generic)) ||
                    (x.NonGenericEquals(y) && ((x.IsGenericInstance && y.IsGenericDefinition) || (x.IsGenericDefinition && y.IsGenericInstance)));
            }

            public int GetHashCode([DisallowNull] TypeSymbol obj)
                => 0; // can't use normal gethashcode, because any type can equal generic
        }
    }
}
