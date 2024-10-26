using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Documentation.Attributes;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FanScript.Compiler
{
    /// <summary>
    /// Variable and function modifiers
    /// </summary>
    [Flags]
    public enum Modifiers : ushort
    {
        [ModifierDoc(
            Info = """
            Makes the variable/parameter readonly - can be assigned only once.

            Can be applied to all types of varibles.
            """,
            Remarks = [
                """
                Readonly vairables need to be initialized.
                """
            ],
            Examples = """
            <codeblock lang="fcs">
            readonly float a = 5 // works

            readonly float b // error - A readonly/constant variable needs to be initialized.
            </>
            """
        )]
        Readonly = 1 << 0,
        [ModifierDoc(
            Info = """
            Makes the variable constant - when compiled, references to this variable get replaced by it's value.
            
            Can be applied to the following variable types:
            <list>
            <item><link type="type">bool</></>
            <item><link type="type">float</></>
            <item><link type="type">vec3</></>
            <item><link type="type">rot</></>
            </>
            """,
            Remarks = [
                """
                Only constant values can be assigned to variables with this modifier.
                """,
                """
                Constant variables need to be initialized.
                """
            ],
            Examples = """
            <codeblock lang="fcs">
            const float a = 5 // works

            float b = 2
            const float c = b // error - Value must be constant.

            const float d // error - A readonly/constant variable needs to be initialized.
            </>
            """
        )]
        Constant = 1 << 1,
        [ModifierDoc(
            Info = """
            Similar to <link type="mod">out</>, but the argument is both taken in and out.
            """
        )]
        Ref = 1 << 2,
        [ModifierDoc(
            Info = """
            Instead of the argument being passed to the function, it is "returned" from it.
            """,
            Examples = """
            <codeblock lang="fcs">
            func add(float a, float b, out float res)
            {
                res = a + b
            }

            float resA
            add(2, 5, out resA)
            inspect(resA)

            // if a parameter has the out modifier, expression variable decleration can be used
            add(30, 25, out float resB)
            inspect(resB)

            // if you don't need the value of the out parameter, you can use a discard
            add(13, 22, out _)
            </>
            """
        )]
        Out = 1 << 3,
        [ModifierDoc(
            Info = """
            Makes the variable global - can be accesed from all functions.
            
            Can be applied to all types of vairables.
            """
        )]
        Global = 1 << 4,
        [ModifierDoc(
            Info = """
            Saves the variable. Gets reset when you edit the game.
            
            Can be applied to the following variable types:
            <list>
            <item><link type="type">float</></>
            </>
            """
        )]
        Saved = 1 << 5,
        [ModifierDoc(
            Info = """
            <list>
            <item>When applied to variable - instead of storing a value stores a reference to some code, which is ran every time the variable is accesed.</>
            <item>When applied to function - inlines the function for every call (by default functions which are called only once are inlined automatically) - replaces calls to the function by the code of the function.</>
            </>

            Can be applied to the following variable types:
            <list>
            <item><link type="type">bool</></>
            <item><link type="type">float</></>
            <item><link type="type">vec3</></>
            <item><link type="type">rot</></>
            <item><link type="type">obj</></>
            <item><link type="type">constr</></>
            </>
            """
        )]
        Inline = 1 << 6,
    }

    public enum ModifierTarget : byte
    {
        Variable,
        Parameter,
        Argument,
        Function,
    }

    /// <summary>
    /// Extension methods for <see cref="Modifiers"/>
    /// </summary>
    public static class ModifiersE
    {
        private static readonly FrozenDictionary<Modifiers, ModifierInfo> lookup = new Dictionary<Modifiers, ModifierInfo>()
        {
            [Modifiers.Readonly] = new ModifierInfo(SyntaxKind.ReadOnlyModifier, [ModifierTarget.Variable, ModifierTarget.Parameter]) { Conflicts = [Modifiers.Constant, Modifiers.Ref, Modifiers.Out] },
            [Modifiers.Constant] = new ModifierInfo(SyntaxKind.ConstantModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Readonly, Modifiers.Saved, Modifiers.Inline] },
            [Modifiers.Ref] = new ModifierInfo(SyntaxKind.RefModifier, [ModifierTarget.Argument]) { Conflicts = [Modifiers.Out, Modifiers.Readonly], MakesTargetReference = true },
            [Modifiers.Out] = new ModifierInfo(SyntaxKind.OutModifier, [ModifierTarget.Argument, ModifierTarget.Parameter]) { Conflicts = [Modifiers.Ref, Modifiers.Readonly], MakesTargetReference = true },
            [Modifiers.Global] = new ModifierInfo(SyntaxKind.GlobalModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Saved] },
            [Modifiers.Saved] = new ModifierInfo(SyntaxKind.SavedModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Global, Modifiers.Constant, Modifiers.Inline] },
            [Modifiers.Inline] = new ModifierInfo(SyntaxKind.InlineModifier, [ModifierTarget.Variable, ModifierTarget.Function]) { Conflicts = [Modifiers.Constant, Modifiers.Saved] },
        }.ToFrozenDictionary();

        public static Modifiers FromKind(SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.ReadOnlyModifier => Modifiers.Readonly,
                SyntaxKind.ConstantModifier => Modifiers.Constant,
                SyntaxKind.RefModifier => Modifiers.Ref,
                SyntaxKind.OutModifier => Modifiers.Out,
                SyntaxKind.GlobalModifier => Modifiers.Global,
                SyntaxKind.SavedModifier => Modifiers.Saved,
                SyntaxKind.InlineModifier => Modifiers.Inline,
                _ => throw new InvalidEnumArgumentException(nameof(kind), (int)kind, typeof(SyntaxKind)),
            };

        public static Modifiers Colaps(this IEnumerable<Modifiers> enumerable)
            => enumerable
            .Aggregate((Modifiers)0, (a, b) => a | b);

        public static Modifiers All()
            => Enum.GetValues<Modifiers>()
            .Colaps();

        public static Modifiers GetValidModifiersFor(ModifierTarget target, TypeSymbol? type)
        {
            if (target == ModifierTarget.Variable && type is not null)
            {
                Modifiers validMods = Modifiers.Readonly | Modifiers.Global;

                if (!type.IsGeneric)
                {
                    validMods |= Modifiers.Inline;
                    if (type != TypeSymbol.Object && type != TypeSymbol.Constraint)
                        validMods |= Modifiers.Constant;
                }
                if (type == TypeSymbol.Float)
                    validMods |= Modifiers.Saved;

                return validMods;
            }

            return lookup
                .Where(item => item.Value.Targets.Contains(target))
                .Select(item => item.Key)
                .Colaps();
        }

        public static IReadOnlyCollection<ModifierTarget> GetTargets(this Modifiers mod)
            => lookup[mod].Targets;

        public static SyntaxKind ToKind(this Modifiers mod)
            => lookup[mod].Kind;

        public static IReadOnlyCollection<Modifiers> GetConflictingModifiers(this Modifiers mod)
            => lookup[mod].Conflicts;

        /// <summary>
        /// Gets modifiers reuired by <paramref name="mod"/>
        /// </summary>
        /// <remarks>
        /// At least one of these must be present
        /// </remarks>
        /// <param name="mod"></param>
        /// <returns>The required modifiers</returns>
        public static IReadOnlyCollection<Modifiers> GetRequiredModifiers(this Modifiers mod)
            => lookup[mod].RequiredOneOf;

        public static bool MakesTargetReference(this Modifiers mods, [NotNullWhen(true)] out Modifiers? makesRefMod)
        {
            foreach (var (mod, info) in lookup)
                if (mods.HasFlag(mod) && info.MakesTargetReference)
                {
                    makesRefMod = mod;
                    return true;
                }

            makesRefMod = null;
            return false;
        }

        public static string ToSyntaxString(this Modifiers mods)
        {
            StringBuilder builder = new StringBuilder();
            mods.ToSyntaxString(builder);
            return builder.ToString();
        }
        public static void ToSyntaxString(this Modifiers mods, StringBuilder builder)
        {
            bool isFirst = true;

            foreach (var modifier in Enum.GetValues<Modifiers>())
                if (mods.HasFlag(modifier))
                {
                    if (!isFirst)
                        builder.Append(' ');

                    isFirst = false;

                    builder.Append(modifier.ToKind().GetText());
                }
        }

        private class ModifierInfo
        {
            public readonly SyntaxKind Kind;
            public IReadOnlyCollection<ModifierTarget> Targets { get; init; }
            public IReadOnlyCollection<Modifiers> Conflicts { get; init; }
            public IReadOnlyCollection<Modifiers> RequiredOneOf { get; init; }

            public bool MakesTargetReference { get; init; } = false;

            public ModifierInfo(SyntaxKind kind, IReadOnlyCollection<ModifierTarget> targets)
            {
                Kind = kind;
                Targets = targets;
                Conflicts = ReadOnlyCollection<Modifiers>.Empty;
                RequiredOneOf = ReadOnlyCollection<Modifiers>.Empty;
            }
        }
    }
}
