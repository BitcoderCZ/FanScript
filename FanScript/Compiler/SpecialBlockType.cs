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
        Button,
    }

    public static class SpecialBlockTypeE
    {
        private static readonly SpecialBlockCollection types = new SpecialBlockCollection()
        {
            new SpecialBlockTypeInfo(SpecialBlockType.Play, [], "Runs code only on the first frame"),
            new SpecialBlockTypeInfo(SpecialBlockType.LateUpdate, [], "Runs code after physics, before rendering"),
            new SpecialBlockTypeInfo(SpecialBlockType.BoxArt, [], "Runs code before taking a screenshot for the game cover"),
            new SpecialBlockTypeInfo(SpecialBlockType.Touch, [
                new SpecialBlockTypeParam("screenX", Modifiers.Ref, TypeSymbol.Float),
                new SpecialBlockTypeParam("screenY", Modifiers.Ref, TypeSymbol.Float),
                new SpecialBlockTypeParam("TOUCH_STATE", 0, TypeSymbol.Float, true),
                new SpecialBlockTypeParam("TOUCH_INDEX", 0, TypeSymbol.Float, true),
            ], "Runs code on touch"),
            new SpecialBlockTypeInfo(SpecialBlockType.Button, [
                new SpecialBlockTypeParam("BUTTON_TYPE", 0, TypeSymbol.Float, true),
            ], "Runs code when the button is pressed"),
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

                builder.Append(param.Name);
            }

            return builder
                .Append(')')
                .ToString();
        }
    }

    public record SpecialBlockTypeParam(string Name, Modifiers Modifiers, TypeSymbol Type, bool IsConstant = false)
    {
        public ParameterSymbol ToParameter(int ordinal)
            => new ParameterSymbol(Name, Modifiers, Type, ordinal);
    }
}
