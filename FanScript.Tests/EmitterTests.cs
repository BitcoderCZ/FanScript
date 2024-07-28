using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.BlockPlacers;
using FanScript.Compiler.Emit.CodeBuilders;
using System.Collections.Immutable;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Text;

namespace FanScript.Tests
{
    public class EmitterTests
    {
        [Fact]
        public void Emitter_VariableDeclaration_Reports_Redeclaration()
        {
            var text = """
                {
                    float x = 10
                    float y = 100
                    float [x] = 5
                }
                """;

            var diagnostics = """
                'x' is already declared.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_BlockStatement_NoInfiniteLoop()
        {
            var text = """
                {
                [)][]
                """;

            var diagnostics = """
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
                Unexpected token <EndOfFileToken>, expected <CloseBraceToken>.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_InvokeFunctionArguments_Missing()
        {
            var text = """
                inspect([)]
                """;

            var diagnostics = """
                Function 'inspect' requires 1 arguments but was given 0.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_InvokeFunctionArguments_Exceeding()
        {
            var text = """
                inspect(1[, 2, 3])
                """;

            var diagnostics = """
                Function 'inspect' requires 1 arguments but was given 3.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_InvokeFunctionArguments_NoInfiniteLoop()
        {
            var text = @"
                inspect(1[[=]][)]
            ";

            var diagnostics = """
                Unexpected token <EqualsToken>, expected <CloseParenthesisToken>.
                Unexpected token <EqualsToken>, expected <IdentifierToken>.
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        //[Fact]
        //public void Emitter_FunctionParameters_NoInfiniteLoop()
        //{
        //    var text = @"
        //        function hi(name: string[[[=]]][)]
        //        {
        //            print(""Hi "" + name + ""!"" )
        //        }[]
        //    ";

        //    var diagnostics = @"
        //        Unexpected token <EqualsToken>, expected <CloseParenthesisToken>.
        //        Unexpected token <EqualsToken>, expected <OpenBraceToken>.
        //        Unexpected token <EqualsToken>, expected <IdentifierToken>.
        //        Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
        //        Unexpected token <EndOfFileToken>, expected <CloseBraceToken>.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_FunctionReturn_Missing()
        //{
        //    var text = @"
        //        function [add](a: int, b: int): int
        //        {
        //        }
        //    ";

        //    var diagnostics = @"
        //        Not all code paths return a value.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        [Fact]
        public void Emitter_IfStatement_Reports_CannotConvert()
        {
            var text = """
                {
                    float x = 0
                    if [10]
                        x = 10
                }
                """;

            var diagnostics = """
                Cannot convert type 'float' to 'bool'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_WhileStatement_Reports_CannotConvert()
        {
            var text = """
                {
                    float x = 0
                    while [10]
                        x = 10
                }
                """;

            var diagnostics = """
                Cannot convert type 'float' to 'bool'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        //[Fact]
        //public void Emitter_DoWhileStatement_Reports_CannotConvert()
        //{
        //    var text = @"
        //        {
        //            var x = 0
        //            do
        //                x = 10
        //            while [10]
        //        }
        //    ";

        //    var diagnostics = @"
        //        Cannot convert type 'int' to 'bool'.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_ForStatement_Reports_CannotConvert_LowerBound()
        //{
        //    var text = @"
        //        {
        //            var result = 0
        //            for i = [false] to 10
        //                result = result + i
        //        }
        //    ";

        //    var diagnostics = @"
        //        Cannot convert type 'bool' to 'int'.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_ForStatement_Reports_CannotConvert_UpperBound()
        //{
        //    var text = @"
        //        {
        //            var result = 0
        //            for i = 1 to [true]
        //                result = result + i
        //        }
        //    ";

        //    var diagnostics = @"
        //        Cannot convert type 'bool' to 'int'.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        [Fact]
        public void Emitter_NameExpression_Reports_Undefined()
        {
            var text = """
                [x] * 10
                """;

            var diagnostics = """
                Variable 'x' doesn't exist.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_NameExpression_Reports_NoErrorForInsertedToken()
        {
            var text = """
                1 + []
                """;

            var diagnostics = """
                Unexpected token <EndOfFileToken>, expected <IdentifierToken>.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_UnaryExpression_Reports_Undefined()
        {
            var text = """
                [+]true
                """;

            var diagnostics = """
                Unary operator '+' is not defined for type 'bool'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_BinaryExpression_Reports_Undefined()
        {
            var text = """
                10 [*] false
                """;

            var diagnostics = """
                Binary operator '*' is not defined for types 'float' and 'bool'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_CompoundExpression_Reports_Undefined()
        {
            var text = """
                float x = 10
                x [+=] false
                """;

            var diagnostics = """
                Binary operator '+=' is not defined for types 'float' and 'bool'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_AssignmentExpression_Reports_Undefined()
        {
            var text = """
                [x] = 10
                """;

            var diagnostics = """
                Variable 'x' doesn't exist.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_CompoundExpression_Assignemnt_NonDefinedVariable_Reports_Undefined()
        {
            var text = """
                [x] += 10
                """;

            var diagnostics = """
                Variable 'x' doesn't exist.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_AssignmentExpression_Reports_CannotAssign()
        {
            var text = """
                {
                    readonly float x = 10
                    x [=] 0
                }
                """;

            var diagnostics = """
                Variable 'x' is read-only and cannot be assigned to.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_CompoundDeclarationExpression_Reports_CannotAssign()
        {
            var text = """
                {
                    readonly float x = 10
                    x [+=] 1
                }
                """;

            var diagnostics = """
                Variable 'x' is read-only and cannot be assigned to.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_AssignmentExpression_Reports_CannotConvert()
        {
            var text = """
                {
                    float x = 10
                    x = [true]
                }
                """;

            var diagnostics = """
                Cannot convert type 'bool' to 'float'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_CallExpression_Reports_Undefined()
        {
            var text = """
                [foo](42)
                """;

            var diagnostics = """
                Function 'foo' doesn't exist.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        //[Fact]
        //public void Emitter_Void_Function_Should_Not_Return_Value()
        //{
        //    var text = @"
        //        function test()
        //        {
        //            return [1]
        //        }
        //    ";

        //    var diagnostics = @"
        //        Since the function 'test' does not return a value the 'return' keyword cannot be followed by an expression.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_Function_With_ReturnValue_Should_Not_Return_Void()
        //{
        //    var text = @"
        //        function test(): int
        //        {
        //            [return]
        //        }
        //    ";

        //    var diagnostics = @"
        //        An expression of type 'int' is expected.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_Not_All_Code_Paths_Return_Value()
        //{
        //    var text = @"
        //        function [test](n: int): bool
        //        {
        //            if (n > 10)
        //               return true
        //        }
        //    ";

        //    var diagnostics = @"
        //        Not all code paths return a value.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_Expression_Must_Have_Value()
        //{
        //    var text = @"
        //        function test(n: int)
        //        {
        //            return
        //        }

        //        let value = [test(100)]
        //    ";

        //    var diagnostics = @"
        //        Expression must have a value.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_IfStatement_Reports_NotReachableCode_Warning()
        //{
        //    var text = @"
        //        function test()
        //        {
        //            let x = 4 * 3
        //            if x > 12
        //            {
        //                [print](""x"")
        //            }
        //            else
        //            {
        //                print(""x"")
        //            }
        //        }
        //    ";

        //    var diagnostics = @"
        //        Unreachable code detected.
        //    ";
        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_ElseStatement_Reports_NotReachableCode_Warning()
        //{
        //    var text = @"
        //        function test(): int
        //        {
        //            if true
        //            {
        //                return 1
        //            }
        //            else
        //            {
        //                [return] 0
        //            }
        //        }
        //    ";

        //    var diagnostics = @"
        //        Unreachable code detected.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_WhileStatement_Reports_NotReachableCode_Warning()
        //{
        //    var text = @"
        //        function test()
        //        {
        //            while false
        //            {
        //                [continue]
        //            }
        //        }
        //    ";

        //    var diagnostics = @"
        //        Unreachable code detected.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        [Theory]
        [InlineData("[break]", "break")]
        [InlineData("[continue]", "continue")]
        public void Emitter_Invalid_Break_Or_Continue(string text, string keyword)
        {
            var diagnostics = $"""
                The keyword '{keyword}' can only be used inside of loops.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        //[Fact]
        //public void Emitter_Parameter_Already_Declared()
        //{
        //    var text = @"
        //        function sum(a: int, b: int, [a: int]): int
        //        {
        //            return a + b + c
        //        }
        //    ";

        //    var diagnostics = @"
        //        A parameter with the name 'a' already exists.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_Function_Must_Have_Name()
        //{
        //    var text = @"
        //        function [(]a: int, b: int): int
        //        {
        //            return a + b
        //        }
        //    ";

        //    var diagnostics = @"
        //        Unexpected token <OpenParenthesisToken>, expected <IdentifierToken>.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_Wrong_Argument_Type()
        //{
        //    var text = @"
        //        function test(n: int): bool
        //        {
        //            return n > 10
        //        }
        //        let testValue = ""string""
        //        test([testValue])
        //    ";

        //    var diagnostics = @"
        //        Cannot convert type 'string' to 'int'. An explicit conversion exists (are you missing a cast?)
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        //[Fact]
        //public void Emitter_Bad_Type()
        //{
        //    var text = @"
        //        function test(n: [invalidtype])
        //        {
        //        }
        //    ";

        //    var diagnostics = @"
        //        Type 'invalidtype' doesn't exist.
        //    ";

        //    AssertDiagnostics(text, diagnostics);
        //}

        private void AssertDiagnostics(string text, string diagnosticText)
        {
            AnnotatedText annotatedText = AnnotatedText.Parse(text);
            SyntaxTree syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            Compilation compilation = Compilation.CreateScript(null, syntaxTree);
            ImmutableArray<Diagnostic> diagnostics = compilation.Emit(new EditorScriptCodeBuilder(new TowerBlockPlacer()));

            string[] expectedDiagnostics = AnnotatedText.UnindentLines(diagnosticText);

            if (annotatedText.Spans.Length != expectedDiagnostics.Length)
                throw new Exception("ERROR: Must mark as many spans as there are expected diagnostics");

            Assert.Equal(expectedDiagnostics.Length, diagnostics.Length);

            for (var i = 0; i < expectedDiagnostics.Length; i++)
            {
                string expectedMessage = expectedDiagnostics[i];
                string actualMessage = diagnostics[i].Message;
                Assert.Equal(expectedMessage, actualMessage);

                TextSpan expectedSpan = annotatedText.Spans[i];
                TextSpan actualSpan = diagnostics[i].Location.Span;
                Assert.Equal(expectedSpan, actualSpan);
            }
        }
    }
}
