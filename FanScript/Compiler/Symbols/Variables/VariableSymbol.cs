using FanScript.Compiler.Binding;
using FanScript.FCInfo;
using System.Diagnostics;

namespace FanScript.Compiler.Symbols.Variables
{
    public abstract class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name)
        {
            Debug.Assert(!modifiers.HasFlag(Modifiers.Global) || !modifiers.HasFlag(Modifiers.Saved)); // cant have both global and saved

            Modifiers = modifiers;
            Type = type;
        }

        public bool IsReadOnly => Modifiers.HasFlag(Modifiers.Readonly) || Modifiers.HasFlag(Modifiers.Constant);
        public bool IsGlobal => Modifiers.HasFlag(Modifiers.Global) || Modifiers.HasFlag(Modifiers.Saved);
        public Modifiers Modifiers { get; private set; }
        public TypeSymbol Type { get; private set; }
        internal BoundConstant? Constant { get; private set; }

        public string ResultName
        {
            get
            {
                string preChar = string.Empty;
                if (Modifiers.HasFlag(Modifiers.Saved))
                    preChar = "!";
                else if (Modifiers.HasFlag(Modifiers.Global))
                    preChar = "$";

                string name = getNameForResult();
                return string.Concat(preChar, Modifiers.HasFlag(Modifiers.Constant) ? name : name.AsSpan(0, Math.Min(name.Length, FancadeConstants.MaxVariableNameLength - preChar.Length)));
            }
        }
        protected virtual string getNameForResult()
            => Name;

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

        public virtual VariableSymbol Clone()
            => new BasicVariableSymbol(Name, Modifiers, Type);

        public override int GetHashCode()
            => HashCode.Combine(ResultName, Modifiers, Type);

        public override bool Equals(object? obj)
        {
            if (obj is VariableSymbol other) return ResultName == other.ResultName && Modifiers == other.Modifiers && Equals(Type, other.Type);
            else return false;
        }
    }
}
