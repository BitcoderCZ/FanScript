using System.Diagnostics;
using FanScript.Compiler.Binding;
using FanScript.FCInfo;
using FanScript.Utils;

namespace FanScript.Compiler.Symbols.Variables
{
    public abstract class VariableSymbol : Symbol
    {
        internal VariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name)
        {
            Debug.Assert(!modifiers.HasFlag(Modifiers.Global) || !modifiers.HasFlag(Modifiers.Saved), "A variable cannot have both global and saved modifiers.");

            Modifiers = modifiers;
            Type = type;
        }

        public bool IsReadOnly => Modifiers.HasFlag(Modifiers.Readonly) || Modifiers.HasFlag(Modifiers.Constant);

        public bool IsGlobal => Modifiers.HasFlag(Modifiers.Global) || Modifiers.HasFlag(Modifiers.Saved);

        public Modifiers Modifiers { get; private set; }

        public TypeSymbol Type { get; private set; }

        /// <summary>
        /// Used to know if a Read-Only variable has been assigned
        /// </summary>
        public bool Initialized { get; private set; }

        public string ResultName
        {
            get
            {
                string preChar = string.Empty;
                if (Modifiers.HasFlag(Modifiers.Saved))
                {
                    preChar = "!";
                }
                else if (Modifiers.HasFlag(Modifiers.Global))
                {
                    preChar = "$";
                }

                string name = GetNameForResult();
                return string.Concat(preChar, Modifiers.HasFlag(Modifiers.Constant) ? name : name.AsSpan(0, Math.Min(name.Length, FancadeConstants.MaxVariableNameLength - preChar.Length)));
            }
        }

        internal BoundConstant? Constant { get; private set; }

        public virtual VariableSymbol Clone()
            => new BasicVariableSymbol(Name, Modifiers, Type);

        public override void WriteTo(TextWriter writer)
        {
            writer.WriteWritable(Type);
            writer.WriteSpace();
            writer.WriteIdentifier(Name);
        }

        public override int GetHashCode()
            => HashCode.Combine(ResultName, Modifiers, Type);

        public override bool Equals(object? obj)
            => obj is VariableSymbol other && ResultName == other.ResultName && Modifiers == other.Modifiers && Equals(Type, other.Type);

        internal void Initialize(BoundConstant? value)
        {
            if (Initialized)
            {
                return;
            }

            Constant = Modifiers.HasFlag(Modifiers.Constant) ? value : null;
            Initialized = true;
        }

        protected virtual string GetNameForResult()
            => Name;
    }
}
