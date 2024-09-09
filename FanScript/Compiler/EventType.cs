using FanScript.Compiler.Symbols;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;

namespace FanScript.Compiler
{
    public enum EventType
    {
        Play,
        LateUpdate,
        BoxArt,
        Touch,
        Swipe,
        Button,
        Collision,
        Loop,
    }

    public static class EventTypeE
    {
        private static readonly EventCollection types = new EventCollection()
        {
            new EventTypeInfo(EventType.Play, [], "Runs only on the first frame"),
            new EventTypeInfo(EventType.LateUpdate, [], "Runs after physics, before rendering"),
            new EventTypeInfo(EventType.BoxArt, [], "Runs before taking a screenshot for the game cover"),
            new EventTypeInfo(EventType.Touch, [
                new EventTypeParam("screenX", Modifiers.Out, TypeSymbol.Float),
                new EventTypeParam("screenY", Modifiers.Out, TypeSymbol.Float),
                new EventTypeParam("TOUCH_STATE", 0, TypeSymbol.Float, true),
                new EventTypeParam("TOUCH_FINGER", 0, TypeSymbol.Float, true),
            ], "Runs on touch"),
            new EventTypeInfo(EventType.Swipe, [
                new EventTypeParam("direction", Modifiers.Out, TypeSymbol.Vector3),
            ], "Runs on swipe"),
            new EventTypeInfo(EventType.Button, [
                new EventTypeParam("BUTTON_TYPE", 0, TypeSymbol.Float, true),
            ], "Runs when the button is pressed"),
            new EventTypeInfo(EventType.Collision, [
                new EventTypeParam("object1", 0, TypeSymbol.Object),
                new EventTypeParam("object2", Modifiers.Out, TypeSymbol.Object),
                new EventTypeParam("impulse", Modifiers.Out, TypeSymbol.Float),
                new EventTypeParam("normal", Modifiers.Out, TypeSymbol.Vector3),
            ], "Runs when 2 objects collide"),
            new EventTypeInfo(EventType.Loop, [
                new EventTypeParam("start", 0, TypeSymbol.Float),
                new EventTypeParam("stop", 0, TypeSymbol.Float),
                new EventTypeParam("counter", Modifiers.Out, TypeSymbol.Float),
            ], "Runs multiple times from start to end (start: 2, end: 5, counter: [2, 3, 4])"),
        };

        public static EventTypeInfo GetInfo(this EventType sbt)
            => types[sbt];

        private sealed class EventCollection : KeyedCollection<EventType, EventTypeInfo>
        {
            protected override EventType GetKeyForItem(EventTypeInfo item)
                => item.Type;
        }
    }

    public record EventTypeInfo(EventType Type, ImmutableArray<EventTypeParam> Parameters, string? Description = null)
    {
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder()
                .Append("on ")
                .Append(Type)
                .Append('(');

            for (int i = 0; i < Parameters.Length; i++)
            {
                var param = Parameters[i];

                if (i != 0)
                    builder.Append(", ");

                if (param.Modifiers != 0)
                {
                    param.Modifiers.ToSyntaxString(builder);
                    builder.Append(' ');
                }

                builder.Append(param.Type.ToString());
                builder.Append(' ');
                builder.Append(param.Name);
            }

            return builder
                .Append(')')
                .ToString();
        }
    }

    public record EventTypeParam(string Name, Modifiers Modifiers, TypeSymbol Type, bool IsConstant = false)
    {
        public ParameterSymbol ToParameter()
            => new ParameterSymbol(Name, Modifiers, Type);
    }
}
