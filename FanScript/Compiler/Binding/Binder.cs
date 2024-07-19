﻿using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Lowering;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.FCInfo;
using FanScript.Utils;
using MathUtils.Vectors;
using System.Collections.Immutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                scriptFunction = new FunctionSymbol("^eval", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.Void, null);
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


        private static BoundScope CreateParentScope(BoundGlobalScope? previous, DiagnosticBag diagnostics)
        {
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
                case SyntaxKind.ArrayInitializerStatement:
                    return BindArrayInitializerStatement((ArrayInitializerStatementSyntax)syntax);
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
            SpecialBlockType type;
            ImmutableArray<ParameterSymbol> parameters;
            switch (syntax.Identifier.Text)
            {
                case "Play":
                    type = SpecialBlockType.Play;
                    parameters = [];
                    break;
                case "LateUpdate":
                    type = SpecialBlockType.LateUpdate;
                    parameters = [];
                    break;
                case "BoxArt":
                    type = SpecialBlockType.BoxArt;
                    parameters = [];
                    break;
                case "Touch":
                    type = SpecialBlockType.Touch;
                    parameters = [
                        new ParameterSymbol("screenX", Modifiers.Ref, TypeSymbol.Float, 0),    
                        new ParameterSymbol("screenY", Modifiers.Ref, TypeSymbol.Float, 1),    
                        new ParameterSymbol("TOUCH_STATE", TypeSymbol.Float, 2),    
                        new ParameterSymbol("TOUCH_INDEX", TypeSymbol.Float, 3),    
                    ];
                    break;
                case "Button":
                    type = SpecialBlockType.Button;
                    parameters = [new ParameterSymbol("BUTTON_TYPE", TypeSymbol.Float, 0)];
                    break;
                default:
                    _diagnostics.ReportUnknownSpecialBlock(syntax.Identifier.Location, syntax.Identifier.Text);
                    return BindErrorStatement(syntax);
            }

            BoundArgumentClause? argumentClause = BindArgumentClause(syntax.ArgumentClause, parameters, "SpecialBlock", syntax.Identifier.Text);

            if (argumentClause is null)
                return BindErrorStatement(syntax);

            BoundBlockStatement block = (BoundBlockStatement)BindBlockStatement(syntax.Block);
            return new BoundSpecialBlockStatement(syntax, type, argumentClause, block);
        }

        private BoundStatement BindVariableDeclaration(VariableDeclarationSyntax syntax)
        {
            TypeSymbol? type = BindTypeClause(syntax.TypeClause);

            TypeSymbol? variableType = type;
            VariableSymbol variable = BindVariableDeclaration(syntax.Identifier, syntax.Modifiers, variableType);

            if (variable.Modifiers.HasFlag(Modifiers.Constant) && syntax.OptionalAssignment is null)
                _diagnostics.ReportConstantNotInitialized(syntax.Identifier.Location);

            BoundStatement? optionalAssignment = syntax.OptionalAssignment is null ? null : BindStatement(syntax.OptionalAssignment);

            return new BoundVariableDeclaration(syntax, variable, optionalAssignment);
        }

        private BoundStatement BindAssignmentStatement(AssignmentStatementSyntax syntax)
        {
            string name = syntax.IdentifierToken.Text;
            BoundExpression boundExpression = BindExpression(syntax.Expression);

            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return BindErrorStatement(syntax);

            if (variable.IsReadOnly && variable.Initialized)
                _diagnostics.ReportCannotAssignReadOnly(syntax.AssignmentToken.Location, name);
            else if (variable.Modifiers.HasFlag(Modifiers.Constant) && boundExpression.ConstantValue is null)
                _diagnostics.ReportValueMustBeConstant(syntax.Expression.Location);

            variable.Initialize(boundExpression.ConstantValue);

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

        private BoundStatement BindArrayInitializerStatement(ArrayInitializerStatementSyntax syntax)
        {
            if (syntax.Elements.Count == 0)
            {
                _diagnostics.ReportEmptyArrayInitializer(syntax.Location);
                return BindErrorStatement(syntax);
            }

            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return BindErrorStatement(syntax);

            if (variable.IsReadOnly && variable.Initialized)
                _diagnostics.ReportCannotAssignReadOnly(syntax.EqualsToken.Location, variable.Name);

            variable.Initialize(null);

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

            return new BoundArrayInitializerStatement(syntax, variable, boundElements.ToImmutable());
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
                        return TypeSymbol.CreateGenericInstance(type, innerType);
                }
                else
                {
                    _diagnostics.ReportNotAGenericType(syntax.Location);
                    return TypeSymbol.Error;
                }
            }
            else if (type.IsGenericDefinition)
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

            ImmutableArray<BoundExpression>.Builder boundArguments = ImmutableArray.CreateBuilder<BoundExpression>(syntax.Arguments.Count);

            foreach (ExpressionSyntax argument in syntax.Arguments)
                boundArguments.Add(BindExpression(argument));

            FunctionSymbol? function = _scope.TryLookupFunction(syntax.Identifier.Text, boundArguments.Select(arg => arg.Type!).ToList());

            if (function is null)
            {
                _diagnostics.ReportUndefinedFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression(syntax);
            }

            TypeSymbol? genericType = null;
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

            BoundArgumentClause? argumentClause = BindArgumentClause(syntax.ArgumentClause, 
                function.Parameters
                    .Select(param => new ParameterSymbol(param.Name, param.Modifiers, fixType(param.Type), param.Ordinal))
                    .ToImmutableArray(),
                "Function", function.Name, boundArguments);

            if (argumentClause is null)
                return new BoundErrorExpression(syntax);

            return new BoundCallExpression(syntax, function, argumentClause, fixType(function.Type)!, genericType);

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

        private BoundExpression BindConversion(TextLocation diagnosticLocation, BoundExpression expression, TypeSymbol type, bool allowExplicit = false)
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

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, ImmutableArray<SyntaxToken> modifierArray, TypeSymbol type)
        {
            string name = identifier.Text ?? "?";
            bool declare = !identifier.IsMissing;

            Modifiers validModifiers = Modifiers.Readonly;
            if (type.GenericEquals(TypeSymbol.Bool) ||
                type.GenericEquals(TypeSymbol.Float) ||
                type.GenericEquals(TypeSymbol.Vector3) ||
                type.GenericEquals(TypeSymbol.Rotation))
                validModifiers |= Modifiers.Constant;

            Modifiers modifiers = BindModifiers(modifierArray, ModifierTarget.Variable, item =>
            {
                var (modifier, token) = item;

                bool valid = validModifiers.HasFlag(modifier);
                if (!valid)
                    _diagnostics.ReportInvalidModifierOnType(token.Location, modifier, type);

                return valid;
            });

            VariableSymbol variable = _function is null
                                ? new GlobalVariableSymbol(name, modifiers, type)
                                : new LocalVariableSymbol(name, modifiers, type);

            if (declare && !_scope.TryDeclareVariable(variable))
                _diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);
            else if (name.Length > FancadeConstants.MaxVariableNameLength)
                _diagnostics.ReportVariableNameTooLong(identifier.Location, name);

            return variable;
        }

        private VariableSymbol? BindVariableReference(SyntaxToken identifierToken)
        {
            string name = identifierToken.Text;
            switch (_scope.TryLookupVariable(name))
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

        private BoundArgumentClause? BindArgumentClause(ArgumentClauseSyntax syntax, ImmutableArray<ParameterSymbol> parameters, string type, string name, ImmutableArray<BoundExpression>.Builder? boundArguments = null)
        {
            if (boundArguments is null)
            {
                boundArguments = ImmutableArray.CreateBuilder<BoundExpression>(syntax.Arguments.Count);

                foreach (ExpressionSyntax argument in syntax.Arguments)
                    boundArguments.Add(BindExpression(argument));
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
                    ExpressionSyntax lastExceedingArgument = syntax.Arguments[syntax.Arguments.Count - 1];
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
                TextLocation argumentLocation = syntax.Arguments[i].Location;
                BoundExpression argument = boundArguments[i];
                ParameterSymbol parameter = parameters[i];

                Modifiers modifiers = BindModifiers(syntax.ArgumentModifiers[i], ModifierTarget.Parameter, item =>
                {
                    var (modifier, token) = item;

                    bool valid = modifier == Modifiers.Ref ? parameter.Modifiers.HasFlag(Modifiers.Ref) : true;

                    if (!valid)
                        _diagnostics.ReportArgumentCannotHaveModifier(token.Location, parameter.Name, Modifiers.Ref);

                    return valid;
                });

                argModifiersBuilder.Add(modifiers);

                if (parameter.Modifiers.HasFlag(Modifiers.Ref) && !modifiers.HasFlag(Modifiers.Ref))
                    _diagnostics.ReportArgumentMustHaveModifier(argumentLocation, parameter.Name, Modifiers.Ref);
                else if (modifiers.HasFlag(Modifiers.Ref) && (argument is not BoundVariableExpression variable || variable.Variable.IsReadOnly))
                    _diagnostics.ReportRefMustBeVariable(argumentLocation);

                boundArguments[i] = BindConversion(argumentLocation, argument, parameter.Type);
            }

            return new BoundArgumentClause(syntax, argModifiersBuilder.ToImmutable(), boundArguments.ToImmutable());
        }

        private Modifiers BindModifiers(IEnumerable<SyntaxToken> tokens, ModifierTarget target, Func<(Modifiers, SyntaxToken), bool>? checkModifier = null)
        {
            if (tokens.Count() == 0)
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
    }
}
