using System.Collections.Immutable;
using System.Text;
using FanScript.Compiler.Symbols.Functions;

namespace FanScript.Documentation.DocElements.Links
{
    public sealed class FunctionLink : DocLink
    {
        public FunctionLink(ImmutableArray<DocArg> arguments, DocString value, FunctionSymbol function)
            : base(arguments, value)
        {
            Function = function;
        }

        public FunctionSymbol Function { get; }

        public override (string DisplayString, string LinkString) GetStrings()
        {
            StringBuilder displayBuilder = new();
            StringBuilder linkBuilder = new();

            displayBuilder.Append(Function.Name);
            linkBuilder.Append(Function.Namespace + Function.Name);
            if (Function.IsGeneric)
            {
                displayBuilder.Append("<>");
            }

            displayBuilder.Append('(');
            linkBuilder.Append('.');
            for (int i = 0; i < Function.Parameters.Length; i++)
            {
                if (i != 0)
                {
                    displayBuilder.Append(", ");
                    linkBuilder.Append('.');
                }

                displayBuilder.Append(Function.Parameters[i].Type.ToString());
                linkBuilder.Append(Function.Parameters[i].Type.Name);
            }

            displayBuilder.Append(')');

            return (displayBuilder.ToString(), linkBuilder.ToString());
        }
    }
}
