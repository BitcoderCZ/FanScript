using FanScript.Compiler;
using FanScript.Utils;
using System.Collections.Immutable;

namespace FanScript.Documentation.DocElements.Links
{
    public sealed class EventLink : DocLink
    {
        public EventLink(ImmutableArray<DocArg> arguments, DocString value, EventType @event)
            : base(arguments, value)
        {
            Event = @event;
        }

        public EventType Event { get; }

        public override (string DisplayString, string LinkString) GetStrings()
        {
            string eventName = Enum.GetName(Event)!;

            return (eventName.ToLowerFirst(), eventName);
        }
    }
}
