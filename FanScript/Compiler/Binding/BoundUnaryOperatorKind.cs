using FanScript.Documentation.Attributes;

namespace FanScript.Compiler.Binding
{
    internal enum BoundUnaryOperatorKind
    {
        [UnaryOperatorDoc(
            Info = """
            Returns a.
            """
        )]
        Identity,
        [UnaryOperatorDoc(
            Info = """
            Returns the negation of a (5 -> -5, vec3(3, 0, -2) -> vec3(-3, 0, 2)).
            """
        )]
        Negation,
        [UnaryOperatorDoc(
            Info = """
            If a is true, returns false, otherwise returns true.
            """
        )]
        LogicalNegation,
    }
}
