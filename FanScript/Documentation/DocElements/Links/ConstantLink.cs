using System.Collections.Immutable;
using FanScript.Compiler;

namespace FanScript.Documentation.DocElements.Links
{
    public sealed class ConstantLink : DocLink
    {
        public ConstantLink(ImmutableArray<DocArg> arguments, DocString value, ConstantGroup group)
            : base(arguments, value)
        {
            Group = group;
        }

        public ConstantGroup Group { get; }

        public override (string DisplayString, string LinkString) GetStrings()
            => (Group.Name, Group.Name);
    }
}
