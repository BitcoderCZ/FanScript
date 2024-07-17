using FanScript.Compiler.Binding;

namespace FanScript.Compiler.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name)
        {
            Modifiers = modifiers;
            Type = type;
        }

        public Modifiers Modifiers { get; }
        public bool IsReadOnly => Modifiers.HasFlag(Modifiers.Readonly) || Modifiers.HasFlag(Modifiers.Constant);
        public TypeSymbol Type { get; }
        internal BoundConstant? Constant { get; private set; }

        /// <summary>
        /// Used to know if a Read-Only variable has been assigned
        /// </summary>
        public bool Initialized { get; private set; }

        internal void Initialize(BoundConstant? value)
        {
            if (Initialized) return;

            Constant = Modifiers.HasFlag(Modifiers.Constant) ? value : null;
            Initialized = true;
        }

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Type);

        public override bool Equals(object? obj)
        {
            if (obj is VariableSymbol other) return Name == other.Name && Modifiers == other.Modifiers && Equals(Type, other.Type);
            else return false;
        }
    }
}
