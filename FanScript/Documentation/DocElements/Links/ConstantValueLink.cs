using System.Collections.Immutable;
using FanScript.Compiler;

namespace FanScript.Documentation.DocElements.Links
{
    public sealed class ConstantValueLink : DocLink
    {
        public ConstantValueLink(ImmutableArray<DocArg> arguments, DocString value, ConstantGroup group, Constant constant)
            : base(arguments, value)
        {
            Group = group;
            Constant = constant;
        }

        public ConstantGroup Group { get; }

        public Constant Constant { get; }

        public override (string DisplayString, string LinkString) GetStrings()
            => (Group.Name + "_" + Constant.Name, Group.Name);
    }
}
