using FanScript.Compiler.Binding;

namespace FanScript.Compiler.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constant)
            : base(name)
        {
            IsReadOnly = isReadOnly;
            Type = type;
            Constant = isReadOnly ? constant : null;
        }

        public bool IsReadOnly { get; }
        public TypeSymbol Type { get; }
        internal BoundConstant? Constant { get; }

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Type);

        public override bool Equals(object? obj)
        {
            if (obj is VariableSymbol other) return Name == other.Name && IsReadOnly == other.IsReadOnly && Equals(Type, other.Type);
            else return false;
        }
    }
}
