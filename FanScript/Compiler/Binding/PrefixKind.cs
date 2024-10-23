using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    public enum PrefixKind
    {
        Increment,
        Decrement,
    }

    public static class PrefixKindE
    {
        public static string ToSyntaxString(this PrefixKind kind)
            => kind switch
            {
                PrefixKind.Increment => "++",
                PrefixKind.Decrement => "--",
                _ => throw new UnknownEnumValueException<PrefixKind>(kind),
            };

        public static SyntaxKind ToBinaryOp(this PrefixKind kind)
            => kind switch
            {
                PrefixKind.Increment => SyntaxKind.PlusToken,
                PrefixKind.Decrement => SyntaxKind.MinusToken,
                _ => throw new UnknownEnumValueException<PrefixKind>(kind),
            };
    }
}
