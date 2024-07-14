using FanScript.Compiler.Emit;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
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

        public void ReportParameterAlreadyDeclared(TextLocation location, string parameterName)
            => ReportError(location, $"A parameter with the name '{parameterName}' already exists.");

        public void ReportUndefinedVariable(TextLocation location, string name)
            => ReportError(location, $"Variable '{name}' doesn't exist.");

        public void ReportNotAVariable(TextLocation location, string name)
            => ReportError(location, $"'{name}' is not a variable.");

        public void ReportUndefinedType(TextLocation location, string name)
            => ReportError(location, $"Type '{name}' doesn't exist.");

        public void ReportCannotConvert(TextLocation location, TypeSymbol? fromType, TypeSymbol? toType)
            => ReportError(location, $"Cannot convert type '{fromType}' to '{toType}'.");

        public void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol? fromType, TypeSymbol? toType)
            => ReportError(location, $"Cannot convert type '{fromType}' to '{toType}'. An explicit conversion exists (are you missing a cast?)");

        public void ReportSymbolAlreadyDeclared(TextLocation location, string name)
            => ReportError(location, $"'{name}' is already declared.");

        public void ReportCannotAssignReadOnly(TextLocation location, string name)
            => ReportError(location, $"Variable '{name}' is read-only and cannot be assigned to.");

        public void ReportUndefinedFunction(TextLocation location, string name)
            => ReportError(location, $"Function '{name}' doesn't exist.");

        public void ReportNotAFunction(TextLocation location, string name)
            => ReportError(location, $"'{name}' is not a function.");

        public void ReportWrongArgumentCount(TextLocation location, string name, int expectedCount, int actualCount)
            => ReportError(location, $"Function '{name}' requires {expectedCount} arguments but was given {actualCount}.");

        public void ReportExpressionMustHaveValue(TextLocation location)
            => ReportError(location, "Expression must have a Value.");

        public void ReportAllPathsMustReturn(TextLocation location)
            => ReportError(location, "Not all code paths return a Value.");

        public void ReportInvalidExpressionStatement(TextLocation location)
            => ReportError(location, $"Only call expressions can be used as a statement.");

        public void ReportOnlyOneFileCanHaveGlobalStatements(TextLocation location)
            => ReportError(location, $"At most one file can have global statements.");

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
                case SyntaxKind.VariableDeclaration:
                    ReportUnreachableCode(((VariableDeclarationSyntax)node).TypeClause.Location);
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
                case SyntaxKind.CallExpression:
                    ReportUnreachableCode(((CallExpressionSyntax)node).Identifier.Location);
                    return;
                default:
                    throw new Exception($"Unexpected syntax {node.Kind}");
            }
        }

        public void ReportInvalidBreakOrContinue(TextLocation location, string text)
          => ReportError(location, $"The keyword '{text}' can only be used inside of loops.");

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
            => ReportError(location, $"Variable name '{name}' is too long, maximum allowed length is {Constants.MaxVariableNameLength}");

        public void ReportEmptyArrayInitializer(TextLocation location)
            => ReportError(location, $"Array initializer cannot be empty.");
    }
}