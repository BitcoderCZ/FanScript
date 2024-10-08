using FanScript.FCInfo;
using System.Diagnostics;

namespace FanScript.Compiler.Symbols.Variables
{
    public sealed class ReservedCompilerVariableSymbol : CompilerVariableSymbol
    {
        public ReservedCompilerVariableSymbol(string identifier, string name, Modifiers modifiers, TypeSymbol type) : base(name, modifiers, type)
        {
            Debug.Assert(!string.IsNullOrEmpty(identifier) && identifier.Length + 2 <= FancadeConstants.MaxVariableNameLength);

            Identifier = identifier;
        }

        protected override string getNameForResult()
            => Identifier + "^" + Name;

        public string Identifier { get; }

        public override ReservedCompilerVariableSymbol Clone()
            => new ReservedCompilerVariableSymbol(Identifier, Name, Modifiers, Type);
    }
}
