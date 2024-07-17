using FanScript.Compiler.Syntax;
using System.Collections.Frozen;
using System.Collections.ObjectModel;

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
    }

    public enum ModifierTarget
    {
        Variable,
        Function,
    }

    /// <summary>
    /// Extension methods for <see cref="Modifiers"/>
    /// </summary>
    public static class ModifiersE
    {
        private static readonly FrozenDictionary<Modifiers, ModifierInfo> lookup = new Dictionary<Modifiers, ModifierInfo>()
        {
            [Modifiers.Readonly] = new ModifierInfo(SyntaxKind.ReadOnlyModifier) { Targets = [ModifierTarget.Variable], Conflicts = [Modifiers.Constant] },
            [Modifiers.Constant] = new ModifierInfo(SyntaxKind.ConstantModifier) { Targets = [ModifierTarget.Variable], Conflicts = [Modifiers.Readonly] },
        }.ToFrozenDictionary();

        public static Modifiers FromKind(SyntaxKind kind)
            => kind switch
            {
                SyntaxKind.ReadOnlyModifier => Modifiers.Readonly,
                SyntaxKind.ConstantModifier => Modifiers.Constant,
                _ => throw new InvalidDataException($"SyntaxKind '{kind}' isn't a modifier"),
            };

        public static IEnumerable<ModifierTarget> GetTargets(this Modifiers mod)
            => lookup[mod].Targets;

        public static SyntaxKind ToKind(this Modifiers mod)
            => lookup[mod].Kind;

        public static IEnumerable<Modifiers> GetConflictingModifiers(this Modifiers mod)
            => lookup[mod].Conflicts;

        private class ModifierInfo
        {
            public readonly SyntaxKind Kind;
            public IReadOnlyCollection<ModifierTarget> Targets { get; init; }
            public IReadOnlyCollection<Modifiers> Conflicts { get; init; }

            public ModifierInfo(SyntaxKind _kind)
            {
                Kind = _kind;
                Targets = ReadOnlyCollection<ModifierTarget>.Empty;
                Conflicts = ReadOnlyCollection<Modifiers>.Empty;
            }
        }
    }
}
