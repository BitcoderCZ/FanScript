using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Lowering;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.LangServer")]
namespace FanScript.Compiler.Binding
{
    internal sealed class Binder
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly bool _isScript;
        private readonly FunctionSymbol? _function;

        private Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _loopStack = new Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)>();
        private int onStatementDepth = 0;
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

            DiagnosticBag scopeDiagnostics = new DiagnosticBag();

            BoundScope parentScope = CreateParentScope(previous, scopeDiagnostics);
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
                scriptFunction = new FunctionSymbol("^^eval", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, null);
            else
                scriptFunction = null;

            ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.Concat(scopeDiagnostics).ToImmutableArray();
            ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVariables();

            if (previous is not null)
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);

            return new BoundGlobalScope(previous, diagnostics, scriptFunction, functions, variables, statements.ToImmutable());
        }

        public static BoundProgram BindProgram(bool isScript, BoundProgram? previous, BoundGlobalScope globalScope)
        {
            DiagnosticBag scopeDiagnostics = new DiagnosticBag();
            BoundScope parentScope = CreateParentScope(globalScope, scopeDiagnostics);

            if (globalScope.Diagnostics.HasErrors())
                return new BoundProgram(previous, globalScope.Diagnostics, null, ImmutableDictionary<FunctionSymbol, BoundBlockStatement>.Empty);

            ImmutableDictionary<FunctionSymbol, BoundBlockStatement>.Builder functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();
            ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            diagnostics.AddRange(scopeDiagnostics);

            int varDistinguisher = 0;

            foreach (FunctionSymbol function in globalScope.Functions)
            {
                Binder binder = new Binder(isScript, parentScope, function);

                BoundStatement body = binder.BindStatement(function.Declaration!.Body);

                BoundBlockStatement loweredBody = Lowerer.Lower(function, body);

                if (function.Type != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                    binder._diagnostics.ReportAllPathsMustReturn(function.Declaration.Identifier.Location);

                if (function != globalScope.ScriptFunction)
                {
                    ImmutableArray<VariableSymbol> vars = binder._scope.GetDeclaredVariables();

                    foreach (var @var in vars)
                        @var.MakeUnique(varDistinguisher);

                    if (vars.Length > 0)
                        varDistinguisher++;
                }

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
                Modifiers modifiers = BindModifiers(parameterSyntax.Modifiers, ModifierTarget.Argument);
                TypeSymbol parameterType = BindTypeClause(parameterSyntax.TypeClause);
                string parameterName = parameterSyntax.Identifier.Text;

                if (!seenParameterNames.Add(parameterName))
                    _diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, parameterName);
                else
                    parameters.Add(new ParameterSymbol(parameterName, modifiers, parameterType));
            }

            TypeSymbol type = syntax.TypeClause is null ? TypeSymbol.Void : BindTypeClause(syntax.TypeClause);

            if (type != TypeSymbol.Void)
                _diagnostics.ReportFeatureNotImplemented(syntax.TypeClause?.Location ?? TextLocation.None, "Non void functions");

            FunctionSymbol function = new FunctionSymbol(syntax.Identifier.Text, parameters.ToImmutable(), type, syntax);
            if (syntax.Identifier.Text is not null &&
                !_scope.TryDeclareFunction(function))
                _diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, function.Name);
        }


        private static BoundScope CreateParentScope(BoundGlobalScope? previous, DiagnosticBag diagnostics)
        {
            RuntimeHelpers.RunClassConstructor(typeof(TypeSymbol).TypeHandle);

            Stack<BoundGlobalScope> stack = new Stack<BoundGlobalScope>();
            while (previous is not null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope parent = CreateRootScope(diagnostics);

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

        private static BoundScope CreateRootScope(DiagnosticBag diagnostics)
        {
            BoundScope result = new BoundScope(null);

            foreach (Constant con in Constants.GetAll())
            {
                VariableSymbol variable = new LocalVariableSymbol(con.Name, Modifiers.Constant, con.Type);
                variable.Initialize(new BoundConstant(con.Value));
                if (!result.TryDeclareVariable(variable))
                    diagnostics.ReportFailedToDeclare(TextLocation.None, "Constant", variable.Name);
            }

            foreach (FunctionSymbol f in BuiltinFunctions.GetAll())
                if (!result.TryDeclareFunction(f))
                    diagnostics.ReportFailedToDeclare(TextLocation.None, "Built in function", f.Name);

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
            syntax.BoundResult = result;

            //if (!_isScript || !isGlobal)
            //{
            if (result is BoundExpressionStatement es)
            {
                bool isAllowedExpression = es.Expression.Kind == BoundNodeKind.ErrorExpression ||
                                          es.Expression.Type == TypeSymbol.Void;
                if (!isAllowedExpression)
                    _diagnostics.ReportInvalidExpressionStatement(syntax.Location);
            }
            //}

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
                case SyntaxKind.PostfixStatement:
                    return BindPostfixStatement((PostfixStatementSyntax)syntax);
                case SyntaxKind.VariableDeclarationStatement:
                    return BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)syntax);
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
                case SyntaxKind.ReturnStatement:
                    return BindReturnStatement((ReturnStatementSyntax)syntax);
                case SyntaxKind.ExpressionStatement:
                    return BindExpressionStatement((ExpressionStatementSyntax)syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            ImmutableArray<BoundStatement>.Builder statements = ImmutableArray.CreateBuilder<BoundStatement>();
            //_scope = new BoundScope(_scope);

            foreach (StatementSyntax statementSyntax in syntax.Statements)
            {
                BoundStatement statement = BindStatement(statementSyntax);
                statements.Add(statement);
            }

            //_scope = _scope.Parent!;

            return new BoundBlockStatement(syntax, statements.ToImmutable());
        }

        private BoundStatement BindSpecialBlockStatement(SpecialBlockStatementSyntax syntax)
        {
            if (!Enum.TryParse(syntax.Identifier.Text, out SpecialBlockType type))
            {
                _diagnostics.ReportUnknownSpecialBlock(syntax.Identifier.Location, syntax.Identifier.Text);
                return BindErrorStatement(syntax);
            }

            ImmutableArray<ParameterSymbol> parameters = type
                .GetInfo()
                .Parameters
                .Select(param => param.ToParameter())
                .ToImmutableArray();

            BoundArgumentClause? argumentClause = syntax.ArgumentClause is null ? null : BindArgumentClause(syntax.ArgumentClause, parameters, "SpecialBlock", syntax.Identifier.Text);

            if (argumentClause is null && parameters.Length != 0)
            {
                if (syntax.ArgumentClause is null)
                    _diagnostics.ReportSBMustHaveArguments(syntax.Identifier.Location, syntax.Identifier.Text);
                return BindErrorStatement(syntax);
            }

            onStatementDepth++;
            BoundBlockStatement block = (BoundBlockStatement)BindBlockStatement(syntax.Block);
            onStatementDepth--;

            return new BoundSpecialBlockStatement(syntax, type, argumentClause, block);
        }

        private BoundStatement BindPostfixStatement(PostfixStatementSyntax syntax)
        {
            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return BindErrorStatement(syntax);

            if (variable.IsReadOnly)
                _diagnostics.ReportCannotAssignReadOnlyVariable(syntax.OperatorToken.Location, variable.Name);

            BoundPostfixKind kind;
            switch (syntax.OperatorToken.Kind)
            {
                case SyntaxKind.PlusPlusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = BoundPostfixKind.Increment;
                    break;
                case SyntaxKind.MinusMinusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = BoundPostfixKind.Decrement;
                    break;
                default:
                    _diagnostics.ReportUndefinedPostfixOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, variable.Type);
                    return BindErrorStatement(syntax);
            }

            return new BoundPostfixStatement(syntax, variable, kind);
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            TypeSymbol? type = BindTypeClause(syntax.TypeClause);

            TypeSymbol? variableType = type;
            VariableSymbol variable = BindVariableDeclaration(syntax.IdentifierToken, syntax.Modifiers, variableType);

            if (variable.IsReadOnly && syntax.OptionalAssignment is null)
                _diagnostics.ReportConstantNotInitialized(syntax.IdentifierToken.Location);

            BoundStatement? optionalAssignment = syntax.OptionalAssignment is null ? null : BindStatement(syntax.OptionalAssignment);

            return new BoundVariableDeclarationStatement(syntax, variable, optionalAssignment);
        }

        private BoundStatement BindAssignmentStatement(AssignmentStatementSyntax syntax)
        {
            BoundExpression boundExpression = BindExpression(syntax.Expression);

            VariableSymbol? variable;
            switch (syntax.AssignableClause)
            {
                case AssignableVariableClauseSyntax varClause:
                    {
                        variable = BindVariableReference(varClause.IdentifierToken);
                        if (variable is null)
                            return BindErrorStatement(syntax);

                        if (variable.IsReadOnly && variable.Initialized)
                            _diagnostics.ReportCannotAssignReadOnlyVariable(syntax.AssignmentToken.Location, variable.Name);
                        else if (variable.Modifiers.HasFlag(Modifiers.Constant) && boundExpression.ConstantValue is null)
                            _diagnostics.ReportValueMustBeConstant(syntax.Expression.Location);

                        variable.Initialize(boundExpression.ConstantValue);

                        varClause.BoundResult = variable;
                    }
                    break;
                case AssignablePropertyClauseSyntax propertyClause:
                    {
                        VariableSymbol? baseVariable = BindVariableReference(propertyClause.VariableToken);
                        if (baseVariable is null)
                            return BindErrorStatement(syntax);

                        PropertyDefinitionSymbol? property = baseVariable.Type.GetProperty(propertyClause.IdentifierToken.Text);

                        if (property is null)
                        {
                            _diagnostics.ReportUndefinedProperty(propertyClause.IdentifierToken.Location, baseVariable.Type, propertyClause.IdentifierToken.Text);
                            return BindErrorStatement(syntax);
                        }

                        if (property.IsReadOnly)
                            _diagnostics.ReportCannotAssignReadOnlyProperty(syntax.AssignmentToken.Location, property.Name);

                        variable = new PropertySymbol(property, new BoundVariableExpression(propertyClause.VariableToken, baseVariable));

                        propertyClause.BoundResult = variable;
                    }
                    break;
                default:
                    throw new InvalidDataException($"Unknown {nameof(AssignableClauseSyntax)} '{syntax.AssignableClause.GetType()}'");
            }

            if (syntax.AssignmentToken.Kind != SyntaxKind.EqualsToken)
            {
                SyntaxKind equivalentOperatorTokenKind = SyntaxFacts.GetBinaryOperatorOfAssignmentOperator(syntax.AssignmentToken.Kind);
                BoundBinaryOperator? boundOperator = BoundBinaryOperator.Bind(equivalentOperatorTokenKind, variable.Type, boundExpression.Type);

                if (boundOperator is null)
                {
                    _diagnostics.ReportUndefinedBinaryOperator(syntax.AssignmentToken.Location, syntax.AssignmentToken.Text, variable.Type, boundExpression.Type);
                    return BindErrorStatement(syntax);
                }

                BoundExpression convertedExpression = bindConversion(boundOperator.RightType);

                return new BoundCompoundAssignmentStatement(syntax, variable, boundOperator, convertedExpression);
            }
            else
            {
                BoundExpression convertedExpression = bindConversion(variable.Type);
                return new BoundAssignmentStatement(syntax, variable, convertedExpression);
            }

            BoundExpression bindConversion(TypeSymbol targetType)
            {
                if (boundExpression.Type.NonGenericEquals(TypeSymbol.ArraySegment) && targetType.NonGenericEquals(TypeSymbol.Array) && boundExpression.Type.InnerType == targetType.InnerType)
                    return boundExpression;
                else
                    return BindConversion(syntax.Expression.Location, boundExpression, targetType);
            }
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition, TypeSymbol.Bool);

            if (condition.ConstantValue is not null)
            {
                if ((bool)condition.ConstantValue.GetValueOrDefault(TypeSymbol.Bool) == false)
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
                if (!(bool)condition.ConstantValue.GetValueOrDefault(TypeSymbol.Bool))
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
            else if (onStatementDepth != 0)
            {
                _diagnostics.ReportBreakOrContinueInSB(syntax.Keyword.Location, syntax.Keyword.Text);
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
            else if (onStatementDepth != 0)
            {
                _diagnostics.ReportBreakOrContinueInSB(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }

            BoundLabel continueLabel = _loopStack.Peek().ContinueLabel;
            return new BoundGotoStatement(syntax, continueLabel);
        }

        private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
        {
            var expression = syntax.Expression is null ? null : BindExpression(syntax.Expression);

            if (_function is null)
            {
                if (expression is not null)
                {
                    // Main does not support return values.
                    _diagnostics.ReportInvalidReturnWithValueInGlobalStatements(syntax.Expression!.Location);
                }
            }
            else
            {
                if (_function.Type == TypeSymbol.Void)
                {
                    if (expression is not null)
                        _diagnostics.ReportInvalidReturnExpression(syntax.Expression!.Location, _function.Name);
                }
                else
                {
                    if (expression is null)
                        _diagnostics.ReportMissingReturnExpression(syntax.ReturnKeyword.Location, _function.Type);
                    else
                        expression = BindConversion(syntax.Expression!.Location, expression, _function.Type);
                }
            }

            return new BoundReturnStatement(syntax, expression);
        }

        private TypeSymbol BindTypeClause(TypeClauseSyntax? syntax)
        {
            if (syntax is null)
                return TypeSymbol.Error;

            TypeSymbol? type = LookupType(syntax.TypeToken.Text);
            if (type is null)
            {
                _diagnostics.ReportUndefinedType(syntax.Location, syntax.TypeToken.Text);
                type = TypeSymbol.Error;
            }

            if (syntax.HasGenericParameter)
            {
                if (type.IsGenericDefinition)
                {
                    TypeSymbol? innerType = BindTypeClause(syntax.InnerType);
                    if (innerType is null)
                        return TypeSymbol.Error;
                    else
                    {
                        TypeSymbol resultType = TypeSymbol.CreateGenericInstance(type, innerType);
                        syntax.BoundResult = resultType;
                        return resultType;
                    }
                }
                else
                {
                    _diagnostics.ReportNotAGenericType(new TextLocation(syntax.SyntaxTree.Text, TextSpan.FromBounds(syntax.LessToken.Span.Start, syntax.GreaterToken.Span.End)));
                    return TypeSymbol.Error;
                }
            }
            else if (type.IsGenericDefinition)
            {
                _diagnostics.ReportTypeMustHaveGenericParameter(syntax.Location);
                return TypeSymbol.Error;
            }

            syntax.BoundResult = type;
            return type;
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            BoundExpression expression = BindExpression(syntax.Expression, canBeVoid: true);
            return new BoundExpressionStatement(syntax, expression);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, TypeSymbol targetType, Context? context = null)
        {
            return BindConversion(syntax, targetType, context: context);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, bool canBeVoid = false, Context? context = null)
        {
            BoundExpression result = BindExpressionInternal(syntax, context);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                _diagnostics.ReportExpressionMustHaveValue(syntax.Location);
                return new BoundErrorExpression(syntax);
            }

            syntax.BoundResult ??= result;

            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax syntax, Context? context = null)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.ParenthesizedExpression:
                    return BindParenthesizedExpression((ParenthesizedExpressionSyntax)syntax);
                case SyntaxKind.LiteralExpression:
                    return BindLiteralExpression((LiteralExpressionSyntax)syntax);
                case SyntaxKind.NameExpression:
                    return BindNameExpression((NameExpressionSyntax)syntax, context);
                case SyntaxKind.VariableDeclarationExpression:
                    return BindVariableDeclarationExpression((VariableDeclarationExpressionSyntax)syntax);
                case SyntaxKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax)syntax);
                case SyntaxKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax)syntax);
                case SyntaxKind.CallExpression:
                    return BindCallExpression((CallExpressionSyntax)syntax, context);
                case SyntaxKind.ConstructorExpression:
                    return BindConstructorExpression((ConstructorExpressionSyntax)syntax);
                case SyntaxKind.PropertyExpression:
                    return BindPropertyExpression((PropertyExpressionSyntax)syntax);
                case SyntaxKind.ArraySegmentExpression:
                    return BindArraySegmentExpression((ArraySegmentExpressionSyntax)syntax);
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
            return new BoundLiteralExpression(syntax, syntax.Value);
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax, Context? context = null)
        {
            string name = syntax.IdentifierToken.Text;
            if (syntax.IdentifierToken.IsMissing)
            {
                // This means the token was inserted by the parser. We already
                // reported error so we can just return an error expression.
                return new BoundErrorExpression(syntax);
            }

            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken, context);
            if (variable is null)
                return new BoundErrorExpression(syntax);

            var res = new BoundVariableExpression(syntax, variable);
            syntax.BoundResult = res;
            return res;
        }

        private BoundExpression BindVariableDeclarationExpression(VariableDeclarationExpressionSyntax syntax)
        {
            TypeSymbol? type = BindTypeClause(syntax.TypeClause);

            TypeSymbol? variableType = type;
            VariableSymbol variable = BindVariableDeclaration(syntax.IdentifierToken, syntax.Modifiers, variableType);

            if (variable.Modifiers.HasFlag(Modifiers.Constant))
                _diagnostics.ReportConstantNotInitialized(syntax.IdentifierToken.Location);

            var res = new BoundVariableExpression(syntax, variable);
            syntax.BoundResult = res;
            return res;
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

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, Context? context = null)
        {
            if (syntax.Arguments.Count == 1 && LookupType(syntax.Identifier.Text) is TypeSymbol type)
                return BindConversion(syntax.Arguments[0].Expression, type, allowExplicit: true);

            context ??= Context.Default;

            var arguments = syntax.Arguments;
            if (context.MethodObject is not null)
            {
                arguments = new SeparatedSyntaxList<ModifierClauseSyntax>(
                    new SyntaxNode[] {
                        new ModifierClauseSyntax(context.MethodObject.SyntaxTree, [], context.MethodObject),
                        new SyntaxToken(context.MethodObject.SyntaxTree, SyntaxKind.CommaToken, context.MethodObject.Span.Start, null, null, [], []),
                    }.Concat(arguments.GetWithSeparators())
                    .ToImmutableArray()
                );
            }

            ImmutableArray<BoundExpression>.Builder boundArguments = ImmutableArray.CreateBuilder<BoundExpression>(arguments.Count);

            foreach (ModifierClauseSyntax argument in arguments)
                boundArguments.Add(BindExpression(argument.Expression, context: new Context()
                {
                    OutArgument = argument.Modifiers.Any(token => token.Kind == SyntaxKind.OutModifier)
                }));

            FunctionSymbol? function = _scope.TryLookupFunction(syntax.Identifier.Text, boundArguments.Select(arg => arg.Type!).ToList(), context.MethodObject is not null);

            if (function is null)
            {
                _diagnostics.ReportUndefinedFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression(syntax);
            }

            TypeSymbol? genericType = null;
            if (function.Parameters.Length == boundArguments.Count)
            {
                if (function.IsGeneric)
                {
                    if (syntax.HasGenericParameter)
                        genericType = BindTypeClause(syntax.GenericTypeClause);
                    else
                    {
                        // try to infer generic type from arguments
                        for (int i = 0; i < function.Parameters.Length; i++)
                        {
                            ParameterSymbol param = function.Parameters[i];
                            BoundExpression arg = boundArguments[i];

                            TypeSymbol? _genericType = null;
                            if (param.Type == TypeSymbol.Generic)
                                _genericType = arg.Type;
                            else if (param.Type!.IsGenericDefinition && arg.Type!.IsGenericInstance)
                                _genericType = arg.Type.InnerType;

                            if (_genericType is not null && _genericType != TypeSymbol.Null)
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
                    }

                    if (genericType is null)
                    {
                        _diagnostics.ReportCannotInferGenericType(syntax.Location);
                        return new BoundErrorExpression(syntax);
                    }
                    else if (genericType.IsGenericInstance)
                    {
                        _diagnostics.ReportGenericTypeRecursion(syntax.HasGenericParameter ? syntax.GenericTypeClause.Location : syntax.Location);
                        return new BoundErrorExpression(syntax);
                    }
                    else if (!function.AllowedGenericTypes.Value.Contains(genericType))
                    {
                        _diagnostics.ReportSpecificGenericTypeNotAllowed(syntax.HasGenericParameter ? syntax.GenericTypeClause.Location : syntax.Location, genericType, function.AllowedGenericTypes.Value);
                        return new BoundErrorExpression(syntax);
                    }
                }
                else if (syntax.HasGenericParameter)
                {
                    _diagnostics.ReportNonGenericMethodTypeArguments(new TextLocation(syntax.SyntaxTree.Text, TextSpan.FromBounds(syntax.LessThanToken.Span.Start, syntax.GreaterThanToken.Span.End)));
                    return new BoundErrorExpression(syntax);
                }
            }

            BoundArgumentClause? argumentClause = BindArgumentClause(new ArgumentClauseSyntax(syntax.ArgumentClause.SyntaxTree, syntax.ArgumentClause.OpenParenthesisToken, arguments, syntax.ArgumentClause.CloseParenthesisToken),
                function.Parameters
                    .Select(param => new ParameterSymbol(param.Name, param.Modifiers, fixType(param.Type)))
                    .ToImmutableArray(),
                "Function", function.Name, boundArguments);

            if (argumentClause is null)
            {
                syntax.BoundResult = new BoundCallExpression(syntax, function, null!, fixType(function.Type)!, genericType);
                return new BoundErrorExpression(syntax);
            }

            var res = new BoundCallExpression(syntax, function, argumentClause, fixType(function.Type)!, genericType);
            syntax.BoundResult = res;
            return res;

            TypeSymbol fixType(TypeSymbol type)
            {
                if (type == TypeSymbol.Generic) return genericType ?? TypeSymbol.Error;
                else if (type.IsGenericDefinition) return TypeSymbol.CreateGenericInstance(type, genericType ?? TypeSymbol.Error);
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
                Vector3F val = new Vector3F((float)expressionX.ConstantValue.GetValueOrDefault(TypeSymbol.Float), (float)expressionY.ConstantValue.GetValueOrDefault(TypeSymbol.Float), (float)expressionZ.ConstantValue.GetValueOrDefault(TypeSymbol.Float));
                if (type == TypeSymbol.Rotation)
                    return new BoundLiteralExpression(syntax, new Rotation(val));
                else
                    return new BoundLiteralExpression(syntax, val);
            }

            return new BoundConstructorExpression(syntax, type, expressionX, expressionY, expressionZ);
        }

        private BoundExpression BindPropertyExpression(PropertyExpressionSyntax syntax)
        {
            BoundExpression baseEx = BindExpression(syntax.BaseExpression);

            switch (syntax.Expression.Kind)
            {
                case SyntaxKind.NameExpression:
                    {
                        NameExpressionSyntax nameEx = (NameExpressionSyntax)syntax.Expression;

                        PropertyDefinitionSymbol? property = baseEx.Type.GetProperty(nameEx.IdentifierToken.Text);
                        if (property is null)
                        {
                            _diagnostics.ReportUndefinedProperty(nameEx.IdentifierToken.Location, baseEx.Type, nameEx.IdentifierToken.Text);
                            syntax.BoundResult = new BoundVariableExpression(syntax, new PropertySymbol(new PropertyDefinitionSymbol("?", TypeSymbol.Error, null!), baseEx));
                            return new BoundErrorExpression(syntax);
                        }

                        var res = new BoundVariableExpression(syntax, new PropertySymbol(property, baseEx));
                        syntax.BoundResult = res;
                        return res;
                    }
                case SyntaxKind.CallExpression:
                    return BindCallExpression((CallExpressionSyntax)syntax.Expression, new Context()
                    {
                        MethodObject = syntax.BaseExpression,
                    });
                default:
                    Debug.Fail($"Invalid property expression: '{syntax.Expression.Kind}'");
                    return new BoundErrorExpression(syntax);
            }

        }

        private BoundExpression BindArraySegmentExpression(ArraySegmentExpressionSyntax syntax)
        {
            if (syntax.Elements.Count == 0)
            {
                _diagnostics.ReportEmptyArrayInitializer(new TextLocation(syntax.SyntaxTree.Text, TextSpan.FromBounds(syntax.OpenSquareToken.Span.Start, syntax.CloseSquareToken.Span.End)));
                return new BoundErrorExpression(syntax);
            }

            ImmutableArray<BoundExpression>.Builder boundElements = ImmutableArray.CreateBuilder<BoundExpression>(syntax.Elements.Count);

            foreach (ExpressionSyntax argument in syntax.Elements)
                boundElements.Add(BindExpression(argument));

            TypeSymbol type = boundElements[0].Type;

            for (int i = 0; i < syntax.Elements.Count; i++)
            {
                TextLocation elementLocation = syntax.Elements[i].Location;
                BoundExpression element = boundElements[i];
                boundElements[i] = BindConversion(elementLocation, element, type); // all elements must be of the same type
            }

            return new BoundArraySegmentExpression(syntax, type, boundElements.ToImmutable());
        }

        #region Helper Methods
        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit = false, Context? context = null)
        {
            BoundExpression expression = BindExpression(syntax, context: context);
            return BindConversion(syntax.Location, expression, type, allowExplicit);
        }

        private BoundExpression BindConversion(TextLocation diagnosticLocation, BoundExpression expression, TypeSymbol type, bool allowExplicit = false)
        {
            Conversion conversion = Conversion.Classify(expression.Type, type);

            if (!conversion.Exists)
            {
                if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
                    _diagnostics.ReportCannotConvert(diagnosticLocation, expression.Type, type);

                return new BoundErrorExpression(expression.Syntax);
            }

            if (!allowExplicit && conversion.Type == Conversion.TypeEnum.Explicit)
                _diagnostics.ReportCannotConvertImplicitly(diagnosticLocation, expression.Type, type);

            if (conversion.IsIdentity || conversion.Type == Conversion.TypeEnum.Direct)
                return expression;

            return new BoundConversionExpression(expression.Syntax, type, expression);
        }

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, ImmutableArray<SyntaxToken> modifierArray, TypeSymbol type)
        {
            string name = identifier.Text ?? "?";
            bool declare = !identifier.IsMissing;

            if (name == "_")
                _diagnostics.ReportInvalidName(identifier.Location, name);

            Modifiers validModifiers = ModifiersE.GetValidModifiersFor(ModifierTarget.Variable, type);

            Modifiers modifiers = BindModifiers(modifierArray, ModifierTarget.Variable, item =>
            {
                var (modifier, token) = item;

                bool valid = validModifiers.HasFlag(modifier);
                if (!valid)
                    _diagnostics.ReportInvalidModifierOnType(token.Location, modifier, type);

                return valid;
            });

            VariableSymbol variable = modifiers.HasFlag(Modifiers.Global) || modifiers.HasFlag(Modifiers.Saved)
                ? new GlobalVariableSymbol(name, modifiers, type)
                : new LocalVariableSymbol(name, modifiers, type);

            if (declare && !_scope.TryDeclareVariable(variable))
                _diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);
            else if (name.Length > FancadeConstants.MaxVariableNameLength && !modifiers.HasFlag(Modifiers.Constant))
                _diagnostics.ReportVariableNameTooLong(identifier.Location, name);

            return variable;
        }

        private VariableSymbol? BindVariableReference(SyntaxToken identifierToken, Context? context = null)
        {
            context ??= Context.Default;

            string name = identifierToken.Text;

            if (context.OutArgument && name == "_")
                return new NullVariableSymbol();

            switch (_scope.TryLookupVariable(name))
            {
                case VariableSymbol variable:
                    return variable;

                default:
                    _diagnostics.ReportUndefinedVariable(identifierToken.Location, name);
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

        private BoundArgumentClause? BindArgumentClause(ArgumentClauseSyntax syntax, ImmutableArray<ParameterSymbol> parameters, string type, string name, ImmutableArray<BoundExpression>.Builder? boundArguments = null)
        {
            if (boundArguments is null)
            {
                boundArguments = ImmutableArray.CreateBuilder<BoundExpression>(syntax.Arguments.Count);

                foreach (ModifierClauseSyntax argument in syntax.Arguments)
                    boundArguments.Add(BindExpression(argument.Expression, context: new Context()
                    {
                        OutArgument = argument.Modifiers.Any(token => token.Kind == SyntaxKind.OutModifier)
                    }));
            }

            if (syntax.Arguments.Count != parameters.Length)
            {
                TextSpan span;
                if (syntax.Arguments.Count > parameters.Length)
                {
                    SyntaxNode firstExceedingNode;
                    if (parameters.Length > 0)
                        firstExceedingNode = syntax.Arguments.GetSeparator(parameters.Length - 1);
                    else
                        firstExceedingNode = syntax.Arguments[0];
                    SyntaxNode lastExceedingArgument = syntax.Arguments[syntax.Arguments.Count - 1];
                    span = TextSpan.FromBounds(firstExceedingNode.Span.Start, lastExceedingArgument.Span.End);
                }
                else
                    span = syntax.CloseParenthesisToken.Span;

                TextLocation location = new TextLocation(syntax.SyntaxTree.Text, span);
                _diagnostics.ReportWrongArgumentCount(location, type, name, parameters.Length, syntax.Arguments.Count);
                return null;
            }

            var argModifiersBuilder = ImmutableArray.CreateBuilder<Modifiers>();

            for (int i = 0; i < syntax.Arguments.Count; i++)
            {
                TextLocation argumentLocation = syntax.Arguments[i].Expression.Location;
                BoundExpression argument = boundArguments[i];
                ParameterSymbol parameter = parameters[i];

                Modifiers modifiers = BindModifiers(syntax.Arguments[i].Modifiers, ModifierTarget.Argument, item =>
                {
                    var (modifier, token) = item;

                    bool valid = modifier == Modifiers.Ref ? parameter.Modifiers.HasFlag(Modifiers.Ref) : true &&
                        modifier == Modifiers.Out ? parameter.Modifiers.HasFlag(Modifiers.Out) : true;

                    if (!valid)
                        _diagnostics.ReportArgumentCannotHaveModifier(token.Location, parameter.Name, modifier);

                    return valid;
                });

                argModifiersBuilder.Add(modifiers);

                if (parameter.Modifiers.HasFlag(Modifiers.Ref) && !modifiers.HasFlag(Modifiers.Ref))
                    _diagnostics.ReportArgumentMustHaveModifier(argumentLocation, parameter.Name, Modifiers.Ref);
                else if (parameter.Modifiers.HasFlag(Modifiers.Out) && !modifiers.HasFlag(Modifiers.Out))
                    _diagnostics.ReportArgumentMustHaveModifier(argumentLocation, parameter.Name, Modifiers.Out);
                else if (modifiers.MakesTargetReference(out Modifiers? makesRefMod) && (argument is not BoundVariableExpression variable ||
                    (variable.Variable.IsReadOnly && argument.Syntax.Kind != SyntaxKind.VariableDeclarationExpression && variable.Variable is not NullVariableSymbol)))
                    _diagnostics.ReportByRefArgMustBeVariable(argumentLocation, makesRefMod.Value);

                boundArguments[i] = BindConversion(argumentLocation, argument, parameter.Type);
            }

            var result = new BoundArgumentClause(syntax, argModifiersBuilder.ToImmutable(), boundArguments.ToImmutable());

            syntax.BoundResult = result;

            return result;
        }

        private Modifiers BindModifiers(IEnumerable<SyntaxToken> tokens, ModifierTarget target, Func<(Modifiers, SyntaxToken), bool>? checkModifier = null)
        {
            if (!tokens.Any())
                return 0;

            checkModifier ??= item => true;

            List<(Modifiers Modifier, SyntaxToken Token)> modifiersAndTokens = tokens
                .Where(token =>
                {
                    // remove tokens that aren't modifiers
                    bool isModifier = token.Kind.IsModifier();

                    if (!isModifier)
                        _diagnostics.ReportNotAModifier(token.Location, token.Text);

                    return isModifier;
                })
                .Select(token => (ModifiersE.FromKind(token.Kind), token))
                .Where(item =>
                {
                    // remove modifiers not valid for this target
                    var (modifier, token) = item;
                    bool valid = modifier.GetTargets().Contains(target);

                    if (!valid)
                        _diagnostics.ReportInvalidModifier(token.Location, modifier, target, modifier.GetTargets());

                    return valid;
                })
                .Where(checkModifier)
                .ToList();

            IEnumerable<Modifiers> _modifiers = modifiersAndTokens.Select(item => item.Modifier);

            // remove conflicting modifiers
            for (int i = 0; i < modifiersAndTokens.Count; i++)
            {
                Modifiers? conflict = null;
                foreach (var _conflict in modifiersAndTokens[i].Modifier.GetConflictingModifiers())
                    if (_modifiers.Contains(_conflict))
                    {
                        conflict = _conflict;
                        break;
                    }

                if (conflict is not null)
                {
                    _diagnostics.ReportConflictingModifiers(modifiersAndTokens[i].Token.Location, conflict.Value, modifiersAndTokens[i].Modifier);

                    modifiersAndTokens.RemoveAt(i);
                    i--;
                }
            }

            // find missing required
            foreach (var (mod, token) in modifiersAndTokens)
            {
                var required = mod.GetRequiredModifiers();

                if (required.Count == 0)
                    continue;

                bool found = false;
                foreach (var requiredMod in required)
                    if (modifiersAndTokens.Where(item => item.Modifier == requiredMod).Any())
                    {
                        found = true;
                        break;
                    }

                if (!found)
                    _diagnostics.ReportMissignRequiredModifiers(token.Location, mod, required);
            }

            // construct the enum
            Modifiers modifiers = 0;

            foreach (var (modifier, token) in modifiersAndTokens)
            {
                if (modifiers.HasFlag(modifier))
                    _diagnostics.ReportDuplicateModifier(token.Location, modifier);
                else
                    modifiers |= modifier;
            }

            return modifiers;
        }
        #endregion

        private class Context
        {
            public static readonly Context Default = new Context();

            public bool OutArgument { get; init; }
            public ExpressionSyntax? MethodObject { get; init; }
        }
    }
}
