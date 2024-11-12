using FanScript.Documentation.Attributes;

namespace FanScript.Compiler.Binding;

internal enum BoundBinaryOperatorKind
{
    [BinaryOperatorDoc(
        Info = """
        Adds the value of a and b.
        """)]
    Addition,
    [BinaryOperatorDoc(
        Info = """
        Substracts b from a.
        """)]
    Subtraction,
    [BinaryOperatorDoc(
        Info = """
        Multiplies a and b.
        """)]
    Multiplication,
    [BinaryOperatorDoc(
        Info = """
        Divides a by b.
        """)]
    Division,
    [BinaryOperatorDoc(
        Info = """
        Returns the remainder of division of a and b.
        """)]
    Modulo,
    [BinaryOperatorDoc(
        Info = """
        Returns true, only if both a and b are true.
        """)]
    LogicalAnd,
    [BinaryOperatorDoc(
        Info = """
        Returns true, if a or b are true.
        """)]
    LogicalOr,
    [BinaryOperatorDoc(
        Info = """
        Returns if a and b have the same value.
        """)]
    Equals,
    [BinaryOperatorDoc(
        Info = """
        Returns if the value of a is not equal to the value of b.
        """)]
    NotEquals,
    [BinaryOperatorDoc(
        Info = """
        Returns if a is less than b.
        """)]
    Less,
    [BinaryOperatorDoc(
        Info = """
        Returns if a is less than b or equal to b.
        """)]
    LessOrEquals,
    [BinaryOperatorDoc(
        Info = """
        Returns if a is greater than b.
        """)]
    Greater,
    [BinaryOperatorDoc(
        Info = """
        Returns if a is greater than b or equal to b.
        """)]
    GreaterOrEquals,
}
