using FanScript.Compiler.Symbols;
using System.Collections.Immutable;
using System.Text;

namespace FanScript.Documentation.DocElements
{
    public abstract class DocLink : DocElement
    {
        protected DocLink(ImmutableArray<DocArg> arguments, DocString value) : base(arguments, value)
        {
        }

        public abstract (string DisplayString, string LinkString) GetStrings();
    }

    public sealed class FunctionLink : DocLink
    {
        public FunctionLink(FunctionSymbol function)
            : base(ImmutableArray<DocArg>.Empty, new DocString(function.Name))
        {
            Function = function;
        }

        public FunctionSymbol Function { get; }

        public override (string DisplayString, string LinkString) GetStrings()
        {
            StringBuilder displayBuilder = new();
            StringBuilder linkBuilder = new();

            displayBuilder.Append(Function.Name);
            linkBuilder.Append(Function.Name);
            if (Function.IsGeneric)
                displayBuilder.Append("<>");

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
