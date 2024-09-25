using FanScript.Compiler.Binding;
using FanScript.FCInfo;
using System.Diagnostics;

namespace FanScript.Compiler.Symbols
{
    public abstract class VariableSymbol : Symbol
    {
        internal string? UniqueId;

        internal VariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name)
        {
            Modifiers = modifiers;
            Type = type;
        }

        public Modifiers Modifiers { get; private set; }
        public bool IsReadOnly => Modifiers.HasFlag(Modifiers.Readonly) || Modifiers.HasFlag(Modifiers.Constant);
        public TypeSymbol Type { get; private set; }
        internal BoundConstant? Constant { get; private set; }

        public virtual string ResultName
        {
            get
            {
                string preChar = string.Empty;
                if (Modifiers.HasFlag(Modifiers.Saved))
                    preChar = "!";
                else if (Modifiers.HasFlag(Modifiers.Global))
                    preChar = "$";

                if (UniqueId is null)
                    return string.Concat(preChar, Name.AsSpan(0, Math.Min(Name.Length, FancadeConstants.MaxVariableNameLength - preChar.Length)));

                Debug.Assert(UniqueId.Length <= FancadeConstants.MaxVariableNameLength - 2 - preChar.Length);

                return string.Concat(preChar, "^", Name.AsSpan(0, Math.Min(Name.Length, FancadeConstants.MaxVariableNameLength - (UniqueId.Length + 2 + preChar.Length))), UniqueId);
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
            => UniqueId = id.ToString();

        public VariableSymbol Clone()
            => new LocalVariableSymbol(Name, Modifiers, Type)
            {
                UniqueId = UniqueId,
            };

        public override int GetHashCode()
            => HashCode.Combine(ResultName, Modifiers, Type, UniqueId);

        public override bool Equals(object? obj)
        {
            if (obj is VariableSymbol other) return ResultName == other.ResultName && Modifiers == other.Modifiers && Equals(Type, other.Type) && UniqueId == other.UniqueId;
            else return false;
        }
    }
}
