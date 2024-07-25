using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.FCInfo;
using FanScript.Utils;
using System.Collections.Frozen;
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

        public FrozenDictionary<string, PropertyDefinitionSymbol> Properties { get; private set; } = FrozenDictionary<string, PropertyDefinitionSymbol>.Empty;
        // TODO:
        // public ImmutableArray<FunctionSymbol> InstanceFunctions { get; private set; }

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

        static TypeSymbol()
        {
            BuiltinFunctions.Init();

            Vector3.Properties = new Dictionary<string, PropertyDefinitionSymbol>() {
                ["x"] = new PropertyDefinitionSymbol("x", Float, (context, variable) => getVectorComponent(context, variable, 0), (context, variable, getStore) => setVectorComponent(context, variable, getStore, 0)),
                ["y"] = new PropertyDefinitionSymbol("y", Float, (context, variable) => getVectorComponent(context, variable, 1), (context, variable, getStore) => setVectorComponent(context, variable, getStore, 1)),
                ["z"] = new PropertyDefinitionSymbol("z", Float, (context, variable) => getVectorComponent(context, variable, 2), (context, variable, getStore) => setVectorComponent(context, variable, getStore, 2)),
            }.ToFrozenDictionary();
            Rotation.Properties = new Dictionary<string, PropertyDefinitionSymbol>()
            {
                ["x"] = new PropertyDefinitionSymbol("x", Float, (context, variable) => getVectorComponent(context, variable, 0), (context, variable, getStore) => setVectorComponent(context, variable, getStore, 0)),
                ["y"] = new PropertyDefinitionSymbol("y", Float, (context, variable) => getVectorComponent(context, variable, 1), (context, variable, getStore) => setVectorComponent(context, variable, getStore, 1)),
                ["z"] = new PropertyDefinitionSymbol("z", Float, (context, variable) => getVectorComponent(context, variable, 2), (context, variable, getStore) => setVectorComponent(context, variable, getStore, 2)),
            }.ToFrozenDictionary();
            EmitStore getVectorComponent(EmitContext context, VariableSymbol variable, int index)
            {
                bool[] arr = new bool[3];
                arr[index] = true;
                return context.BreakVectorAny(new BoundVariableExpression(null!, variable), arr)[index]!;
            }
            EmitStore setVectorComponent(EmitContext context, VariableSymbol baseVariable, Func<EmitStore> getStore, int index)
            {
                WireType varType = baseVariable.Type.ToWireType();

                return context.EmitSetVariable(baseVariable, () =>
                {
                    Block make = context.Builder.AddBlock(Blocks.Math.MakeByType(varType));

                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        Block @break = context.Builder.AddBlock(Blocks.Math.BreakByType(varType));

                        context.Builder.BlockPlacer.ExpressionBlock(() =>
                        {
                            Block var = context.Builder.AddBlock(Blocks.Variables.VariableByType(varType));
                            context.Builder.SetBlockValue(var, 0, baseVariable.Name);

                            context.Connect(BasicEmitStore.COut(var, var.Type.Terminals[0]), BasicEmitStore.CIn(@break, @break.Type.Terminals[3]));
                        });

                        for (int i = 0; i < 3; i++)
                        {
                            if (i != index)
                                context.Connect(BasicEmitStore.COut(@break, @break.Type.Terminals[2 - i]), BasicEmitStore.CIn(make, make.Type.Terminals[(2 - i) + 1]));
                        }
                    });
                    context.Builder.BlockPlacer.ExpressionBlock(() =>
                    {
                        EmitStore store = getStore();

                        context.Connect(store, BasicEmitStore.CIn(make, make.Type.Terminals[(2 - index) + 1]));
                    });

                    return BasicEmitStore.COut(make, make.Type.Terminals[0]);
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
