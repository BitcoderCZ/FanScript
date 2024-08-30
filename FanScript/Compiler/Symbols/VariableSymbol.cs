using FanScript.Compiler.Binding;
using FanScript.FCInfo;
using System.Diagnostics;

namespace FanScript.Compiler.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        private string? uniqueId;

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

        public virtual string ResultName
        {
            get
            {
                if (uniqueId is null)
                    return Name;

                Debug.Assert(uniqueId.Length <= FancadeConstants.MaxVariableNameLength - 2);

                return "^" + Name.Substring(0, Math.Min(Name.Length, FancadeConstants.MaxVariableNameLength - (uniqueId.Length + 2))) + uniqueId;
            }
        }

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

        internal void MakeUnique(int id)
            => uniqueId = id.ToString();

        public override int GetHashCode()
            => HashCode.Combine(ResultName, Modifiers, Type);

        public override bool Equals(object? obj)
        {
            if (obj is VariableSymbol other) return ResultName == other.ResultName && Modifiers == other.Modifiers && Equals(Type, other.Type);
            else return false;
        }
    }
}
