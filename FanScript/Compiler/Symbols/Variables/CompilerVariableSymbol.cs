namespace FanScript.Compiler.Symbols.Variables
{
    public class CompilerVariableSymbol : BasicVariableSymbol
    {
        public CompilerVariableSymbol(string name, Modifiers modifiers, TypeSymbol type)
            : base(name, modifiers, type)
        {
        }

        protected override string GetNameForResult()
            => "^" + Name;
    }
}
