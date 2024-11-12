using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Syntax;

namespace FanScript.Compiler.Binding;

public enum PostfixKind
{
    Increment,
    Decrement,
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "It does, but enums aren't detected for some reason")]
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
