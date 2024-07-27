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
    }

    public enum ModifierTarget
    {
        Variable,
        Parameter,
        Function,
    }

    /// <summary>
    /// Extension methods for <see cref="Modifiers"/>
    /// </summary>
    public static class ModifiersE
    {
        private static readonly FrozenDictionary<Modifiers, ModifierInfo> lookup = new Dictionary<Modifiers, ModifierInfo>()
        {
            [Modifiers.Readonly] = new ModifierInfo(SyntaxKind.ReadOnlyModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Constant] },
            [Modifiers.Constant] = new ModifierInfo(SyntaxKind.ConstantModifier, [ModifierTarget.Variable]) { Conflicts = [Modifiers.Readonly] },
            [Modifiers.Ref] = new ModifierInfo(SyntaxKind.RefModifier, [ModifierTarget.Parameter]) { Conflicts = [Modifiers.Out], MakesTargetReference = true },
            [Modifiers.Out] = new ModifierInfo(SyntaxKind.OutModifier, [ModifierTarget.Parameter]) { Conflicts = [Modifiers.Ref], MakesTargetReference = true },
        }.ToFrozenDictionary();

        public static Modifiers FromKind(SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.ReadOnlyModifier => Modifiers.Readonly,
                SyntaxKind.ConstantModifier => Modifiers.Constant,
                SyntaxKind.RefModifier => Modifiers.Ref,
                SyntaxKind.OutModifier => Modifiers.Out,
                _ => throw new InvalidDataException($"SyntaxKind '{kind}' isn't a modifier"),
            };

        public static IEnumerable<ModifierTarget> GetTargets(this Modifiers mod)
            => lookup[mod].Targets;

        public static SyntaxKind ToKind(this Modifiers mod)
            => lookup[mod].Kind;

        public static IEnumerable<Modifiers> GetConflictingModifiers(this Modifiers mod)
            => lookup[mod].Conflicts;

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

        public static string ToSyntaxString(this Modifiers mod)
        {
            StringBuilder builder = new StringBuilder();
            mod.ToSyntaxString(builder);
            return builder.ToString();
        }
        public static void ToSyntaxString(this Modifiers mod, StringBuilder builder)
        {
            bool isFirst = true;

            foreach (var modifier in Enum.GetValues<Modifiers>())
                if (mod.HasFlag(modifier))
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

            public bool MakesTargetReference { get; init; } = false;

            public ModifierInfo(SyntaxKind _kind, IReadOnlyCollection<ModifierTarget> _targets)
            {
                Kind = _kind;
                Targets = _targets;
                Conflicts = ReadOnlyCollection<Modifiers>.Empty;
            }
        }
    }
}
