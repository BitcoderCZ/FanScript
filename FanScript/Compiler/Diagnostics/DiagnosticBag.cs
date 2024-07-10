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
        {
            var message = $"The number {text} isn't valid {type}.";
            ReportError(location, message);
        }

        public void ReportBadCharacter(TextLocation location, char character)
        {
            var message = $"Bad character input: '{character}'.";
            ReportError(location, message);
        }

        public void ReportUnterminatedString(TextLocation location)
        {
            var message = "Unterminated string literal.";
            ReportError(location, message);
        }

        public void ReportInvalidEscapeSequance(TextLocation location, string escapeSequance)
            => ReportError(location, $"Invalid escape sequance: '{escapeSequance}'.");

        public void ReportUnterminatedMultiLineComment(TextLocation location)
        {
            var message = "Unterminated multi-line comment.";
            ReportError(location, message);
        }

        public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind expectedKind)
        {
            var message = $"Unexpected token <{actualKind}>, expected <{expectedKind}>.";
            ReportError(location, message);
        }
        public void ReportUnexpectedToken(TextLocation location, SyntaxKind actualKind, SyntaxKind[] expectedKinds)
        {
            var message = $"Unexpected token <{actualKind}>, expected one of <{string.Join(", ", expectedKinds)}>.";
            ReportError(location, message);
        }

        public void ReportUndefinedUnaryOperator(TextLocation location, string operatorText, TypeSymbol? operandType)
        {
            var message = $"Unary operator '{operatorText}' is not defined for type '{operandType}'.";
            ReportError(location, message);
        }

        public void ReportUndefinedBinaryOperator(TextLocation location, string operatorText, TypeSymbol? leftType, TypeSymbol? rightType)
        {
            var message = $"Binary operator '{operatorText}' is not defined for types '{leftType}' and '{rightType}'.";
            ReportError(location, message);
        }

        public void ReportParameterAlreadyDeclared(TextLocation location, string parameterName)
        {
            var message = $"A parameter with the name '{parameterName}' already exists.";
            ReportError(location, message);
        }

        public void ReportUndefinedVariable(TextLocation location, string name)
        {
            var message = $"Variable '{name}' doesn't exist.";
            ReportError(location, message);
        }

        public void ReportNotAVariable(TextLocation location, string name)
        {
            var message = $"'{name}' is not a variable.";
            ReportError(location, message);
        }

        public void ReportUndefinedType(TextLocation location, string name)
        {
            var message = $"Type '{name}' doesn't exist.";
            ReportError(location, message);
        }

        public void ReportCannotConvert(TextLocation location, TypeSymbol? fromType, TypeSymbol? toType)
        {
            var message = $"Cannot convert type '{fromType}' to '{toType}'.";
            ReportError(location, message);
        }

        public void ReportCannotConvertImplicitly(TextLocation location, TypeSymbol? fromType, TypeSymbol? toType)
        {
            var message = $"Cannot convert type '{fromType}' to '{toType}'. An explicit conversion exists (are you missing a cast?)";
            ReportError(location, message);
        }

        public void ReportSymbolAlreadyDeclared(TextLocation location, string name)
        {
            var message = $"'{name}' is already declared.";
            ReportError(location, message);
        }

        public void ReportCannotAssign(TextLocation location, string name)
        {
            var message = $"Variable '{name}' is read-only and cannot be assigned to.";
            ReportError(location, message);
        }

        public void ReportUndefinedFunction(TextLocation location, string name)
        {
            var message = $"Function '{name}' doesn't exist.";
            ReportError(location, message);
        }

        public void ReportNotAFunction(TextLocation location, string name)
        {
            var message = $"'{name}' is not a function.";
            ReportError(location, message);
        }

        public void ReportWrongArgumentCount(TextLocation location, string name, int expectedCount, int actualCount)
        {
            var message = $"Function '{name}' requires {expectedCount} arguments but was given {actualCount}.";
            ReportError(location, message);
        }

        public void ReportExpressionMustHaveValue(TextLocation location)
        {
            var message = "Expression must have a Value.";
            ReportError(location, message);
        }

        public void ReportAllPathsMustReturn(TextLocation location)
            => ReportError(location, "Not all code paths return a Value.");

        public void ReportInvalidExpressionStatement(TextLocation location)
            => ReportError(location, $"Only assignment and call expressions can be used as a statement.");

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
                    ReportUnreachableCode(((VariableDeclarationSyntax)node).Keyword.Location);
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
                case SyntaxKind.AssignmentExpression:
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
    }
}