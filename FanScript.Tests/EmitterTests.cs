using FanScript.Compiler;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit.BlockPlacers;
using FanScript.Compiler.Emit.CodeBuilders;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System.Collections.Immutable;

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
                    float $[x]$ = 5
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
                $[)]$$[]$
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
                inspect($[)]$
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
                inspect(1$[, 2, 3]$)
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
                inspect(1$[$[=]$]$$[)]$
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
                    if $[10]$
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
                    while $[10]$
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
                $[x]$ * 10
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
                1 + $[]$
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
                $[+]$true
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
                10 $[*]$ false
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
                x $[+=]$ false
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
                $[x]$ = 10
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
                $[x]$ += 10
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
                    x $[=]$ 0
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
                    const float y = 10
                    x $[+=]$ 1
                    y $[+=]$ 1
                }
                """;

            var diagnostics = """
                Variable 'x' is read-only and cannot be assigned to.
                Variable 'y' is read-only and cannot be assigned to.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_AssignmentExpression_Reports_CannotConvert()
        {
            var text = """
                {
                    float x = 10
                    x = $[true]$
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
                $[foo]$(42)
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

        [Fact]
        public void Emitter_ExpressionStatement_Reports_Invalid()
        {
            var text = """
                $[random(0, 10)]$
                """;

            var diagnostics = """
                Only void call expressions can be used as a statement.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Theory]
        [InlineData("$[break]$", "break")]
        [InlineData("$[continue]$", "continue")]
        public void Emitter_Invalid_Break_Or_Continue(string text, string keyword)
        {
            var diagnostics = $"""
                The keyword '{keyword}' can only be used inside of loops.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Theory]
        [InlineData("break")]
        [InlineData("continue")]
        public void Emitter_Break_Or_Continue_In_SB(string keyword)
        {
            var text = $$"""
                float x
                while (x > 0)
                {
                    on Play()
                    {
                        $[{{keyword}}]$
                    }
                }
                """;

            var diagnostics = $"""
                The keyword '{keyword}' cannot be used inside of an on block.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Value_Must_Be_Constant()
        {
            var text = """
                float x
                object o = getObject($[x]$, 0, 0)
                """;

            var diagnostics = """
                Value must be constant.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Generic_Type_Recursion()
        {
            var text = """
                array<array$[<float>]$> x
                """;

            var diagnostics = """
                Type argument cannot be generic.
                """;

            AssertDiagnostics(text, diagnostics);
        }


        [Fact]
        public void Emitter_Not_A_Generic_Type()
        {
            var text = """
                float$[<float>]$ x
                """;

            var diagnostics = """
                Non generic type cannot have a type argument.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Type_Must_Have_Generic_Parameter()
        {
            var text = """
                $[array]$ x
                """;

            var diagnostics = """
                Type must have a type argument.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Non_Generic_Method_Type_Arguments()
        {
            var text = """
                float x = min$[<float>]$(10, 5)
                """;

            var diagnostics = """
                Non-generic methods cannot be used with a type argument.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Variable_Name_Too_Long()
        {
            string name = new string('x', FCInfo.FancadeConstants.MaxVariableNameLength + 1);

            var text = $"""
                float $[{name}]$
                """;

            var diagnostics = $"""
                Variable name '{name}' is too long, maximum allowed length is {FCInfo.FancadeConstants.MaxVariableNameLength}
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Empty_Array_Initializer()
        {
            var text = """
                array<float> x = $[[]]$
                """;

            var diagnostics = """
                Array initializer cannot be empty.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Theory]
        [MemberData(nameof(GetModifiersInvalidFor), ModifierTarget.Variable)]
        public void Emitter_Invalid_Modifier_Target_Variable(string modifier, string validTargets)
        {
            var text = $"""
                $[{modifier}]$ float x
                """;

            var diagnostics = $"""
                Modifier '{modifier}' was used on <{ModifierTarget.Variable}>, but it can only be used on <{validTargets}>.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Theory]
        [MemberData(nameof(GetModifiersValidFor), ModifierTarget.Variable)]
        public void Emitter_Duplicate_Modifier(string modifier)
        {
            var text = $"""
                {modifier} $[{modifier}]$ float x = 0
                """;

            var diagnostics = $"""
                Duplicate '{modifier}' modifier.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Invalid_Modifier_On_Type()
        {
            var text = $"""
                $[const]$ array<float> x
                """;

            var diagnostics = $"""
                Modifier 'const' isn't valid on a variable of type 'array<float>'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Constant_Not_Initialized()
        {
            var text = $"""
                const float $[x]$
                """;

            var diagnostics = $"""
                A constant variable needs to be initialized.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Theory]
        [MemberData(nameof(GetConflictingModifiersFor), ModifierTarget.Variable)]
        public void Emitter_Conflicting_Modifiers(string modifier1, string modifier2)
        {
            var text = $"""
                $[{modifier1}]$ {modifier2} float x = 0
                """;

            var diagnostics = $"""
                Modifier '{modifier1}' conflicts with modifier '{modifier2}'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Unknown_Special_Block()
        {
            var text = """
                on $[Foo]$()
                {
                }
                """;

            var diagnostics = $"""
                Unknown on block 'Foo'.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Argument_Must_Have_Modifier()
        {
            var text = """
                float x
                float y
                worldToScreen(vec3(0, 0, 0), $[x]$, out y)

                on Touch(out x, $[y]$, 0, 0)
                {
                }
                """;

            var diagnostics = $"""
                Argument for paramater 'screenX' must be passed with the 'out' modifier.
                Argument for paramater 'screenY' must be passed with the 'out' modifier.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_Argument_Cannot_Have_Modifier()
        {
            var text = """
                float y
                worldToScreen(vec3(0, 0, 0), out $[5]$, out y)
                """;

            var diagnostics = $"""
                A out argument must be an assignable variable.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_By_Ref_Arg_Must_Be_Variable()
        {
            var text = """
                float x
                inspect($[ref]$ x)
                inspect($[out]$ x)
                """;

            var diagnostics = $"""
                Argument for paramater 'value' cannot be passed with the 'ref' modifier.
                Argument for paramater 'value' cannot be passed with the 'out' modifier.
                """;

            AssertDiagnostics(text, diagnostics);
        }

        [Fact]
        public void Emitter_SB_Must_Have_Arguments()
        {
            var text = """
                on $[Touch]$
                {
                }
                """;

            var diagnostics = $"""
                On block 'Touch' must have arguments.
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

        public static IEnumerable<object[]> GetModifiersValidFor(ModifierTarget validTarget)
        {
            foreach (var mod in Enum.GetValues<Modifiers>())
            {
                var targets = mod.GetTargets();
                if (targets.Contains(validTarget))
                    yield return [mod.ToSyntaxString()];
            }
        }
        public static IEnumerable<object[]> GetModifiersInvalidFor(ModifierTarget invalidTarget)
        {
            foreach (var mod in Enum.GetValues<Modifiers>())
            {
                var targets = mod.GetTargets();
                if (!targets.Contains(invalidTarget))
                    yield return [mod.ToSyntaxString(), string.Join(", ", targets)];
            }
        }

        public static IEnumerable<object[]> GetConflictingModifiersFor(ModifierTarget validTarget)
        {
            foreach (var mod1 in Enum.GetValues<Modifiers>())
            {
                var mod1Targets = mod1.GetTargets();
                if (mod1Targets.Contains(validTarget))
                {
                    foreach (var mod2 in Enum.GetValues<Modifiers>())
                        if (mod1.GetConflictingModifiers().Contains(mod2) && mod2.GetTargets().Contains(validTarget))
                            yield return [mod1.ToSyntaxString(), mod2.ToSyntaxString()];
                }
            }
        }
    }
}
