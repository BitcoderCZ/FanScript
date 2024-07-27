using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding
{
    public enum BoundPostfixKind
    {
        Increment,
        Decrement,
    }

    public static class BoundPostfixKindE
    {
        public static string ToSyntaxString(this BoundPostfixKind kind)
            => kind switch
            {
                BoundPostfixKind.Increment => "++",
                BoundPostfixKind.Decrement => "--",
                _ => throw new InvalidDataException($"Unknown {nameof(BoundPostfixKind)} '{kind}'"),
            };

        public static SyntaxKind ToBinaryOp(this BoundPostfixKind kind)
            => kind switch
            {
                BoundPostfixKind.Increment => SyntaxKind.PlusToken,
                BoundPostfixKind.Decrement => SyntaxKind.MinusToken,
                _ => throw new InvalidDataException($"Unknown {nameof(BoundPostfixKind)} '{kind}'"),
            };
    }
}
