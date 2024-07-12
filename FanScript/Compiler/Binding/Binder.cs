using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Lowering;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;

namespace FanScript.Compiler.Binding
{
    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly bool _isScript;
        private readonly FunctionSymbol? _function;

        private Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _loopStack = new Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)>();
        private int _labelCounter;
        private BoundScope _scope;

        private Binder(bool isScript, BoundScope? parent, FunctionSymbol? function)
        {
            _scope = new BoundScope(parent);
            _isScript = isScript;
            _function = function;

            if (function is not null)
            {
                foreach (ParameterSymbol p in function.Parameters)
                    _scope.TryDeclareVariable(p);
            }
        }

        public static BoundGlobalScope BindGlobalScope(bool isScript, BoundGlobalScope? previous, ImmutableArray<SyntaxTree> syntaxTrees)
        {
            if (!isScript)
                throw new Exception("Must be script");

            BoundScope parentScope = CreateParentScope(previous);
            Binder binder = new Binder(isScript, parentScope, function: null);

            binder.Diagnostics.AddRange(syntaxTrees.SelectMany(st => st.Diagnostics));
            if (binder.Diagnostics.HasErrors())
                return new BoundGlobalScope(previous, binder.Diagnostics.ToImmutableArray(), null, ImmutableArray<FunctionSymbol>.Empty, ImmutableArray<VariableSymbol>.Empty, ImmutableArray<BoundStatement>.Empty);

            IEnumerable<FunctionDeclarationSyntax> functionDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<FunctionDeclarationSyntax>();

            foreach (FunctionDeclarationSyntax function in functionDeclarations)
                binder.BindFunctionDeclaration(function);

            IEnumerable<GlobalStatementSyntax> globalStatements = syntaxTrees.SelectMany(st => st.Root.Members)
                                              .OfType<GlobalStatementSyntax>();

            ImmutableArray<BoundStatement>.Builder statements = ImmutableArray.CreateBuilder<BoundStatement>();

            foreach (GlobalStatementSyntax globalStatement in globalStatements)
            {
                BoundStatement statement = binder.BindGlobalStatement(globalStatement.Statement);
                statements.Add(statement);
            }

            // Check global statements
            GlobalStatementSyntax[] firstGlobalStatementPerSyntaxTree = syntaxTrees
                .Select(st => st.Root.Members.OfType<GlobalStatementSyntax>().FirstOrDefault())
                .Where(g => g is not null)
                .ToArray()!;

            if (firstGlobalStatementPerSyntaxTree.Length > 1)
                foreach (GlobalStatementSyntax globalStatement in firstGlobalStatementPerSyntaxTree)
                    binder.Diagnostics.ReportOnlyOneFileCanHaveGlobalStatements(globalStatement.Location);

            // Check for main/script with global statements

            ImmutableArray<FunctionSymbol> functions = binder._scope.GetDeclaredFunctions();

            FunctionSymbol? scriptFunction;

            if (globalStatements.Any())
                scriptFunction = new FunctionSymbol("$eval", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, null);
            else
                scriptFunction = null;

            ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.ToImmutableArray();
            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVariables();

            if (previous is not null)
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);

            return new BoundGlobalScope(previous, diagnostics, scriptFunction, functions, variables, statements.ToImmutable());
        }

        public static BoundProgram BindProgram(bool isScript, BoundProgram? previous, BoundGlobalScope globalScope)
        {
            BoundScope parentScope = CreateParentScope(globalScope);

            if (globalScope.Diagnostics.HasErrors())
                return new BoundProgram(previous, globalScope.Diagnostics, null, ImmutableDictionary<FunctionSymbol, BoundBlockStatement>.Empty);

            ImmutableDictionary<FunctionSymbol, BoundBlockStatement>.Builder functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
            ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            foreach (FunctionSymbol function in globalScope.Functions)
            {
                Binder binder = new Binder(isScript, parentScope, function);
                BoundStatement body = binder.BindStatement(function.Declaration!.Body);
                BoundBlockStatement loweredBody = Lowerer.Lower(function, body);

                if (function.Type != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                    binder._diagnostics.ReportAllPathsMustReturn(function.Declaration.Identifier.Location);

                functionBodies.Add(function, loweredBody);

                diagnostics.AddRange(binder.Diagnostics);
            }

            SyntaxNode? compilationUnit = globalScope.Statements.Any()
                ? globalScope.Statements.First().Syntax.AncestorsAndSelf().LastOrDefault()
                : null;

            if (globalScope.ScriptFunction is not null)
            {
                ImmutableArray<BoundStatement> statements = globalScope.Statements;
                /*if (statements.Length == 1 &&
                    statements[0] is BoundExpressionStatement es &&
                    es.Expression.Type != TypeSymbol.Void)
                    statements = statements.SetItem(0, new BoundReturnStatement(es.Expression.Syntax, es.Expression));
                else if (statements.Any() && statements.Last().Kind != BoundNodeKind.ReturnStatement)
                {
                    var nullValue = new BoundLiteralExpression(compilationUnit!, "");
                    statements = statements.Add(new BoundReturnStatement(compilationUnit!, nullValue));
                }*/

                BoundBlockStatement body = Lowerer.Lower(globalScope.ScriptFunction, new BoundBlockStatement(compilationUnit!, statements));
                functionBodies.Add(globalScope.ScriptFunction, body);
            }

            return new BoundProgram(previous,
                                    diagnostics.ToImmutable(),
                                    globalScope.ScriptFunction,
                                    functionBodies.ToImmutable());
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax syntax)
        {
            ImmutableArray<ParameterSymbol>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

            HashSet<string> seenParameterNames = new HashSet<string>();

            foreach (ParameterSyntax parameterSyntax in syntax.Parameters)
            {
                string parameterName = parameterSyntax.Identifier.Text;
                TypeSymbol? parameterType = BindTypeClause(parameterSyntax.Type);
                if (!seenParameterNames.Add(parameterName))
                    _diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, parameterName);
                else
                    parameters.Add(new ParameterSymbol(parameterName, parameterType, parameters.Count));
            }

            TypeSymbol type = BindTypeClause(syntax.TypeClause) ?? TypeSymbol.Void;

            FunctionSymbol function = new FunctionSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, syntax);
            if (syntax.Identifier.Text is not null &&
                !_scope.TryDeclareFunction(function))
                _diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, function.Name);
        }


        private static BoundScope CreateParentScope(BoundGlobalScope? previous)
        {
            Stack<BoundGlobalScope> stack = new Stack<BoundGlobalScope>();
            while (previous is not null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope parent = CreateRootScope();

            while (stack.Count > 0)
            {
                previous = stack.Pop();
                BoundScope scope = new BoundScope(parent);

                foreach (FunctionSymbol f in previous.Functions)
                    scope.TryDeclareFunction(f);

                foreach (VariableSymbol v in previous.Variables)
                    scope.TryDeclareVariable(v);

                parent = scope;
            }

            return parent;
        }

        private static BoundScope CreateRootScope()
        {
            BoundScope result = new BoundScope(null);

            foreach (var f in BuiltinFunctions.GetAll())
                result.TryDeclareFunction(f);

            return result;
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private BoundStatement BindErrorStatement(SyntaxNode syntax)
            => new BoundExpressionStatement(syntax, new BoundErrorExpression(syntax));

        private BoundStatement BindGlobalStatement(StatementSyntax syntax)
            => BindStatement(syntax, isGlobal: true);

        private BoundStatement BindStatement(StatementSyntax syntax, bool isGlobal = false)
        {
            BoundStatement result = BindStatementInternal(syntax);

            if (!_isScript || !isGlobal)
            {
                if (result is BoundExpressionStatement es)
                {
                    bool isAllowedExpression = es.Expression.Kind == BoundNodeKind.ErrorExpression ||
                                              es.Expression.Kind == BoundNodeKind.CallExpression;
                    if (!isAllowedExpression)
                        _diagnostics.ReportInvalidExpressionStatement(syntax.Location);
                }
            }

            return result;
        }

        private BoundStatement BindStatementInternal(StatementSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.BlockStatement:
                    return BindBlockStatement((BlockStatementSyntax)syntax);
                case SyntaxKind.SpecialBlockStatement:
                    return BindSpecialBlockStatement((SpecialBlockStatementSyntax)syntax);
                case SyntaxKind.VariableDeclaration:
                    return BindVariableDeclaration((VariableDeclarationSyntax)syntax);
                case SyntaxKind.AssignmentStatement:
                    return BindAssignmentStatement((AssignmentStatementSyntax)syntax);
                case SyntaxKind.IfStatement:
                    return BindIfStatement((IfStatementSyntax)syntax);
                case SyntaxKind.WhileStatement:
                    return BindWhileStatement((WhileStatementSyntax)syntax);
                //case SyntaxKind.DoWhileStatement:
                //    return BindDoWhileStatement((DoWhileStatementSyntax)syntax);
                //case SyntaxKind.ForStatement:
                //    return BindForStatement((ForStatementSyntax)syntax);
                case SyntaxKind.BreakStatement:
                    return BindBreakStatement((BreakStatementSyntax)syntax);
                case SyntaxKind.ContinueStatement:
                    return BindContinueStatement((ContinueStatementSyntax)syntax);
                //case SyntaxKind.ReturnStatement:
                //    return BindReturnStatement((ReturnStatementSyntax)syntax);
                case SyntaxKind.ExpressionStatement:
                    return BindExpressionStatement((ExpressionStatementSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            ImmutableArray<BoundStatement>.Builder statements = ImmutableArray.CreateBuilder<BoundStatement>();
            _scope = new BoundScope(_scope);

            foreach (StatementSyntax statementSyntax in syntax.Statements)
            {
                BoundStatement statement = BindStatement(statementSyntax);
                statements.Add(statement);
            }

            _scope = _scope.Parent!;

            return new BoundBlockStatement(syntax, statements.ToImmutable());
        }

        private BoundStatement BindSpecialBlockStatement(SpecialBlockStatementSyntax syntax)
        {
            BoundBlockStatement block = (BoundBlockStatement)BindBlockStatement(syntax.Block);
            return new BoundSpecialBlockStatement(syntax, syntax.KeywordToken, block);
        }

        private BoundStatement BindVariableDeclaration(VariableDeclarationSyntax syntax)
        {
            TypeSymbol? type = BindTypeClause(syntax.TypeClause);

            TypeSymbol? variableType = type;
            VariableSymbol variable = BindVariableDeclaration(syntax.Identifier, false, variableType, /*optionalAssignment?.ConstantValue*/null);

            BoundAssignmentStatement? optionalAssignment = syntax.OptionalAssignment is null ? null : (BoundAssignmentStatement)BindAssignmentStatement(syntax.OptionalAssignment);

            return new BoundVariableDeclaration(syntax, variable, optionalAssignment);
        }

        private BoundStatement BindAssignmentStatement(AssignmentStatementSyntax syntax)
        {
            string name = syntax.IdentifierToken.Text;
            BoundExpression boundExpression = BindExpression(syntax.Expression);

            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return BindErrorStatement(syntax);

            if (variable.IsReadOnly)
                _diagnostics.ReportCannotAssignReadOnly(syntax.AssignmentToken.Location, name);

            if (syntax.AssignmentToken.Kind != SyntaxKind.EqualsToken)
            {
                SyntaxKind equivalentOperatorTokenKind = SyntaxFacts.GetBinaryOperatorOfAssignmentOperator(syntax.AssignmentToken.Kind);
                BoundBinaryOperator? boundOperator = BoundBinaryOperator.Bind(equivalentOperatorTokenKind, variable.Type, boundExpression.Type);

                if (boundOperator is null)
                {
                    _diagnostics.ReportUndefinedBinaryOperator(syntax.AssignmentToken.Location, syntax.AssignmentToken.Text, variable.Type, boundExpression.Type);
                    return BindErrorStatement(syntax);
                }

                BoundExpression convertedExpression = BindConversion(syntax.Expression.Location, boundExpression, boundOperator.RightType/*variable.Type*/);

                return new BoundCompoundAssignmentStatement(syntax, variable, boundOperator, convertedExpression);
            }
            else
            {
                BoundExpression convertedExpression = BindConversion(syntax.Expression.Location, boundExpression, variable.Type);
                return new BoundAssignmentStatement(syntax, variable, convertedExpression);
            }
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition, TypeSymbol.Bool);

            if (condition.ConstantValue is not null)
            {
                if ((bool)condition.ConstantValue.Value == false)
                    _diagnostics.ReportUnreachableCode(syntax.ThenStatement);
                else if (syntax.ElseClause is not null)
                    _diagnostics.ReportUnreachableCode(syntax.ElseClause.ElseStatement);
            }

            BoundStatement thenStatement = BindStatement(syntax.ThenStatement);
            BoundStatement? elseStatement = syntax.ElseClause is null ? null : BindStatement(syntax.ElseClause.ElseStatement);
            return new BoundIfStatement(syntax, condition, thenStatement, elseStatement);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition, TypeSymbol.Bool);

            if (condition.ConstantValue != null)
                if (!(bool)condition.ConstantValue.Value)
                    _diagnostics.ReportUnreachableCode(syntax.Body);

            BoundStatement body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);
            return new BoundWhileStatement(syntax, condition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindLoopBody(StatementSyntax body, out BoundLabel breakLabel, out BoundLabel continueLabel)
        {
            _labelCounter++;
            breakLabel = new BoundLabel($"break{_labelCounter}");
            continueLabel = new BoundLabel($"continue{_labelCounter}");

            _loopStack.Push((breakLabel, continueLabel));
            BoundStatement boundBody = BindStatement(body);
            _ = _loopStack.Pop();

            return boundBody;
        }

        private BoundStatement BindBreakStatement(BreakStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }

            BoundLabel breakLabel = _loopStack.Peek().BreakLabel;
            return new BoundGotoStatement(syntax, breakLabel);
        }

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }

            BoundLabel continueLabel = _loopStack.Peek().ContinueLabel;
            return new BoundGotoStatement(syntax, continueLabel);
        }

        private TypeSymbol? BindTypeClause(TypeClauseSyntax? syntax)
        {
            if (syntax is null)
                return null;

            TypeSymbol? type = LookupType(syntax.TypeToken.Text);
            if (type is null)
                _diagnostics.ReportUndefinedType(syntax.Location, syntax.TypeToken.Text);

            if (syntax.HasGenericParameter)
            {
                if (type is null)
                    return TypeSymbol.Error;
                else if (type.IsGenericDefinition)
                {
                    TypeSymbol? innerType = BindTypeClause(syntax.InnerType);
                    if (innerType is null)
                        return TypeSymbol.Error;
                    else
                        return TypeSymbol.CreateGenericInstance(type, innerType);
                }
                else
                {
                    _diagnostics.ReportNotAGenericType(syntax.Location);
                    return TypeSymbol.Error;
                }
            } else if (type is not null && type.IsGenericDefinition)
            {
                _diagnostics.ReportTypeMustHaveGenericParameter(syntax.Location);
                return TypeSymbol.Error;
            }

            return type;
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            BoundExpression expression = BindExpression(syntax.Expression, canBeVoid: true);
            return new BoundExpressionStatement(syntax, expression);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, TypeSymbol targetType)
        {
            return BindConversion(syntax, targetType);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, bool canBeVoid = false)
        {
            BoundExpression result = BindExpressionInternal(syntax);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                _diagnostics.ReportExpressionMustHaveValue(syntax.Location);
                return new BoundErrorExpression(syntax);
            }

            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.ParenthesizedExpression:
                    return BindParenthesizedExpression((ParenthesizedExpressionSyntax)syntax);
                case SyntaxKind.LiteralExpression:
                    return BindLiteralExpression((LiteralExpressionSyntax)syntax);
                case SyntaxKind.NameExpression:
                    return BindNameExpression((NameExpressionSyntax)syntax);
                case SyntaxKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax)syntax);
                case SyntaxKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax)syntax);
                case SyntaxKind.CallExpression:
                    return BindCallExpression((CallExpressionSyntax)syntax);
                case SyntaxKind.ConstructorExpression:
                    return BindConstructorExpression((ConstructorExpressionSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
        {
            return BindExpression(syntax.Expression);
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            object value = syntax.Value ?? 0;
            return new BoundLiteralExpression(syntax, value);
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            string name = syntax.IdentifierToken.Text;
            if (syntax.IdentifierToken.IsMissing)
            {
                // This means the token was inserted by the parser. We already
                // reported error so we can just return an error expression.
                return new BoundErrorExpression(syntax);
            }

            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return new BoundErrorExpression(syntax);

            return new BoundVariableExpression(syntax, variable);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            BoundExpression boundOperand = BindExpression(syntax.Operand);

            if (boundOperand.Type == TypeSymbol.Error)
                return new BoundErrorExpression(syntax);

            BoundUnaryOperator? boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

            if (boundOperator is null)
            {
                _diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundOperand.Type);
                return new BoundErrorExpression(syntax);
            }

            return new BoundUnaryExpression(syntax, boundOperator, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            BoundExpression boundLeft = BindExpression(syntax.Left);
            BoundExpression boundRight = BindExpression(syntax.Right);

            if (boundLeft.Type == TypeSymbol.Error || boundRight.Type == TypeSymbol.Error)
                return new BoundErrorExpression(syntax);

            BoundBinaryOperator? boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            if (boundOperator is null)
            {
                _diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression(syntax);
            }

            return new BoundBinaryExpression(syntax, boundLeft, boundOperator, boundRight);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax)
        {
            if (syntax.Arguments.Count == 1 && LookupType(syntax.Identifier.Text) is TypeSymbol type)
                return BindConversion(syntax.Arguments[0], type, allowExplicit: true);

            ImmutableArray<BoundExpression>.Builder boundArguments = ImmutableArray.CreateBuilder<BoundExpression>();

            foreach (ExpressionSyntax argument in syntax.Arguments)
                boundArguments.Add(BindExpression(argument));

            Symbol? symbol = _scope.TryLookupSymbol(syntax.Identifier.Text);
            if (symbol is null)
            {
                _diagnostics.ReportUndefinedFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression(syntax);
            }

            if (symbol is not FunctionSymbol function)
            {
                _diagnostics.ReportNotAFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression(syntax);
            }

            if (syntax.Arguments.Count != function.Parameters.Length)
            {
                TextSpan span;
                if (syntax.Arguments.Count > function.Parameters.Length)
                {
                    SyntaxNode firstExceedingNode;
                    if (function.Parameters.Length > 0)
                        firstExceedingNode = syntax.Arguments.GetSeparator(function.Parameters.Length - 1);
                    else
                        firstExceedingNode = syntax.Arguments[0];
                    ExpressionSyntax lastExceedingArgument = syntax.Arguments[syntax.Arguments.Count - 1];
                    span = TextSpan.FromBounds(firstExceedingNode.Span.Start, lastExceedingArgument.Span.End);
                }
                else
                    span = syntax.CloseParenthesisToken.Span;

                TextLocation location = new TextLocation(syntax.SyntaxTree.Text, span);
                _diagnostics.ReportWrongArgumentCount(location, function.Name, function.Parameters.Length, syntax.Arguments.Count);
                return new BoundErrorExpression(syntax);
            }

            TypeSymbol? genericType = null;
            if (function.IsGeneric)
            {
                // TODO: once added, check for explicit generic type
                // try to inferred generic type from arguments
                for (int i = 0; i < function.Parameters.Length; i++)
                {
                    ParameterSymbol param = function.Parameters[i];
                    BoundExpression arg = boundArguments[i];

                    TypeSymbol? _genericType = null;
                    if (param.Type == TypeSymbol.Generic)
                        _genericType = arg.Type;
                    else if (param.Type!.IsGenericDefinition && arg.Type!.IsGenericInstance)
                        _genericType = arg.Type.InnerType;

                    if (_genericType is not null)
                    {
                        if (genericType is null)
                            genericType = _genericType;
                        else if (!genericType.GenericEquals(_genericType))
                        {
                            genericType = null;
                            break;
                        }
                    }
                }

                if (genericType is null)
                {
                    _diagnostics.ReportCannotInferrGenericType(syntax.Location);
                    return new BoundErrorExpression(syntax);
                }
                else if (genericType.IsGenericInstance)
                {
                    _diagnostics.ReportGenericTypeRecursion(syntax.Location);
                    return new BoundErrorExpression(syntax);
                }
                else if (!function.AllowedGenericTypes.Value.Contains(genericType))
                {
                    _diagnostics.ReportSpecificGenericTypeNotAllowed(syntax.Location, genericType, function.AllowedGenericTypes.Value);
                    return new BoundErrorExpression(syntax);
                }
            }

            for (int i = 0; i < syntax.Arguments.Count; i++)
            {
                TextLocation argumentLocation = syntax.Arguments[i].Location;
                BoundExpression argument = boundArguments[i];
                VariableSymbol parameter = function.Parameters[i];
                boundArguments[i] = BindConversion(argumentLocation, argument, fixType(parameter.Type));
            }

            return new BoundCallExpression(syntax, function, boundArguments.ToImmutable(), fixType(function.Type)!, genericType);

            TypeSymbol? fixType(TypeSymbol? type)
            {
                if (genericType is null || type is null) return type;
                else if (type == TypeSymbol.Generic) return genericType;
                else if (type.IsGenericDefinition) return TypeSymbol.CreateGenericInstance(type, genericType);
                else return type;
            }
        }

        private BoundExpression BindConstructorExpression(ConstructorExpressionSyntax syntax)
        {
            BoundExpression expressionX = BindExpression(syntax.ExpressionX, TypeSymbol.Float);
            BoundExpression expressionY = BindExpression(syntax.ExpressionY, TypeSymbol.Float);
            BoundExpression expressionZ = BindExpression(syntax.ExpressionZ, TypeSymbol.Float);

            TypeSymbol type = syntax.KeywordToken.Kind.ToTypeSymbol();

            if (expressionX.ConstantValue is not null && expressionY.ConstantValue is not null && expressionZ.ConstantValue is not null)
            {
                Vector3F val = new Vector3F((float)expressionX.ConstantValue.Value, (float)expressionY.ConstantValue.Value, (float)expressionZ.ConstantValue.Value);
                if (type == TypeSymbol.Rotation)
                    return new BoundLiteralExpression(syntax, new Rotation(val));
                else
                    return new BoundLiteralExpression(syntax, val);
            }

            return new BoundConstructorExpression(syntax, type, expressionX, expressionY, expressionZ);
        }

        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit = false)
        {
            BoundExpression expression = BindExpression(syntax);
            return BindConversion(syntax.Location, expression, type, allowExplicit);
        }

        private BoundExpression BindConversion(TextLocation diagnosticLocation, BoundExpression expression, TypeSymbol? type, bool allowExplicit = false)
        {
            Conversion conversion = Conversion.Classify(expression.Type, type);

            if (!conversion.Exists)
            {
                if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
                    _diagnostics.ReportCannotConvert(diagnosticLocation, expression.Type, type);

                return new BoundErrorExpression(expression.Syntax);
            }

            if (!allowExplicit && conversion.IsExplicit)
                _diagnostics.ReportCannotConvertImplicitly(diagnosticLocation, expression.Type, type);

            if (conversion.IsIdentity)
                return expression;

            return new BoundConversionExpression(expression.Syntax, type, expression);
        }

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, bool isReadOnly, TypeSymbol? type, BoundConstant? constant = null)
        {
            string name = identifier.Text ?? "?";
            bool declare = !identifier.IsMissing;
            VariableSymbol variable = _function is null
                                ? new GlobalVariableSymbol(name, isReadOnly, type, constant)
                                : new LocalVariableSymbol(name, isReadOnly, type, constant);

            if (declare && !_scope.TryDeclareVariable(variable))
                _diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);

            return variable;
        }

        private VariableSymbol? BindVariableReference(SyntaxToken identifierToken)
        {
            string name = identifierToken.Text;
            switch (_scope.TryLookupSymbol(name))
            {
                case VariableSymbol variable:
                    return variable;

                case null:
                    _diagnostics.ReportUndefinedVariable(identifierToken.Location, name);
                    return null;

                default:
                    _diagnostics.ReportNotAVariable(identifierToken.Location, name);
                    return null;
            }
        }

        private TypeSymbol? LookupType(string name)
        {
            TypeSymbol type = TypeSymbol.GetType(name);

            if (type == TypeSymbol.Error)
                return null;
            else
                return type;
        }
    }
}
