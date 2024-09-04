using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
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
        Readonly = 1 << 0,
        Constant = 1 << 1,
        Ref = 1 << 2,
        Out = 1 << 3,
        Global = 1 << 4,
        Saved = 1 << 5,
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
            [Modifiers.Readonly] = new ModifierInfo(SyntaxKind.ReadOnlyModifier, [ModifierTarget.Variable, ModifierTarget.Parameter]) { Conflicts = [Modifiers.Constant] },
            [Modifiers.Constant] = new ModifierInfo(SyntaxKind.ConstantModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Readonly, Modifiers.Saved, Modifiers.Inline] },
            [Modifiers.Ref] = new ModifierInfo(SyntaxKind.RefModifier, [ModifierTarget.Argument, ModifierTarget.Parameter]) { Conflicts = [Modifiers.Out], MakesTargetReference = true },
            [Modifiers.Out] = new ModifierInfo(SyntaxKind.OutModifier, [ModifierTarget.Argument, ModifierTarget.Parameter]) { Conflicts = [Modifiers.Ref], MakesTargetReference = true },
            [Modifiers.Global] = new ModifierInfo(SyntaxKind.GlobalModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Saved] },
            [Modifiers.Saved] = new ModifierInfo(SyntaxKind.SavedModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Global, Modifiers.Constant, Modifiers.Inline] },
            [Modifiers.Inline] = new ModifierInfo(SyntaxKind.InlineModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Constant, Modifiers.Saved]/*, RequiredOneOf = [Modifiers.Readonly, Modifiers.Constant]*/ },
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
                _ => throw new InvalidDataException($"SyntaxKind '{kind}' isn't a modifier"),
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
