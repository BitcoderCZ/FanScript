using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    public enum PostfixKind
    {
        Increment,
        Decrement,
    }

    public static class PostfixKindE
    {
        public static string ToSyntaxString(this PostfixKind kind)
            => kind switch
            {
                PostfixKind.Increment => "++",
                PostfixKind.Decrement => "--",
                _ => throw new UnknownEnumValueException<PostfixKind>(kind),
            };

        public static SyntaxKind ToBinaryOp(this PostfixKind kind)
            => kind switch
            {
                PostfixKind.Increment => SyntaxKind.PlusToken,
                PostfixKind.Decrement => SyntaxKind.MinusToken,
                _ => throw new UnknownEnumValueException<PostfixKind>(kind),
            };
    }
}
