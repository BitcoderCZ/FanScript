using FanScript.Compiler;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Emit;
using FanScript.Compiler.Emit.CodePlacers;
using FanScript.Compiler.Emit.BlockBuilders;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace FanScript.Tests;

public class EmitterTests
{
    [Theory]
    [MemberData(nameof(GetVariableDeclarations))]
    [InlineData("on Play { }")]
    [InlineData("on Play() { }")]
    [InlineData("""
        float x
        float y
        on Touch(out x, out y, 0, 0) { }
        """)]
    [InlineData("""
        float i = 0
        while (i < 10)
            i++
        """)]
    [InlineData("""
        float i = 0
        do
        {
            i++
        } while (i < 10)
        """)]
    // code is reachable
    [InlineData("""
        float i = 0
        do
        {
            i++
        } while (false)
        """)]
    [InlineData("on Touch(out float x, out float y, 0, 0) { }")]
    [InlineData("""
        float x = 1
        inspect(x)
        """)]
    [InlineData("""
        float x = 1
        inspect<float>(x)
        """)]
    [InlineData("""
        vec3 v = vec3(1, 2, 3)
        float x = v.y
        """)]
    [InlineData("""
        float x = vec3(1, 2, 3).y
        """)]
    [InlineData("""
        vec3 v
        v.x = 1
        v.y = 2
        v.z = 3
        """)]
    [InlineData("""
        raycast(null, vec3(0, 0, 10), out bool didHit, out vec3 pos, out obj hitObj)
        """)]
    [InlineData("""
        raycast(null, vec3(0, 0, 10), out _, out _, out _)
        """)]
    [InlineData("""
        array<float> arr
        arr.setRange(0, [1, 2, 3])
        float first = arr.get(0)
        """)]
    public void NoDiagnostics(string text)
        => AssertDiagnostics(text, string.Empty);

    [Fact]
    public void VariableDeclaration_Reports_Redeclaration()
    {
        string text = """
            {
                float x = 10
                float y = 100
                float $[x]$ = 5
            }
            """;

        string diagnostics = """
            'x' is already declared.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void BlockStatement_NoInfiniteLoop()
    {
        string text = """
            {
            $[)]$$[]$
            """;

        string diagnostics = """
            Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
            Unexpected token <EndOfFileToken>, expected <CloseBraceToken>.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void InvokeFunctionArguments_Missing()
    {
        string text = """
            inspect($[)]$
            """;

        string diagnostics = """
            Function 'inspect' requires 1 arguments but was given 0.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void InvokeFunctionArguments_Exceeding()
    {
        string text = """
            inspect(1$[, 2, 3]$)
            """;

        string diagnostics = """
            Function 'inspect' requires 1 arguments but was given 3.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void InvokeFunctionArguments_NoInfiniteLoop()
    {
        string text = @"
                inspect(1$[$[=]$]$$[$[)]$]$
            ";

        string diagnostics = """
            Unexpected token <EqualsToken>, expected <CloseParenthesisToken>.
            Unexpected token <EqualsToken>, expected <IdentifierToken>.
            Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
            Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void FunctionParameters_NoInfiniteLoop()
    {
        string text = @"
                func foo(float numb$[$[$[=]$]$]$$[$[)]$]$
                {
                    inspect(numb)
                }$[]$
            ";

        string diagnostics = @"
                Unexpected token <EqualsToken>, expected <CloseParenthesisToken>.
                Unexpected token <EqualsToken>, expected <OpenBraceToken>.
                Unexpected token <EqualsToken>, expected <IdentifierToken>.
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
                Unexpected token <EndOfFileToken>, expected <CloseBraceToken>.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    //[Fact]
    //public void FunctionReturn_Missing()
    //{
    //    string text = @"
    //        function [add](a: int, b: int): int
    //        {
    //        }
    //    ";

    //    string diagnostics = @"
    //        Not all code paths return a value.
    //    ";

    //    AssertDiagnostics(text, diagnostics);
    //}

    [Fact]
    public void IfStatement_Reports_CannotConvert()
    {
        string text = """
            {
                float x = 0
                if $[10]$
                    x = 10
            }
            """;

        string diagnostics = """
            Cannot convert type 'float' to 'bool'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void WhileStatement_Reports_CannotConvert()
    {
        string text = """
            {
                float x = 0
                while $[10]$
                    x = 10
            }
            """;

        string diagnostics = """
            Cannot convert type 'float' to 'bool'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void DoWhileStatement_Reports_CannotConvert()
    {
        string text = @"
                {
                    float x = 0
                    do
                    {
                        x = 10
                    }
                    while $[(10)]$
                }
            ";

        string diagnostics = @"
                Cannot convert type 'float' to 'bool'.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    //[Fact]
    //public void ForStatement_Reports_CannotConvert_LowerBound()
    //{
    //    string text = @"
    //        {
    //            string result = 0
    //            for i = [false] to 10
    //                result = result + i
    //        }
    //    ";

    //    string diagnostics = @"
    //        Cannot convert type 'bool' to 'int'.
    //    ";

    //    AssertDiagnostics(text, diagnostics);
    //}

    //[Fact]
    //public void ForStatement_Reports_CannotConvert_UpperBound()
    //{
    //    string text = @"
    //        {
    //            string result = 0
    //            for i = 1 to [true]
    //                result = result + i
    //        }
    //    ";

    //    string diagnostics = @"
    //        Cannot convert type 'bool' to 'int'.
    //    ";

    //    AssertDiagnostics(text, diagnostics);
    //}

    [Fact]
    public void NameExpression_Reports_Undefined()
    {
        string text = """
            $[x]$ * 10
            """;

        string diagnostics = """
            Variable 'x' doesn't exist.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void NameExpression_Reports_NoErrorForInsertedToken()
    {
        string text = """
            1 + $[]$
            """;

        string diagnostics = """
            Unexpected token <EndOfFileToken>, expected <IdentifierToken>.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void UnaryExpression_Reports_Undefined()
    {
        string text = """
            $[+]$true
            """;

        string diagnostics = """
            Unary operator '+' is not defined for type 'bool'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void BinaryExpression_Reports_Undefined()
    {
        string text = """
            10 $[*]$ false
            """;

        string diagnostics = """
            Binary operator '*' is not defined for types 'float' and 'bool'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void CompoundExpression_Reports_Undefined()
    {
        string text = """
            float x = 10
            x $[+=]$ false
            """;

        string diagnostics = """
            Binary operator '+=' is not defined for types 'float' and 'bool'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void AssignmentExpression_Reports_Undefined()
    {
        string text = """
            $[x]$ = 10
            """;

        string diagnostics = """
            Variable 'x' doesn't exist.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void CompoundExpression_Assignemnt_NonDefinedVariable_Reports_Undefined()
    {
        string text = """
            $[x]$ += 10
            """;

        string diagnostics = """
            Variable 'x' doesn't exist.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void AssignmentExpression_Reports_CannotAssign()
    {
        string text = """
            {
                readonly float x = 10
                x $[=]$ 0
                x $[+=]$ 5
                x$[++]$
            }
            """;

        string diagnostics = """
            Variable 'x' is read-only and cannot be assigned to.
            Variable 'x' is read-only and cannot be assigned to.
            Variable 'x' is read-only and cannot be assigned to.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void CompoundDeclarationExpression_Reports_CannotAssign()
    {
        string text = """
            {
                readonly float x = 10
                const float y = 10
                x $[+=]$ 1
                y $[+=]$ 1
            }
            """;

        string diagnostics = """
            Variable 'x' is read-only and cannot be assigned to.
            Variable 'y' is read-only and cannot be assigned to.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void AssignmentExpression_Reports_CannotConvert()
    {
        string text = """
            {
                float x = 10
                x = $[true]$
            }
            """;

        string diagnostics = """
            Cannot convert type 'bool' to 'float'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void CallExpression_Reports_Undefined()
    {
        string text = """
            $[foo]$(42)
            """;

        string diagnostics = """
            Function 'foo' doesn't exist.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Void_Function_Should_Not_Return_Value()
    {
        string text = @"
                func test()
                {
                    return $[1]$
                }
            ";

        string diagnostics = @"
                Since the function 'test' does not return a value the 'return' keyword cannot be followed by an expression.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Function_With_ReturnValue_Should_Not_Return_Void()
    {
        string text = @"
                func float test()
                {
                    $[return]$
                }
            ";

        string diagnostics = @"
                An expression of type 'float' is expected.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Not_All_Code_Paths_Return_Value()
    {
        string text = @"
                func bool $[test]$(float n)
                {
                    if (n > 10)
                       return true
                }
            ";

        string diagnostics = @"
                Not all code paths return a value.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Expression_Must_Have_Value()
    {
        string text = @"
                func test(float n)
                {
                    return
                }

                float value = $[test(100)]$
            ";

        string diagnostics = @"
                Expression must have a value.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void IfStatement_Reports_NotReachableCode_Warning()
    {
        string text = @"
                func test()
                {
                    const float x = 4 * 3
                    if x > 12
                    {
                        $[inspect]$(x)
                    }
                    else
                    {
                        inspect(x)
                    }
                }
            ";

        string diagnostics = @"
                Unreachable code detected.
            ";
        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void ElseStatement_Reports_NotReachableCode_Warning()
    {
        string text = @"
                func float test()
                {
                    if true
                    {
                        return 1
                    }
                    else
                    {
                        $[return]$ 0
                    }
                }
            ";

        string diagnostics = @"
                Unreachable code detected.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void WhileStatement_Reports_NotReachableCode_Warning()
    {
        string text = @"
                func test()
                {
                    while false
                    {
                        $[continue]$
                    }
                }
            ";

        string diagnostics = @"
                Unreachable code detected.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    [Theory]
    [InlineData("$[2]$")]
    [InlineData("$[5 + 3]$")]
    public void ExpressionStatement_Reports_Invalid(string text)
    {
        string diagnostics = """
            Only call expressions can be used as a statement.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Theory]
    [InlineData("$[break]$", "break")]
    [InlineData("$[continue]$", "continue")]
    public void Invalid_Break_Or_Continue(string text, string keyword)
    {
        string diagnostics = $"""
            The keyword '{keyword}' can only be used inside of loops.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Theory]
    [InlineData("break")]
    [InlineData("continue")]
    [InlineData("return")]
    public void Invalid_Keyword_In_Event(string keyword)
    {
        string text = $$"""
            float x
            while (x > 0)
            {
                on Play()
                {
                    $[{{keyword}}]$
                }
            }
            """;

        string diagnostics = $"""
            The keyword '{keyword}' cannot be used inside of an event block.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Value_Must_Be_Constant()
    {
        string text = """
            float x
            obj o = getObject($[x]$, 0, 0)
            """;

        string diagnostics = """
            Value must be constant.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Generic_Type_Recursion()
    {
        string text = """
            array<array$[<float>]$> x
            """;

        string diagnostics = """
            Type argument cannot be generic.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Not_A_Generic_Type()
    {
        string text = """
            float$[<float>]$ x
            """;

        string diagnostics = """
            Non generic type cannot have a type argument.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Type_Must_Have_Generic_Parameter()
    {
        string text = """
            $[array]$ x
            """;

        string diagnostics = """
            Type must have a type argument.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Non_Generic_Method_Type_Arguments()
    {
        string text = """
            float x = min$[<float>]$(10, 5)
            """;

        string diagnostics = """
            Non-generic methods cannot be used with a type argument.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Variable_Name_Too_Long()
    {
        string name = new string('x', FCInfo.FancadeConstants.MaxVariableNameLength + 1);

        string text = $"""
            float $[{name}]$
            """;

        string diagnostics = $"""
            Variable name '{name}' is too long, maximum allowed length is {FCInfo.FancadeConstants.MaxVariableNameLength}
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Empty_Array_Initializer()
    {
        string text = """
            array<float> x = $[[]]$
            """;

        string diagnostics = """
            Array initializer cannot be empty.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetModifiersFor), false, ModifierTarget.Variable, null)]
    public void Invalid_Modifier_Target_Variable(string modifier, string validTargets)
    {
        string text = $"""
            $[{modifier}]$ float x
            """;

        string diagnostics = $"""
            Modifier '{modifier}' was used on <{ModifierTarget.Variable}>, but it can only be used on <{validTargets}>.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetModifiersFor), true, ModifierTarget.Variable, null)]
    public void Duplicate_Modifier(string modifier)
    {
        string text = $"""
            {modifier} $[{modifier}]$ float x = 0
            """;

        string diagnostics = $"""
            Duplicate '{modifier}' modifier.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Invalid_Modifier_On_Type()
    {
        string text = $"""
            $[const]$ array<float> x
            """;

        string diagnostics = $"""
            Modifier 'const' isn't valid on a variable of type 'array<float>'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Theory]
    [InlineData("readonly")]
    [InlineData("const")]
    public void Variable_Not_Initialized(string modifier)
    {
        string text = $"""
            {modifier} float $[x]$
            """;

        string diagnostics = $"""
            A readonly/constant variable needs to be initialized.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Theory]
    [MemberData(nameof(GetConflictingModifiersFor), ModifierTarget.Variable)]
    public void Conflicting_Modifiers(string modifier1, string modifier2)
    {
        string text = $"""
            $[{modifier1}]$ {modifier2} float x = 0
            """;

        string diagnostics = $"""
            Modifier '{modifier1}' conflicts with modifier '{modifier2}'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Unknown_Special_Block()
    {
        string text = """
            on $[Foo]$()
            {
            }
            """;

        string diagnostics = $"""
            Unknown event 'Foo'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Argument_Must_Have_Modifier()
    {
        string text = """
            float x
            float y
            worldToScreen(vec3(0, 0, 0), $[x]$, out y)

            on Touch(out x, $[y]$, 0, 0)
            {
            }
            """;

        string diagnostics = $"""
            Argument for paramater 'screenX' must be passed with the 'out' modifier.
            Argument for paramater 'screenY' must be passed with the 'out' modifier.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Argument_Cannot_Have_Modifier()
    {
        string text = """
            float y
            worldToScreen(vec3(0, 0, 0), out $[5]$, out y)
            """;

        string diagnostics = $"""
            A out argument must be an assignable variable.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void By_Ref_Arg_Must_Be_Variable()
    {
        string text = """
            float x
            inspect($[ref]$ x)
            inspect($[out]$ x)
            """;

        string diagnostics = $"""
            Argument for paramater 'value' cannot be passed with the 'ref' modifier.
            Argument for paramater 'value' cannot be passed with the 'out' modifier.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Undefined_Property()
    {
        string text = """
            float x = vec3(1, 2, 3).$[a]$
            """;

        string diagnostics = $"""
            Type 'vec3' doesn't have a property 'a'.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void SB_Must_Have_Arguments()
    {
        string text = """
            on $[Touch]$
            {
            }
            """;

        string diagnostics = $"""
            Event 'Touch' must have arguments.
            """;

        AssertDiagnostics(text, diagnostics);
    }

    // TODO: uncomment when a modifier uses required modifiers
    //[Theory]
    //[MemberData(nameof(GetModifiersWithMissingRequiredFor), ModifierTarget.Variable)]
    //public void Missign_Required_Modifiers(string modifier, string requiredModifiers)
    //{
    //    string text = $"""
    //        $[{modifier}]$ float x = 0
    //        """;

    //    string diagnostics = $"""
    //        Modifier '{modifier}' requires that one of <{requiredModifiers}> is present.
    //        """;

    //    AssertDiagnostics(text, diagnostics);
    //}

    [Fact]
    public void Parameter_Already_Declared()
    {
        string text = @"
                func sum(float a, float b, float $[a]$, out float res)
                {
                    res = a + b + c
                }
            ";

        string diagnostics = @"
                A parameter with the name 'a' already exists.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Function_Must_Have_Name()
    {
        string text = @"
                func $[(]$float a, float b, out float res)
                {
                    res = a + b
                }
            ";

        string diagnostics = @"
                Unexpected token <OpenParenthesisToken>, expected <IdentifierToken>.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    [Fact]
    public void Wrong_Argument_Type()
    {
        string text = @"
                func bool test(float n)
                {
                    return n > 10
                }
                bool testValue = false
                test($[testValue]$)
            ";

        string diagnostics = @"
                Cannot convert type 'bool' to 'float'.
            ";

        AssertDiagnostics(text, diagnostics);
    }

    //[Fact]
    //public void Bad_Type()
    //{
    //    string text = @"
    //        func test($[invalidtype]$ n)
    //        {
    //        }
    //    ";

    //    string diagnostics = @"
    //        Type 'invalidtype' doesn't exist.
    //    ";

    //    AssertDiagnostics(text, diagnostics);
    //}

    [Fact]
    public void Circular_Call()
    {
        string text = @"
                func a()
                {
                    a()
                }
            ";

        string diagnostics = @"
                Circular calls (recursion) aren't allowed (void a() -> void a()).
            ";

        AssertDiagnostics(text, diagnostics, noLocationDiagnostic: true);
    }
    [Fact]
    public void Circular_Call2()
    {
        string text = @"
                func a()
                {
                    b()
                }

                func b()
                {
                    a()
                }
            ";

        string diagnostics = @"
                Circular calls (recursion) aren't allowed (void b() -> void a() -> void b()).
            ";

        AssertDiagnostics(text, diagnostics, noLocationDiagnostic: true);
    }

    private static void AssertDiagnostics(string text, string diagnosticText, bool noLocationDiagnostic = false)
    {
        AnnotatedText annotatedText = AnnotatedText.Parse(text, noLocationDiagnostic);
        SyntaxTree syntaxTree = SyntaxTree.Parse(annotatedText.Text);
        Compilation compilation = Compilation.Create(null, syntaxTree);

        BlockBuilder builder = new EditorScriptBlockBuilder();
        CodePlacer placer = new TowerCodePlacer(builder);

        ImmutableArray<Diagnostic> diagnostics = compilation.Emit(placer, builder);

        string[] expectedDiagnostics = AnnotatedText.UnindentLines(diagnosticText);

        if (annotatedText.Spans.Length != expectedDiagnostics.Length)
            throw new Exception("ERROR: Must mark as many spans as there are expected diagnostics");

        Assert.Equal(expectedDiagnostics.Length, diagnostics.Length);

        for (int i = 0; i < expectedDiagnostics.Length; i++)
        {
            string expectedMessage = expectedDiagnostics[i];
            string actualMessage = diagnostics[i].Message;
            Assert.Equal(expectedMessage, actualMessage);

            TextSpan expectedSpan = annotatedText.Spans[i];
            TextSpan actualSpan = diagnostics[i].Location.Span;
            Assert.Equal(expectedSpan, actualSpan);
        }
    }

    public static IEnumerable<object[]> GetModifiersFor(bool valid, ModifierTarget target, TypeSymbol? type = null)
    {
        foreach (var mod in GetModifiersForTarget(valid, target, type))
        {
            yield return valid ? ([mod.ToSyntaxString()]) : ([mod.ToSyntaxString(), string.Join(", ", mod.GetTargets())]);
        }
    }

    public static IEnumerable<object[]> GetConflictingModifiersFor(ModifierTarget validTarget)
    {
        foreach (var mod1 in Enum.GetValues<Modifiers>())
        {
            var mod1Targets = mod1.GetTargets();
            if (mod1Targets.Contains(validTarget))
            {
                var required1 = mod1.GetRequiredModifiers();

                foreach (var mod2 in Enum.GetValues<Modifiers>())
                {
                    var required2 = mod2.GetRequiredModifiers();

                    if (mod1.GetConflictingModifiers().Contains(mod2) && mod2.GetTargets().Contains(validTarget) &&
                        (required1.Count == 0 || required1.Contains(mod2)) &&
                        (required2.Count == 0 || required2.Contains(mod1)))
                        yield return [mod1.ToSyntaxString(), mod2.ToSyntaxString()];
                }
            }
        }
    }

    // currently not used by anything
    //public static IEnumerable<object[]> GetModifiersWithMissingRequiredFor(ModifierTarget target)
    //{
    //    Modifiers validMods = ModifiersE.GetValidModifiersFor(target, null);

    //    foreach (var mod in Enum.GetValues<Modifiers>())
    //    {
    //        if (validMods.HasFlag(mod) && mod.GetRequiredModifiers().Count != 0)
    //            yield return [mod.ToSyntaxString(), string.Join(", ", mod.GetRequiredModifiers().Select(mod => mod.ToSyntaxString()))];
    //    }
    //}

    public static IEnumerable<object[]> GetVariableDeclarations()
    {
        ReadOnlyDictionary<TypeSymbol, string?> initializers = new Dictionary<TypeSymbol, string?>()
        {
            [TypeSymbol.Bool] = "true",
            [TypeSymbol.Float] = "1",
            [TypeSymbol.Vector3] = "vec3(1, 2, 3)",
            [TypeSymbol.Rotation] = "rot(1, 2, 3)",
            [TypeSymbol.Object] = "null",
            [TypeSymbol.Constraint] = "null",
            [TypeSymbol.CreateGenericInstance(TypeSymbol.Array, TypeSymbol.Bool)] = "[true, false, true]",
            [TypeSymbol.CreateGenericInstance(TypeSymbol.Array, TypeSymbol.Float)] = "[1, 2, 3]",
            [TypeSymbol.CreateGenericInstance(TypeSymbol.Array, TypeSymbol.Vector3)] = "[vec3(1, 1, 1), vec3(2, 2, 2), vec3(3, 3, 3)]",
            [TypeSymbol.CreateGenericInstance(TypeSymbol.Array, TypeSymbol.Rotation)] = "[rot(1, 1, 1), rot(2, 2, 2), rot(3, 3, 3)]",
            [TypeSymbol.CreateGenericInstance(TypeSymbol.Array, TypeSymbol.Object)] = "null",
            [TypeSymbol.CreateGenericInstance(TypeSymbol.Array, TypeSymbol.Constraint)] = "null",
        }.AsReadOnly();

        foreach (TypeSymbol type in TypeSymbol.BuiltInNonGenericTypes)
        {
            foreach (Modifiers mods in GetModifierCombinationsFor(ModifierTarget.Variable, type))
            {
                string declaration = mods.ToSyntaxString() + " " + type + " x";

                if (!mods.HasFlag(Modifiers.Constant) && !mods.HasFlag(Modifiers.Readonly))
                    yield return [declaration];

                string? initializer = initializers[type];
                if (!string.IsNullOrEmpty(initializer))
                    yield return [declaration + " = " + initializer];
            }
        }

        foreach (TypeSymbol baseType in TypeSymbol.BuiltInGenericTypes)
        {
            foreach (TypeSymbol innerType in TypeSymbol.BuiltInNonGenericTypes)
            {
                TypeSymbol type = TypeSymbol.CreateGenericInstance(baseType, innerType);

                foreach (Modifiers mods in GetModifierCombinationsFor(ModifierTarget.Variable, type))
                {
                    string declaration = mods.ToSyntaxString() + " " + type + " x";

                    if (!mods.HasFlag(Modifiers.Constant) && !mods.HasFlag(Modifiers.Readonly))
                        yield return [declaration];

                    string? initializer = initializers[type];
                    if (!string.IsNullOrEmpty(initializer))
                        yield return [declaration + " = " + initializer];
                }
            }
        }
    }

    #region Utils
    private static IEnumerable<Modifiers> GetModifiersForTarget(bool valid, ModifierTarget target, TypeSymbol? type = null)
    {
        Modifiers validMods = ModifiersE.GetValidModifiersFor(target, type);

        foreach (var mod in Enum.GetValues<Modifiers>())
        {
            if (valid == validMods.HasFlag(mod) && (!valid || mod == ValidateModifiers(mod, target, type)))
                yield return mod;
        }
    }
    private static IEnumerable<Modifiers> GetModifierCombinationsFor(ModifierTarget target, TypeSymbol? type = null)
    {
        HashSet<Modifiers> returnedMods = [];

        int max = (int)ModifiersE.All();
        for (int i = 0; i <= max; i++)
        {
            Modifiers mods = ValidateModifiers((Modifiers)i, target, type);
            if (returnedMods.Add(mods)) // don't return the same modifiers multiple times
                yield return mods;
        }
    }

    private static Modifiers ValidateModifiers(Modifiers mods, ModifierTarget? target = null, TypeSymbol? type = null)
    {
        Modifiers? validMods = null;
        if (target is not null)
            validMods = ModifiersE.GetValidModifiersFor(target.Value, type);

        foreach (var mod in Enum.GetValues<Modifiers>())
            if (mods.HasFlag(mod))
            {
                if (validMods is not null && !validMods.Value.HasFlag(mod))
                {
                    mods ^= mod; // toggle mod bit (remove)
                    continue;
                }

                foreach (var conflict in mod.GetConflictingModifiers())
                    if (mods.HasFlag(conflict))
                        mods ^= conflict; // toggle conflict bit (remove)

                var requiredMods = mod.GetRequiredModifiers();
                if (requiredMods.Count != 0)
                {
                    bool found = false;

                    foreach (var required in requiredMods)
                        if (mods.HasFlag(required))
                        {
                            found = true;
                            break;
                        }

                    if (!found)
                        mods |= requiredMods.First();
                }
            }

        return mods;
    }
    #endregion
}