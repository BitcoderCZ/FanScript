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

        public static ReservedCompilerVariableSymbol CreateParam(FunctionSymbol func, int paramIndex)
            => new ReservedCompilerVariableSymbol("func" + func.Id.ToString(), paramIndex.ToString(), func.Parameters[paramIndex].Modifiers, func.Parameters[paramIndex].Type);

        public static ReservedCompilerVariableSymbol CreateFunctionRes(FunctionSymbol func, bool inlineFunc = false)
            => new ReservedCompilerVariableSymbol("func" + func.Id.ToString(), "res", inlineFunc ? Modifiers.Inline : 0, func.Type);

        public static ReservedCompilerVariableSymbol CreateDiscard(TypeSymbol type)
            => new ReservedCompilerVariableSymbol("discard", string.Empty, 0, type);
    }
}
