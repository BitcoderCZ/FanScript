using FanScript.Compiler.Symbols;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;

namespace FanScript.Compiler
{
    public enum SpecialBlockType
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

    public static class SpecialBlockTypeE
    {
        private static readonly SpecialBlockCollection types = new SpecialBlockCollection()
        {
            new SpecialBlockTypeInfo(SpecialBlockType.Play, [], "Runs only on the first frame"),
            new SpecialBlockTypeInfo(SpecialBlockType.LateUpdate, [], "Runs after physics, before rendering"),
            new SpecialBlockTypeInfo(SpecialBlockType.BoxArt, [], "Runs before taking a screenshot for the game cover"),
            new SpecialBlockTypeInfo(SpecialBlockType.Touch, [
                new SpecialBlockTypeParam("screenX", Modifiers.Out, TypeSymbol.Float),
                new SpecialBlockTypeParam("screenY", Modifiers.Out, TypeSymbol.Float),
                new SpecialBlockTypeParam("TOUCH_STATE", 0, TypeSymbol.Float, true),
                new SpecialBlockTypeParam("TOUCH_FINGER", 0, TypeSymbol.Float, true),
            ], "Runs on touch"),
            new SpecialBlockTypeInfo(SpecialBlockType.Swipe, [
                new SpecialBlockTypeParam("direction", Modifiers.Out, TypeSymbol.Vector3),
            ], "Runs on swipe"),
            new SpecialBlockTypeInfo(SpecialBlockType.Button, [
                new SpecialBlockTypeParam("BUTTON_TYPE", 0, TypeSymbol.Float, true),
            ], "Runs when the button is pressed"),
            new SpecialBlockTypeInfo(SpecialBlockType.Collision, [
                new SpecialBlockTypeParam("object1", 0, TypeSymbol.Object),
                new SpecialBlockTypeParam("object2", Modifiers.Out, TypeSymbol.Object),
                new SpecialBlockTypeParam("impulse", Modifiers.Out, TypeSymbol.Float),
                new SpecialBlockTypeParam("normal", Modifiers.Out, TypeSymbol.Vector3),
            ], "Runs when 2 objects collide"),
            new SpecialBlockTypeInfo(SpecialBlockType.Loop, [
                new SpecialBlockTypeParam("start", 0, TypeSymbol.Float),
                new SpecialBlockTypeParam("stop", 0, TypeSymbol.Float),
                new SpecialBlockTypeParam("counter", Modifiers.Out, TypeSymbol.Float),
            ], "Runs multiple times from start to end (start: 2, end: 5, counter: [2, 3, 4])"),
        };

        public static SpecialBlockTypeInfo GetInfo(this SpecialBlockType sbt)
            => types[sbt];

        private sealed class SpecialBlockCollection : KeyedCollection<SpecialBlockType, SpecialBlockTypeInfo>
        {
            protected override SpecialBlockType GetKeyForItem(SpecialBlockTypeInfo item)
                => item.Type;
        }
    }

    public record SpecialBlockTypeInfo(SpecialBlockType Type, ImmutableArray<SpecialBlockTypeParam> Parameters, string? Description = null)
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

    public record SpecialBlockTypeParam(string Name, Modifiers Modifiers, TypeSymbol Type, bool IsConstant = false)
    {
        public ParameterSymbol ToParameter()
            => new ParameterSymbol(Name, Modifiers, Type);
    }
}
