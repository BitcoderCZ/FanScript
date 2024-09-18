using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.FCInfo;
using FanScript.Utils;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FanScript.Compiler.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("?");
        public static readonly TypeSymbol Null = new TypeSymbol("null");
        // used in function return type or parameter
        public static readonly TypeSymbol Generic = new TypeSymbol("generic");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol Float = new TypeSymbol("float");
        public static readonly TypeSymbol String = new TypeSymbol("string");
        public static readonly TypeSymbol Vector3 = new TypeSymbol("vec3");
        public static readonly TypeSymbol Rotation = new TypeSymbol("rot");
        public static readonly TypeSymbol Object = new TypeSymbol("obj");
        public static readonly TypeSymbol Constraint = new TypeSymbol("constr");
        public static readonly TypeSymbol Array = new TypeSymbol("array", true);
        public static readonly TypeSymbol ArraySegment = new TypeSymbol("arraySegment", true);
        public static readonly TypeSymbol Void = new TypeSymbol("void");

        public bool IsGeneric => IsGenericDefinition || IsGenericInstance;

        public bool IsGenericDefinition { get; }

        [MemberNotNullWhen(true, nameof(InnerType))]
        public bool IsGenericInstance { get; }
        public TypeSymbol? InnerType { get; }

        public FrozenDictionary<string, PropertyDefinitionSymbol> Properties { get; private set; } = FrozenDictionary<string, PropertyDefinitionSymbol>.Empty;
        // TODO:
        // public ImmutableArray<FunctionSymbol> InstanceFunctions { get; private set; }

        public static readonly ImmutableArray<TypeSymbol> BuiltInGenericTypes = [Array];
        public static readonly ImmutableArray<TypeSymbol> BuiltInNonGenericTypes = [Bool, Float, Vector3, Rotation, Object, Constraint];
        public static readonly ImmutableArray<TypeSymbol> BuiltInTypes = BuiltInGenericTypes.AddRange(BuiltInNonGenericTypes);

        private static ImmutableArray<TypeSymbol> allTypes;
        public static ImmutableArray<TypeSymbol> AllTypes
            => allTypes.IsDefault ?
                (allTypes = typeof(TypeSymbol).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.FieldType == typeof(TypeSymbol)).Select(field => (TypeSymbol)field.GetValue(null)!).ToImmutableArray()) :
                allTypes;

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

        static TypeSymbol()
        {
            BuiltinFunctions.Init();

            Vector3.Properties = new Dictionary<string, PropertyDefinitionSymbol>()
            {
                ["x"] = new PropertyDefinitionSymbol("x", Float, (context, expression) => getVectorComponent(context, expression, 0), (context, expression, getStore) => setVectorComponent(context, expression, getStore, 0)),
                ["y"] = new PropertyDefinitionSymbol("y", Float, (context, expression) => getVectorComponent(context, expression, 1), (context, expression, getStore) => setVectorComponent(context, expression, getStore, 1)),
                ["z"] = new PropertyDefinitionSymbol("z", Float, (context, expression) => getVectorComponent(context, expression, 2), (context, expression, getStore) => setVectorComponent(context, expression, getStore, 2)),
            }.ToFrozenDictionary();
            Rotation.Properties = new Dictionary<string, PropertyDefinitionSymbol>()
            {
                ["x"] = new PropertyDefinitionSymbol("x", Float, (context, expression) => getVectorComponent(context, expression, 0), (context, expression, getStore) => setVectorComponent(context, expression, getStore, 0)),
                ["y"] = new PropertyDefinitionSymbol("y", Float, (context, expression) => getVectorComponent(context, expression, 1), (context, expression, getStore) => setVectorComponent(context, expression, getStore, 1)),
                ["z"] = new PropertyDefinitionSymbol("z", Float, (context, expression) => getVectorComponent(context, expression, 2), (context, expression, getStore) => setVectorComponent(context, expression, getStore, 2)),
            }.ToFrozenDictionary();
            EmitStore getVectorComponent(EmitContext context, BoundExpression expression, int index)
            {
                bool[] arr = new bool[3];
                arr[index] = true;
                return context.BreakVectorAny(expression, arr)[index]!;
            }
            EmitStore setVectorComponent(EmitContext context, BoundExpression expression, Func<EmitStore> getStore, int index)
            {
                WireType varType = expression.Type.ToWireType();

                return context.EmitSetExpression(expression, () =>
                {
                    Block make;

                    using (context.ExpressionBlock())
                    {
                        make = context.AddBlock(Blocks.Math.MakeByType(varType));

                        using (context.ExpressionBlock())
                        {
                            Block @break = context.AddBlock(Blocks.Math.BreakByType(varType));

                            using (context.ExpressionBlock())
                            {
                                EmitStore expressionStore = context.EmitExpression(expression);

                                context.Connect(expressionStore, BasicEmitStore.CIn(@break, @break.Type.TerminalArray[3]));
                            }

                            for (int i = 0; i < 3; i++)
                            {
                                if (i != index)
                                    context.Connect(BasicEmitStore.COut(@break, @break.Type.TerminalArray[2 - i]), BasicEmitStore.CIn(make, make.Type.TerminalArray[(2 - i) + 1]));
                            }
                        }
                        using (context.ExpressionBlock())
                        {
                            EmitStore store = getStore();

                            context.Connect(store, BasicEmitStore.CIn(make, make.Type.TerminalArray[(2 - index) + 1]));
                        }
                    }

                    return BasicEmitStore.COut(make, make.Type.TerminalArray[0]);
                });
            }
        }

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

        public static TypeSymbol GetTypeInternal(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return Error;

            for (int i = 0; i < AllTypes.Length; i++)
                if (AllTypes[i].Name == name)
                    return AllTypes[i];

            return Error;
        }

        public bool IsGenericDefinitionOf(TypeSymbol type)
            => IsGenericDefinition && type.IsGenericInstance && NonGenericEquals(type);
        public bool IsGenericInstanceOf(TypeSymbol type)
            => IsGenericInstance && type.IsGenericDefinition && NonGenericEquals(type);

        public PropertyDefinitionSymbol? GetProperty(string name)
            => Properties.TryGetValue(name, out var property) ? property : null;

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
