using FanScript.Compiler;
using FanScript.Utils;
using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements.Links
{
    public sealed class BuildCommandLink : DocLink
    {
        public BuildCommandLink(ImmutableArray<DocArg> arguments, DocString value, BuildCommand command)
            : base(arguments, value)
        {
            Command = command;
        }

        public BuildCommand Command { get; }

        public override (string DisplayString, string LinkString) GetStrings()
        {
            string commandName = Enum.GetName(Command)!;
            return (commandName.ToLowerFirst(), commandName);
        }
    }
}
