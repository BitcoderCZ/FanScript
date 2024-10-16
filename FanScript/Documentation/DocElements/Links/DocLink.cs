using System.Collections.Immutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FanScript.Documentation.DocElements.Links
{
    public abstract class DocLink : DocElement
    {
        protected DocLink(ImmutableArray<DocArg> arguments, DocString value) : base(arguments, value)
        {
        }

        public abstract (string DisplayString, string LinkString) GetStrings();
    }
}
