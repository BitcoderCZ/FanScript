using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.FCInfo;
using System.Collections;

namespace FanScript.Compiler.Diagnostics
{
    public sealed class DiagnosticBag : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddRange(IEnumerable<Diagnostic> diagnostics)
            => _diagnostics.AddRange(diagnostics);

        private void ReportError(TextLocation location, string message)
            => _diagnostics.Add(Diagnostic.Error(location, message));

        private void ReportWarning(TextLocation location, string message)
            => _diagnostics.Add(Diagnostic.Warning(location, message));

        public void ReportInvalidNumber(TextLocation location, string text, TypeSymbol type)
            => ReportError(location, $"The number {text} isn't valid {type}.");

        public void ReportBadCharacter(TextLocation location, char character)
            => ReportError(location, $"Bad character input: '{character}'.");

        public void ReportUnterminatedString(TextLocation location)
            => ReportError(location, "Unterminated string literal.");

        public void ReportInvalidEscapeSequance(TextLocation location, string escapeSequance)
            => ReportError(location, $"Invalid escape sequance: '{escapeSequance}'.");

        public void ReportUnterminatedMultiLineComment(TextLocation location)
            => ReportError(location, "Unterminated multi-line comment.");

        public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind expectedKind)
            => ReportError(location, $"Unexpected token <{actualKind}>, expected <{expectedKind}>.");
        public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind[] expectedKinds)
            => ReportError(location, $"Unexpected token <{actualKind}>, expected one of <{string.Join(", ", expectedKinds)}>.");

        public void ReportUndefinedUnaryOperator(TextLocation location, string operatorText, TypeSymbol? operandType)
            => ReportError(location, $"Unary operator '{operatorText}' is not defined for type '{operandType}'.");

        public void ReportUndefinedBinaryOperator(TextLocation location, string operatorText, TypeSymbol? leftType, TypeSymbol? rightType)
            => ReportError(location, $"Binary operator '{operatorText}' is not defined for types '{leftType}' and '{rightType}'.");

        public void ReportUndefinedPostfixOperator(TextLocation location, string operatorText, TypeSymbol? operandType)
            => ReportError(location, $"Postfix operator '{operatorText}' is not defined for type '{operandType}'.");

        public void ReportParameterAlreadyDeclared(TextLocation location, string parameterName)
            => ReportError(location, $"A parameter with the name '{parameterName}' already exists.");

        public void ReportUndefinedVariable(TextLocation location, string name)
            => ReportError(location, $"Variable '{name}' doesn't exist.");

        public void ReportUndefinedType(TextLocation location, string name)
            => ReportError(location, $"Type '{name}' doesn't exist.");

        public void ReportCannotConvert(TextLocation location, TypeSymbol? fromType, TypeSymbol? toType)
            => ReportError(location, $"Cannot convert type '{fromType}' to '{toType}'.");

        public void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol? fromType, TypeSymbol? toType)
            => ReportError(location, $"Cannot convert type '{fromType}' to '{toType}'. An explicit conversion exists (are you missing a cast?)");

        public void ReportSymbolAlreadyDeclared(TextLocation location, string name)
            => ReportError(location, $"'{name}' is already declared.");

        public void ReportCannotAssignReadOnlyVariable(TextLocation location, string name)
            => ReportError(location, $"Variable '{name}' is read-only and cannot be assigned to.");

        public void ReportUndefinedFunction(TextLocation location, string name)
            => ReportError(location, $"Function '{name}' doesn't exist.");

        public void ReportNotAFunction(TextLocation location, string name)
            => ReportError(location, $"'{name}' is not a function.");

        public void ReportWrongArgumentCount(TextLocation location, string type, string name, int expectedCount, int actualCount)
            => ReportError(location, $"{type} '{name}' requires {expectedCount} arguments but was given {actualCount}.");

        public void ReportExpressionMustHaveValue(TextLocation location)
            => ReportError(location, "Expression must have a Value.");

        public void ReportAllPathsMustReturn(TextLocation location)
            => ReportError(location, "Not all code paths return a Value.");

        public void ReportInvalidExpressionStatement(TextLocation location)
            => ReportError(location, $"Only void call expressions can be used as a statement.");

        public void ReportOnlyOneFileCanHaveGlobalStatements(TextLocation location)
            => ReportError(location, $"At most one file can have global statements.");

        public void ReportInvalidBreakOrContinue(TextLocation location, string text)
          => ReportError(location, $"The keyword '{text}' can only be used inside of loops.");

        public void ReportBreakOrContinueInSB(TextLocation location, string text)
          => ReportError(location, $"The keyword '{text}' cannot be used inside of an on block.");

        public void ReportValueMustBeConstant(TextLocation location)
            => ReportError(location, "Value must be constant.");

        public void ReportGenericTypeNotAllowed(TextLocation location)
            => ReportError(location, "Generic type isn't allowed here.");

        public void ReportGenericTypeRecursion(TextLocation location)
            => ReportError(location, "Type argument cannot be generic.");

        public void ReportNotAGenericType(TextLocation location)
            => ReportError(location, "Non generic type cannot have a type argument.");

        public void ReportCannotInferGenericType(TextLocation location)
            => ReportError(location, "Cannot infer type argument from usage.");

        public void ReportSpecificGenericTypeNotAllowed(TextLocation location, TypeSymbol genericType, IEnumerable<TypeSymbol> allowedGenericTypes)
            => ReportError(location, $"Type argument type '{genericType}' isn't allowed, allowed types: <{string.Join(", ", allowedGenericTypes)}>.");

        public void ReportTypeMustHaveGenericParameter(TextLocation location)
            => ReportError(location, "Type must have a type argument.");

        public void ReportNonGenericMethodTypeArguments(TextLocation location)
            => ReportError(location, "Non-generic methods cannot be used with a type argument.");

        public void ReportVariableNameTooLong(TextLocation location, string name)
            => ReportError(location, $"Variable name '{name}' is too long, maximum allowed length is {FancadeConstants.MaxVariableNameLength}");

        public void ReportEmptyArrayInitializer(TextLocation location)
            => ReportError(location, $"Array initializer cannot be empty.");

        public void ReportNotAModifier(TextLocation location, string text)
            => ReportError(location, $"'{text}' isn't a valid modifier.");

        public void ReportInvalidModifier(TextLocation location, Modifiers modifier, ModifierTarget usedTarget, IEnumerable<ModifierTarget> validTargets)
            => ReportError(location, $"Modifier '{modifier.ToKind().GetText()}' was used on <{usedTarget}>, but it can only be used on <{string.Join(", ", validTargets)}>.");

        public void ReportDuplicateModifier(TextLocation location, Modifiers modifier)
            => ReportError(location, $"Duplicate '{modifier.ToKind().GetText()}' modifier.");

        public void ReportInvalidModifierOnType(TextLocation location, Modifiers modifier, TypeSymbol type)
            => ReportError(location, $"Modifier '{modifier.ToKind().GetText()}' isn't valid on a variable of type '{type}'.");

        public void ReportConstantNotInitialized(TextLocation location)
            => ReportError(location, "A constant variable needs to be initialized.");

        public void ReportConflictingModifiers(TextLocation location, Modifiers modifier, Modifiers conflictingModifier)
            => ReportError(location, $"Modifier '{conflictingModifier.ToKind().GetText()}' conflicts with modifier '{modifier.ToKind().GetText()}'.");

        public void ReportUnknownSpecialBlock(TextLocation location, string text)
            => ReportError(location, $"Unknown special block '{text}'.");

        public void ReportArgumentMustHaveModifier(TextLocation location, string name, Modifiers modifier)
            => ReportError(location, $"Argument for paramater '{name}' must be passed with the '{modifier.ToKind().GetText()}' modifier.");

        public void ReportArgumentCannotHaveModifier(TextLocation location, string name, Modifiers modifier)
            => ReportError(location, $"Argument for paramater '{name}' cannot be passed with the '{modifier.ToKind().GetText()}' modifier.");

        public void ReportRefMustBeVariable(TextLocation location, Modifiers makesRefMod)
            => ReportError(location, $"A {makesRefMod.ToKind().GetText()} argument must be an assignable variable.");

        public void ReportMustBeName(TextLocation location)
            => ReportError(location, "Expression must be a name");

        public void ReportUndefinedProperty(TextLocation location, TypeSymbol type, string name)
            => ReportError(location, $"Type '{type}' doesn't have a property '{name}'.");

        public void ReportCannotAssignReadOnlyProperty(TextLocation location, string name)
            => ReportError(location, $"Property '{name}' is read-only and cannot be assigned to.");

        public void ReportUnreachableCode(TextLocation location)
          => ReportWarning(location, $"Unreachable code detected.");
        public void ReportUnreachableCode(SyntaxNode node)
        {
            switch (node.Kind)
            {
                case SyntaxKind.BlockStatement:
                    StatementSyntax? firstStatement = ((BlockStatementSyntax)node).Statements.FirstOrDefault();
                    // Report just for non empty blocks.
                    if (firstStatement is not null)
                        ReportUnreachableCode(firstStatement);
                    return;
                case SyntaxKind.VariableDeclarationStatement:
                    ReportUnreachableCode(((VariableDeclarationStatementSyntax)node).TypeClause.Location);
                    return;
                case SyntaxKind.IfStatement:
                    ReportUnreachableCode(((IfStatementSyntax)node).IfKeyword.Location);
                    return;
                case SyntaxKind.WhileStatement:
                    ReportUnreachableCode(((WhileStatementSyntax)node).WhileKeyword.Location);
                    return;
                //case SyntaxKind.DoWhileStatement:
                //    ReportUnreachableCode(((DoWhileStatementSyntax)node).DoKeyword.Location);
                //    return;
                //case SyntaxKind.ForStatement:
                //    ReportUnreachableCode(((ForStatementSyntax)node).Keyword.Location);
                //    return;
                case SyntaxKind.BreakStatement:
                    ReportUnreachableCode(((BreakStatementSyntax)node).Keyword.Location);
                    return;
                case SyntaxKind.ContinueStatement:
                    ReportUnreachableCode(((ContinueStatementSyntax)node).Keyword.Location);
                    return;
                //case SyntaxKind.ReturnStatement:
                //    ReportUnreachableCode(((ReturnStatementSyntax)node).ReturnKeyword.Location);
                //    return;
                case SyntaxKind.AssignmentStatement:
                    ReportUnreachableCode(node.Location);
                    return;
                case SyntaxKind.ExpressionStatement:
                    ExpressionSyntax expression = ((ExpressionStatementSyntax)node).Expression;
                    ReportUnreachableCode(expression);
                    return;
                case SyntaxKind.NameExpression:
                    ReportUnreachableCode(((NameExpressionSyntax)node).IdentifierToken.Location);
                    break;
                case SyntaxKind.CallExpression:
                    ReportUnreachableCode(((CallExpressionSyntax)node).Identifier.Location);
                    return;
                default:
                    throw new Exception($"Unexpected syntax {node.Kind}");
            }
        }

        public void ReportOpeationNotSupportedOnPlatform(TextLocation location, BuildPlatformInfo platformInfo)
        {
            string msg = platformInfo switch
            {
                BuildPlatformInfo.CanGetBlocks => $"Current {nameof(CodeBuilder)} can't connect object wires to blocks.",
                BuildPlatformInfo.CanCreateCustomBlocks => $"Current {nameof(CodeBuilder)} can't create custom blocks.",
                _ => $"Operation not supported by current {nameof(CodeBuilder)}.",
            };

            ReportWarning(location, msg);
        }

        public void ReportFailedToDeclare(TextLocation location, string type, string name)
            => ReportWarning(location, $"Failed to declare {type} '{name}'.");
    }
}