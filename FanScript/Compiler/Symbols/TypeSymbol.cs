using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FanScript.Compiler.Binding;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Documentation.Attributes;
using FanScript.FCInfo;
using FanScript.Utils;

namespace FanScript.Compiler.Symbols;

public sealed class TypeSymbol : Symbol
{
    public static readonly TypeSymbol Error = new TypeSymbol("?");
    [TypeDoc(
        Info = """
        If used as an argument or in a constant operation - gets converted to the default value (0, vec3(0, 0, 0)), otherwise, when emited, no block gets placed.
        """,
        HowToCreate = """
        <codeblock lang="fcs">
        float a = null // emits just the set variable block
        float b = 5 + null // emits b = 5

        // loops from 0 to 5, nothing gets assigned to the start input of the loop block
        on Loop(null, 5)
        {

        }
        </>
        """,
        Remarks = [
            """
            Null has implicit cast to all types.
            """
        ])]
    public static readonly TypeSymbol Null = new TypeSymbol("null");

    // used in function return type or parameter
    public static readonly TypeSymbol Generic = new TypeSymbol("generic");
    [TypeDoc(
        Info = """
        Holds one of 2 values: true or false.
        """,
        HowToCreate = """
        <codeblock lang="fcs">
        bool a = false
        bool b = true
        </>
        """)]
    public static readonly TypeSymbol Bool = new TypeSymbol("bool");
    [TypeDoc(
        Info = """
        A floating point number.
        """,
        HowToCreate = """
        <codeblock lang="fcs">
        float a = 10
        float b = 3.14
        float c = .5
        float d = 0b1010_0101 // binary, _ is optional
        float e = 0x1234_ABCD // hexadecimal, _ is optional
        </>
        """)]
    public static readonly TypeSymbol Float = new TypeSymbol("float");
    [TypeDoc(
        Info = """
        List of characters (text).
        """,
        HowToCreate = """
        Strings cannot currently be used as variables, only as arguments to functions.  
        For example:
        <codeblock lang="fcs">
        shopSection("The string")
        </>
        """)]
    public static readonly TypeSymbol String = new TypeSymbol("string");
    [TypeDoc(
        Info = """
        A vector of 3 <link type="type">float</>s.
        """,
        HowToCreate = """
        <codeblock lang="fcs">
        // vec3(float x, float y, float z)
        vec3 a = vec3(1, 2, 3) // constant - uses the vector block

        float y = 10
        vec3 b = vec3(5, y, 5) // uses the make vector block

        const float x = 8
        vec3 c = vec3(x, 3, 5) // constant - uses the vector block
        </>
        """)]
    public static readonly TypeSymbol Vector3 = new TypeSymbol("vec3");
    [TypeDoc(
        Info = """
        Represents a rotation using euler angles, internaly uses quaternion.
        """,
        HowToCreate = """
        <codeblock lang="fcs">
        // rot(float x, float y, float z) - euler angle in degrees
        rot a = rot(45, 90, 45) // constant - uses the vector block

        float y = 30
        rot b = rot(60, y, 180) // uses the make vector block

        const float x = 45
        rot c = rot(x, 60, 10) // constant - uses the vector block
        </>
        """)]
    public static readonly TypeSymbol Rotation = new TypeSymbol("rot");
    [TypeDoc(
        Info = """
        A fancade object.
        """,
        HowToCreate = """
        <codeblock lang="fcs">
        obj a = getObject(5, 0, 0) // gets the object located at (5, 0, 0)
        obj b = getBlockById(BLOCK_GRASS) // places the GRASS block (id 3) and returns a reference to it
        // comming soon: obj c = getBlockByName("My block") // places the block with nane "My block" and returns a reference to it
        </>
        """)]
    public static readonly TypeSymbol Object = new TypeSymbol("obj");
    [TypeDoc(
        Info = """
        Fancade constraint - constraints the movement of a physics object.
        """,
        HowToCreate = """
        <codeblock lang="fcs">
        object base = getObject(20, 0, 5)
        object part = getObject(25, 0, 5)
        base.addConstraint(part, out constr a)
        </>
        """)]
    public static readonly TypeSymbol Constraint = new TypeSymbol("constr");
    [TypeDoc(
        Info = """
        An array/list of a type.
        """,
        HowToCreate = """
        <codeblock lang="fcs">
        // using arraySegment
        array<float> a = [1, 2, 3]

        // using setRange function
        array<bool> b
        b.setRange(0, [true, false, true])

        // using multiple set's
        array<vec3> c
        c.set(0, vec3(1, 2, 3))
        c.set(1, vec3(4, 5, 6))
        c.set(2, vec3(7, 8, 9))
        </>
        """,
        Related = [
            """
            <link type="type">arraySegment</>
            """
        ])]
    public static readonly TypeSymbol Array = new TypeSymbol("array", true);
    [TypeDoc(
        Info = """
        Represents a segment of values.
        """,
        HowToCreate = """
        Array segment isn't a runtime type - variables of the type cannot be created.  
        Array segment can only be used to:
        <list>
        <item>create <link type="type">array</>s</>
        <item>as argument in functions <link type="func">setRange;array;float;arraySegment</></>
        </>
        <codeblock lang="fcs">
        array<float> arr = [1, 2, 3]
        arr.setRange(3, [4, 5, 6])
        </>
        """,
        Related = [
            """
            <link type="type">array</>
            """
        ])]
    public static readonly TypeSymbol ArraySegment = new TypeSymbol("arraySegment", true);
    public static readonly TypeSymbol Void = new TypeSymbol("void");

    public static readonly ImmutableArray<TypeSymbol> BuiltInGenericTypes = [Array];

    public static readonly ImmutableArray<TypeSymbol> BuiltInNonGenericTypes = [Bool, Float, Vector3, Rotation, Object, Constraint];
    public static readonly ImmutableArray<TypeSymbol> BuiltInTypes = BuiltInGenericTypes.AddRange(BuiltInNonGenericTypes);

    public static readonly IReadOnlyDictionary<TypeSymbol, TypeDocAttribute> TypeToDoc;

    private static ImmutableArray<TypeSymbol> allTypes;

    static TypeSymbol()
    {
        BuiltinFunctions.Init();

        TypeToDoc = typeof(TypeSymbol)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(TypeSymbol))
            .Select(f =>
            {
                TypeSymbol group = (TypeSymbol)f.GetValue(null)!;
                TypeDocAttribute attrib = f.GetCustomAttribute<TypeDocAttribute>()!;

                return (group, attrib);
            })
            .Where(item => item.attrib is not null)
           .ToDictionary()
           .AsReadOnly();

        Vector3.Properties = new Dictionary<string, PropertyDefinitionSymbol>()
        {
            ["x"] = new PropertyDefinitionSymbol("x", Float, (context, expression) => GetVectorComponent(context, expression, 0), (context, expression, getStore) => SetVectorComponent(context, expression, getStore, 0)),
            ["y"] = new PropertyDefinitionSymbol("y", Float, (context, expression) => GetVectorComponent(context, expression, 1), (context, expression, getStore) => SetVectorComponent(context, expression, getStore, 1)),
            ["z"] = new PropertyDefinitionSymbol("z", Float, (context, expression) => GetVectorComponent(context, expression, 2), (context, expression, getStore) => SetVectorComponent(context, expression, getStore, 2)),
        }.ToFrozenDictionary();
        Rotation.Properties = new Dictionary<string, PropertyDefinitionSymbol>()
        {
            ["x"] = new PropertyDefinitionSymbol("x", Float, (context, expression) => GetVectorComponent(context, expression, 0), (context, expression, getStore) => SetVectorComponent(context, expression, getStore, 0)),
            ["y"] = new PropertyDefinitionSymbol("y", Float, (context, expression) => GetVectorComponent(context, expression, 1), (context, expression, getStore) => SetVectorComponent(context, expression, getStore, 1)),
            ["z"] = new PropertyDefinitionSymbol("z", Float, (context, expression) => GetVectorComponent(context, expression, 2), (context, expression, getStore) => SetVectorComponent(context, expression, getStore, 2)),
        }.ToFrozenDictionary();
        IEmitStore GetVectorComponent(IEmitContext context, BoundExpression expression, int index)
        {
            bool[] arr = new bool[3];
            arr[index] = true;
            return context.BreakVectorAny(expression, arr)[index]!;
        }

        IEmitStore SetVectorComponent(IEmitContext context, BoundExpression expression, Func<IEmitStore> getStore, int index)
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
                            IEmitStore expressionStore = context.EmitExpression(expression);

                            context.Connect(expressionStore, BasicEmitStore.CIn(@break, @break.Type.TerminalArray[3]));
                        }

                        for (int i = 0; i < 3; i++)
                        {
                            if (i != index)
                            {
                                context.Connect(BasicEmitStore.COut(@break, @break.Type.TerminalArray[2 - i]), BasicEmitStore.CIn(make, make.Type.TerminalArray[(2 - i) + 1]));
                            }
                        }
                    }

                    using (context.ExpressionBlock())
                    {
                        IEmitStore store = getStore();

                        context.Connect(store, BasicEmitStore.CIn(make, make.Type.TerminalArray[(2 - index) + 1]));
                    }
                }

                return BasicEmitStore.COut(make, make.Type.TerminalArray[0]);
            });
        }
    }

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

    public static ImmutableArray<TypeSymbol> AllTypes
        => allTypes.IsDefault ?
            (allTypes = typeof(TypeSymbol).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.FieldType == typeof(TypeSymbol)).Select(field => (TypeSymbol)field.GetValue(null)!).ToImmutableArray()) :
            allTypes;

    public override SymbolKind Kind => SymbolKind.Type;

    public bool IsGeneric => IsGenericDefinition || IsGenericInstance;

    public bool IsGenericDefinition { get; }

    [MemberNotNullWhen(true, nameof(InnerType))]
    public bool IsGenericInstance { get; }

    public TypeSymbol? InnerType { get; }

    public FrozenDictionary<string, PropertyDefinitionSymbol> Properties { get; private set; } = FrozenDictionary<string, PropertyDefinitionSymbol>.Empty;

    public static TypeSymbol CreateGenericInstance(TypeSymbol type, TypeSymbol innerType)
        => !type.IsGenericDefinition
            ? throw new ArgumentException(nameof(type), $"{nameof(type)} must be generic definition.")
            : innerType.IsGeneric
            ? throw new ArgumentException(nameof(innerType), $"{nameof(innerType)} must not be generic.")
            : new TypeSymbol(type.Name, innerType);

    public static TypeSymbol GetGenericDefinition(TypeSymbol genericInstance)
    {
        if (!genericInstance.IsGenericInstance)
        {
            throw new ArgumentException(nameof(genericInstance), $"{nameof(genericInstance)} must be generic instance.");
        }

        TypeSymbol definition = GetTypeInternal(genericInstance.Name);

        Debug.Assert(definition.IsGenericDefinition, $"The returned type from {nameof(GetTypeInternal)} for a generic instance must be a generic definition");

        return definition;
    }

    public static TypeSymbol GetType(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Error;
        }

        for (int i = 0; i < BuiltInTypes.Length; i++)
        {
            if (BuiltInTypes[i].Name == name)
            {
                return BuiltInTypes[i];
            }
        }

        return Error;
    }

    public static TypeSymbol GetTypeInternal(ReadOnlySpan<char> name)
    {
        if (name.IsEmpty)
        {
            return Error;
        }

        for (int i = 0; i < AllTypes.Length; i++)
        {
            if (name.Equals(AllTypes[i].Name.AsSpan(), StringComparison.InvariantCulture))
            {
                return AllTypes[i];
            }
        }

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

    public override void WriteTo(TextWriter writer)
    {
        writer.SetForeground(IsGeneric ? ConsoleColor.DarkGreen : ConsoleColor.Blue);
        writer.Write(Name);
        writer.ResetColor();
        if (IsGeneric)
        {
            writer.WritePunctuation(SyntaxKind.LessToken);
            if (IsGenericInstance)
            {
                InnerType.WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.GreaterToken);
        }
    }

    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), InnerType);

    public override bool Equals(object? obj)
        => obj is TypeSymbol other && GenericEquals(other);

    public class FuntionParamsComparer : IEqualityComparer<TypeSymbol>
    {
        public bool Equals(TypeSymbol? x, TypeSymbol? y)
        {
            if (x == y)
            {
                return true;
            }
            else if (x is null || y is null)
            {
                return false;
            }

            return x.GenericEquals(y) ||
                (x.Equals(Generic) || y.Equals(Generic)) ||
                (x.NonGenericEquals(y) && ((x.IsGenericInstance && y.IsGenericDefinition) || (x.IsGenericDefinition && y.IsGenericInstance)));
        }

        public int GetHashCode([DisallowNull] TypeSymbol obj)
            => 0; // can't use normal gethashcode, because any type can equal generic
    }
}
