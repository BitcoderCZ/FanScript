﻿using FanScript.Compiler.Binding.Rewriters;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Exceptions;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Symbols.Variables;
using FanScript.Compiler.Syntax;
using FanScript.Compiler.Text;
using FanScript.FCInfo;
using MathUtils.Vectors;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FanScript.LangServer")]
namespace FanScript.Compiler.Binding
{
    internal sealed class Binder
    {
        // TODO: remove when importing/exporting is added
        private static readonly Namespace defaultNamespace = new Namespace("default");

        private readonly DiagnosticBag diagnostics = new DiagnosticBag();
        private readonly bool isScript;
        private readonly FunctionSymbol? function;

        private Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> loopStack = new Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)>();
        private Stack<bool> blockStack = new Stack<bool>(); // if true - is in an event, else is in a loop
        private int labelCounter;
        private BoundScope scope;

        private FunctionFactory functionFactory = new();

        private Binder(bool isScript, BoundScope? parent, FunctionSymbol? function)
        {
            scope = parent?.AddChild() ?? new BoundScope();
            this.isScript = isScript;
            this.function = function;

            if (function is not null)
            {
                foreach (ParameterSymbol p in function.Parameters)
                    scope.TryDeclareVariable(p);
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
                return new BoundGlobalScope(previous, binder.Diagnostics.ToImmutableArray(), null, ImmutableArray<FunctionSymbol>.Empty, ImmutableArray<VariableSymbol>.Empty, ImmutableArray<BoundStatement>.Empty, new ScopeWSpan());

            IEnumerable<FunctionDeclarationSyntax> functionDeclarations = syntaxTrees.SelectMany(st => st.Root.Members)
                                                  .OfType<FunctionDeclarationSyntax>();

            foreach (FunctionDeclarationSyntax function in functionDeclarations)
                binder.BindFunctionDeclaration(function);

            IEnumerable<GlobalStatementSyntax> globalStatements = syntaxTrees.SelectMany(st => st.Root.Members)
                                              .OfType<GlobalStatementSyntax>();

            ImmutableArray<BoundStatement>.Builder statements = ImmutableArray.CreateBuilder<BoundStatement>();

            binder.enterScope();
            foreach (GlobalStatementSyntax globalStatement in globalStatements)
            {
                BoundStatement statement = binder.BindGlobalStatement(globalStatement.Statement);
                statements.Add(statement);
            }
            binder.exitScope();

            // Check global statements
            GlobalStatementSyntax[] firstGlobalStatementPerSyntaxTree = syntaxTrees
                .Select(st => st.Root.Members.OfType<GlobalStatementSyntax>().FirstOrDefault())
                .Where(g => g is not null)
                .ToArray()!;

            if (firstGlobalStatementPerSyntaxTree.Length > 1)
                foreach (GlobalStatementSyntax globalStatement in firstGlobalStatementPerSyntaxTree)
                    binder.Diagnostics.ReportOnlyOneFileCanHaveGlobalStatements(globalStatement.Location);

            // Check for main/script with global statements

            ImmutableArray<FunctionSymbol> functions = binder.scope.GetDeclaredFunctions();

            FunctionSymbol? scriptFunction;

            if (globalStatements.Any())
                scriptFunction = binder.functionFactory.Create(defaultNamespace, 0, TypeSymbol.Void, "^^eval", ImmutableArray<ParameterSymbol>.Empty, null);
            else
                scriptFunction = null;

            ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.Concat(scopeDiagnostics).ToImmutableArray();
            ImmutableArray<VariableSymbol> variables = binder.scope.GetAllDeclaredVariables();

            if (previous is not null)
                diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);

            return new BoundGlobalScope(previous, diagnostics, scriptFunction, functions, variables, statements.ToImmutable(), binder.scope.GetWithSpan());
        }

        public static BoundProgram BindProgram(bool isScript, BoundProgram? previous, BoundGlobalScope globalScope)
        {
            DiagnosticBag diagnostics = new DiagnosticBag();
            BoundScope parentScope = CreateParentScope(globalScope, diagnostics);

            ImmutableDictionary<FunctionSymbol, BoundBlockStatement>.Builder functionBodies = ImmutableDictionary.CreateBuilder<FunctionSymbol, BoundBlockStatement>();

            diagnostics.AddRange(globalScope.Diagnostics);

            BoundAnalysisResult analysisResult = new BoundAnalysisResult();
            Dictionary<FunctionSymbol, ScopeWSpan> functionScopes = new();
            BoundTreeVariableRenamer.Continuation? continuation = null;

            foreach (FunctionSymbol function in globalScope.Functions)
            {
                Binder binder = new Binder(isScript, parentScope, function);

                BoundStatement _body = binder.BindStatement(function.Declaration!.Body);
                BoundBlockStatement body = _body is BoundBlockStatement block ? block : new BoundBlockStatement(_body.Syntax, [_body]);

                functionScopes.Add(function, binder.scope.GetWithSpan());

                analysisResult.Add(BoundTreeAnalyzer.Analyze(body, function));

                functionBodies.Add(function, BoundTreeVariableRenamer.RenameVariables(body, function, ref continuation));

                diagnostics.AddRange(binder.Diagnostics);
            }

            SyntaxNode? compilationUnit = globalScope.Statements.Any()
                ? globalScope.Statements.First().Syntax.AncestorsAndSelf().LastOrDefault()
                : null;

            // add the script function
            if (globalScope.ScriptFunction is not null)
            {
                BoundBlockStatement body = new BoundBlockStatement(compilationUnit!, globalScope.Statements);
                functionBodies.Add(globalScope.ScriptFunction, body);
                functionScopes.Add(globalScope.ScriptFunction, globalScope.Scope);

                analysisResult.Add(BoundTreeAnalyzer.Analyze(body, globalScope.ScriptFunction));
            }

            // lower the bodies of all functions
            foreach (var func in functionBodies.Keys.ToList())
            {
                BoundBlockStatement loweredBody = Lowerer.Lower(func, functionBodies[func]);
                functionBodies[func] = loweredBody;

                if (func.Declaration is not null && func.Type != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                    diagnostics.ReportAllPathsMustReturn(func.Declaration.Identifier.Location);
            }

            if (analysisResult.HasCircularCalls(out var circularCall))
            {
                diagnostics.ReportCircularCall(TextLocation.None, circularCall);
            }
            else
            {
                // inline function calls
                Inliner.Continuation? inlineContinuation = null;

                foreach (var func in analysisResult
                    .EnumerateFunctionsInReverse()
                    .Concat([globalScope.ScriptFunction!]))
                {
                    if (func is null)
                        continue;

                    BoundBlockStatement inlinedBody = Inliner.Inline(functionBodies[func], analysisResult, functionBodies.ToImmutable(), ref inlineContinuation);

                    functionBodies[func] = inlinedBody;
                }
            }

            return new BoundProgram(
                previous,
                diagnostics.ToImmutableArray(),
                globalScope.ScriptFunction,
                functionBodies.ToImmutable(),
                analysisResult,
                functionScopes.ToImmutableDictionary()
            );
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax syntax)
        {
            ImmutableArray<ParameterSymbol>.Builder parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

            HashSet<string> seenParameterNames = new HashSet<string>();

            foreach (ParameterSyntax parameterSyntax in syntax.Parameters)
            {
                Modifiers paramMods = BindModifierClause(parameterSyntax.Modifiers, ModifierTarget.Parameter).Enum;
                TypeSymbol paramType = BindTypeClause(parameterSyntax.TypeClause);
                string paramName = parameterSyntax.Identifier.Text;

                if (!paramMods.MakesTargetReference(out _) && !paramMods.HasFlag(Modifiers.Readonly))
                    paramMods |= Modifiers.Readonly;

                if (!seenParameterNames.Add(paramName))
                    diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Identifier.Location, paramName);
                else
                    parameters.Add(new ParameterSymbol(paramName, paramMods, paramType));
            }

            Modifiers modifiers = BindModifierClause(syntax.Modifiers, ModifierTarget.Function).Enum;

            TypeSymbol type = syntax.TypeClause is null ? TypeSymbol.Void : BindTypeClause(syntax.TypeClause);

            FunctionSymbol function = functionFactory.Create(defaultNamespace, modifiers, type, syntax.Identifier.Text, parameters.ToImmutable(), syntax);
            if (syntax.Identifier.Text is not null &&
                !scope.TryDeclareFunction(function))
                diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, function.Name);
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
                BoundScope scope = parent.AddChild();

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
            BoundScope result = new BoundScope();

            foreach (var group in Constants.Groups)
            {
                foreach (var con in group.Values)
                {
                    VariableSymbol variable = new BasicVariableSymbol(group.Name + "_" + con.Name, Modifiers.Constant | Modifiers.Global, group.Type);
                    variable.Initialize(new BoundConstant(con.Value));
                    if (!result.TryDeclareVariable(variable))
                        diagnostics.ReportFailedToDeclare(TextLocation.None, "Constant", variable.Name);
                }
            }

            foreach (FunctionSymbol f in BuiltinFunctions.GetAll())
                if (!result.TryDeclareFunction(f))
                    diagnostics.ReportFailedToDeclare(TextLocation.None, "Built in function", f.Name);

            return result;
        }

        public DiagnosticBag Diagnostics => diagnostics;

        private BoundStatement BindErrorStatement(SyntaxNode syntax)
            => new BoundExpressionStatement(syntax, new BoundErrorExpression(syntax));

        private BoundStatement BindGlobalStatement(StatementSyntax syntax)
            => BindStatement(syntax, isGlobal: true);

        private BoundStatement BindStatement(StatementSyntax syntax, bool isGlobal = false)
        {
            BoundStatement result = BindStatementInternal(syntax);

            if (result is BoundExpressionStatement es)
            {
                bool isAllowedExpression = es.Expression switch
                {
                    BoundErrorExpression => true,
                    BoundCallExpression when es.Expression.Type == TypeSymbol.Void => true, // methods
                    _ => false,
                };

                if (!isAllowedExpression)
                    diagnostics.ReportInvalidExpressionStatement(syntax.Location);
            }

            return result;
        }

        private BoundStatement BindStatementInternal(StatementSyntax syntax)
        {
            spanProcessed(syntax.Span);

            return syntax switch
            {
                BlockStatementSyntax blockStatement => BindBlockStatement(blockStatement),
                EventStatementSyntax eventStatement => BindEventStatement(eventStatement),
                PostfixStatementSyntax postfixStatement => BindPostfixStatement(postfixStatement),
                PrefixStatementSyntax prefixStatement => BindPrefixStatement(prefixStatement),
                VariableDeclarationStatementSyntax variableDeclarationStatement => BindVariableDeclarationStatement(variableDeclarationStatement),
                AssignmentStatementSyntax assignmentStatement => BindAssignmentStatement(assignmentStatement),
                IfStatementSyntax ifStatement => BindIfStatement(ifStatement),
                WhileStatementSyntax whileStatement => BindWhileStatement(whileStatement),
                DoWhileStatementSyntax doWhileStatement => BindDoWhileStatement(doWhileStatement),
                //ForStatementSyntax forStatement => BindForStatement(forStatement),
                BreakStatementSyntax breakStatement => BindBreakStatement(breakStatement),
                ContinueStatementSyntax continueStatement => BindContinueStatement(continueStatement),
                ReturnStatementSyntax returnStatement => BindReturnStatement(returnStatement),
                BuildCommandStatementSyntax buildCommandStatement => BindBuildCommandStatement(buildCommandStatement),
                CallStatementSyntax callStatement => BindCallStatement(callStatement),
                ExpressionStatementSyntax expressionStatement => BindExpressionStatement(expressionStatement),

                _ => throw new UnexpectedSyntaxException(syntax),
            };
        }

        private BoundStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            ImmutableArray<BoundStatement>.Builder statements = ImmutableArray.CreateBuilder<BoundStatement>();

            enterScope();
            foreach (StatementSyntax statementSyntax in syntax.Statements)
            {
                BoundStatement statement = BindStatement(statementSyntax);
                statements.Add(statement);
            }
            exitScope();

            return new BoundBlockStatement(syntax, statements.ToImmutable());
        }

        private BoundStatement BindEventStatement(EventStatementSyntax syntax)
        {
            if (!Enum.TryParse(syntax.Identifier.Text, out EventType type))
            {
                diagnostics.ReportUnknownEvent(syntax.Identifier.Location, syntax.Identifier.Text);
                return BindErrorStatement(syntax);
            }

            ImmutableArray<ParameterSymbol> parameters = type
                .GetInfo()
                .Parameters
                .ToImmutableArray();

            enterScope();

            BoundArgumentClause? argumentClause = syntax.ArgumentClause is null ? null : BindArgumentClause(syntax.ArgumentClause, parameters, "Event", syntax.Identifier.Text);

            if (argumentClause is null && parameters.Length != 0)
            {
                if (syntax.ArgumentClause is null)
                    diagnostics.ReportSBMustHaveArguments(syntax.Identifier.Location, syntax.Identifier.Text);
                return BindErrorStatement(syntax);
            }

            blockStack.Push(true);
            BoundBlockStatement block = (BoundBlockStatement)BindBlockStatement(syntax.Block);
            _ = blockStack.Pop();

            exitScope();

            return new BoundEventStatement(syntax, type, argumentClause, block);
        }

        private BoundStatement BindPostfixStatement(PostfixStatementSyntax syntax)
        {
            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return BindErrorStatement(syntax);

            if (variable.IsReadOnly)
                diagnostics.ReportCannotAssignReadOnlyVariable(syntax.OperatorToken.Location, variable.Name);

            PostfixKind kind;
            switch (syntax.OperatorToken.Kind)
            {
                case SyntaxKind.PlusPlusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = PostfixKind.Increment;
                    break;
                case SyntaxKind.MinusMinusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = PostfixKind.Decrement;
                    break;
                default:
                    diagnostics.ReportUndefinedPostfixOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, variable.Type);
                    return BindErrorStatement(syntax);
            }

            return new BoundPostfixStatement(syntax, variable, kind);
        }

        private BoundStatement BindPrefixStatement(PrefixStatementSyntax syntax)
        {
            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return BindErrorStatement(syntax);

            if (variable.IsReadOnly)
                diagnostics.ReportCannotAssignReadOnlyVariable(syntax.OperatorToken.Location, variable.Name);

            PrefixKind kind;
            switch (syntax.OperatorToken.Kind)
            {
                case SyntaxKind.PlusPlusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = PrefixKind.Increment;
                    break;
                case SyntaxKind.MinusMinusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = PrefixKind.Decrement;
                    break;
                default:
                    diagnostics.ReportUndefinedPostfixOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, variable.Type);
                    return BindErrorStatement(syntax);
            }

            return new BoundPrefixStatement(syntax, variable, kind);
        }

        private BoundStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
        {
            TypeSymbol? type = BindTypeClause(syntax.TypeClause);

            TypeSymbol? variableType = type;
            VariableSymbol variable = BindVariableDeclaration(syntax.IdentifierToken, syntax.ModifierClause, variableType);

            if (variable.IsReadOnly && syntax.OptionalAssignment is null)
                diagnostics.ReportVariableNotInitialized(syntax.IdentifierToken.Location);

            BoundStatement? optionalAssignment = syntax.OptionalAssignment is null ? null : BindStatement(syntax.OptionalAssignment);

            return new BoundVariableDeclarationStatement(syntax, variable, optionalAssignment);
        }

        private BoundStatement BindAssignmentStatement(AssignmentStatementSyntax syntax)
        {
            var item = BindAssignment(syntax.Destination, syntax.AssignmentToken, syntax.Expression);

            if (item is null)
                return BindErrorStatement(syntax);

            var (variable, op, expression) = item.Value;

            if (op is null)
                return new BoundAssignmentStatement(syntax, variable, expression);
            else
                return new BoundCompoundAssignmentStatement(syntax, variable, op, expression);
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition, TypeSymbol.Bool);

            if (condition.ConstantValue is not null)
            {
                if ((bool)condition.ConstantValue.GetValueOrDefault(TypeSymbol.Bool) == false)
                    diagnostics.ReportUnreachableCode(syntax.ThenStatement);
                else if (syntax.ElseClause is not null)
                    diagnostics.ReportUnreachableCode(syntax.ElseClause.ElseStatement);
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
                    diagnostics.ReportUnreachableCode(syntax.Body);

            BoundStatement body = BindLoopBody(syntax.Body, out BoundLabel breakLabel, out BoundLabel continueLabel);
            return new BoundWhileStatement(syntax, condition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindDoWhileStatement(DoWhileStatementSyntax syntax)
        {
            BoundExpression condition = BindExpression(syntax.Condition, TypeSymbol.Bool);

            BoundStatement body = BindLoopBody(syntax.Body, out BoundLabel breakLabel, out BoundLabel continueLabel);
            return new BoundDoWhileStatement(syntax, body, condition, breakLabel, continueLabel);
        }

        private BoundStatement BindLoopBody(StatementSyntax body, out BoundLabel breakLabel, out BoundLabel continueLabel)
        {
            labelCounter++;
            breakLabel = new BoundLabel($"break{labelCounter}");
            continueLabel = new BoundLabel($"continue{labelCounter}");

            loopStack.Push((breakLabel, continueLabel));
            blockStack.Push(false);
            BoundStatement boundBody = BindStatement(body);
            _ = blockStack.Pop();
            _ = loopStack.Pop();

            return boundBody;
        }

        private BoundStatement BindBreakStatement(BreakStatementSyntax syntax)
        {
            if (loopStack.Count == 0)
            {
                diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }
            else if (blockStack.Peek())
            {
                diagnostics.ReportInvalidKeywordInEvent(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }

            BoundLabel breakLabel = loopStack.Peek().BreakLabel;
            return new BoundGotoStatement(syntax, breakLabel, false);
        }

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (loopStack.Count == 0)
            {
                diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }
            else if (blockStack.Peek())
            {
                diagnostics.ReportInvalidKeywordInEvent(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement(syntax);
            }

            BoundLabel continueLabel = loopStack.Peek().ContinueLabel;
            return new BoundGotoStatement(syntax, continueLabel, false);
        }

        private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
        {
            BoundExpression? expression = syntax.Expression is null ? null : BindExpression(syntax.Expression);

            if (blockStack.Contains(true)) // contains event
            {
                diagnostics.ReportInvalidKeywordInEvent(syntax.ReturnKeyword.Location, syntax.ReturnKeyword.Text);
                return BindErrorStatement(syntax);
            }

            if (function is null)
            {
                if (expression is not null)
                {
                    // Main does not support return values.
                    diagnostics.ReportInvalidReturnWithValueInGlobalStatements(syntax.Expression!.Location);
                }
            }
            else
            {
                if (function.Type == TypeSymbol.Void)
                {
                    if (expression is not null)
                        diagnostics.ReportInvalidReturnExpression(syntax.Expression!.Location, function.Name);
                }
                else
                {
                    if (expression is null)
                        diagnostics.ReportMissingReturnExpression(syntax.ReturnKeyword.Location, function.Type);
                    else
                        expression = BindConversion(syntax.Expression!.Location, expression, function.Type);
                }
            }

            return new BoundReturnStatement(syntax, expression);
        }

        private BoundStatement BindBuildCommandStatement(BuildCommandStatementSyntax syntax)
        {
            BuildCommand? command = BuildCommandE.Parse(syntax.Identifier.Text);

            if (command is null)
            {
                diagnostics.ReportUnknownBuildCommand(syntax.Identifier.Location, syntax.Identifier.Text);
                return BindErrorStatement(syntax);
            }

            BoundEmitterHintStatement.HintKind kind;

            switch (command)
            {
                case BuildCommand.Highlight:
                    kind = BoundEmitterHintStatement.HintKind.HighlightStart;
                    break;
                case BuildCommand.EndHighlight:
                    kind = BoundEmitterHintStatement.HintKind.HighlightEnd;
                    break;
                default:
                    throw new UnknownEnumValueException<BuildCommand>(command);
            }

            return new BoundEmitterHintStatement(syntax, kind);
        }

        private BoundStatement BindCallStatement(CallStatementSyntax syntax)
        {
            var arguments = syntax.Arguments;

            ImmutableArray<BoundExpression>.Builder boundArguments = ImmutableArray.CreateBuilder<BoundExpression>(arguments.Count);

            foreach (ModifiersWExpressionSyntax argument in arguments)
                boundArguments.Add(BindExpression(argument.Expression, context: new Context()
                {
                    OutArgument = argument.ModifierClause.Modifiers.Any(token => token.Kind == SyntaxKind.OutModifier)
                }));

            FunctionSymbol? function = scope.TryLookupFunction(syntax.Identifier.Text, boundArguments.Select(arg => arg.Type!).ToList(), false);

            if (function is null)
            {
                diagnostics.ReportUndefinedFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return BindErrorStatement(syntax);
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

                            TypeSymbol? paramGenericType = null;
                            if (param.Type == TypeSymbol.Generic)
                                paramGenericType = arg.Type;
                            else if (param.Type!.IsGenericDefinition && arg.Type!.IsGenericInstance)
                                paramGenericType = arg.Type.InnerType;

                            if (paramGenericType is not null && paramGenericType != TypeSymbol.Null)
                            {
                                if (genericType is null)
                                    genericType = paramGenericType;
                                else if (!genericType.GenericEquals(paramGenericType))
                                {
                                    genericType = null;
                                    break;
                                }
                            }
                        }
                    }

                    if (genericType is null)
                    {
                        diagnostics.ReportCannotInferGenericType(syntax.Location);
                        return BindErrorStatement(syntax);
                    }
                    else if (genericType.IsGenericInstance)
                    {
                        diagnostics.ReportGenericTypeRecursion(syntax.HasGenericParameter ? syntax.GenericTypeClause.Location : syntax.Location);
                        return BindErrorStatement(syntax);
                    }
                    else if (!function.AllowedGenericTypes.Value.Contains(genericType))
                    {
                        diagnostics.ReportSpecificGenericTypeNotAllowed(syntax.HasGenericParameter ? syntax.GenericTypeClause.Location : syntax.Location, genericType, function.AllowedGenericTypes.Value);
                        return BindErrorStatement(syntax);
                    }
                }
                else if (syntax.HasGenericParameter)
                {
                    diagnostics.ReportNonGenericMethodTypeArguments(new TextLocation(syntax.SyntaxTree.Text, TextSpan.FromBounds(syntax.LessThanToken.Span.Start, syntax.GreaterThanToken.Span.End)));
                    return BindErrorStatement(syntax);
                }
            }

            BoundArgumentClause? argumentClause = BindArgumentClause(new ArgumentClauseSyntax(syntax.ArgumentClause.SyntaxTree, syntax.ArgumentClause.OpenParenthesisToken, arguments, syntax.ArgumentClause.CloseParenthesisToken),
                function.Parameters
                    .Select(param => new ParameterSymbol(param.Name, param.Modifiers, fixType(param.Type)))
                    .ToImmutableArray(),
                "Function", function.Name, boundArguments);

            if (argumentClause is null)
                return BindErrorStatement(syntax);

            return new BoundCallStatement(syntax, function, argumentClause, fixType(function.Type)!, genericType, null);

            TypeSymbol fixType(TypeSymbol type)
            {
                if (type == TypeSymbol.Generic) return genericType ?? TypeSymbol.Error;
                else if (type.IsGenericDefinition) return TypeSymbol.CreateGenericInstance(type, genericType ?? TypeSymbol.Error);
                else return type;
            }
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
                diagnostics.ReportExpressionMustHaveValue(syntax.Location);
                return new BoundErrorExpression(syntax);
            }

            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax syntax, Context? context = null)
        {
            spanProcessed(syntax.Span);

            return syntax switch
            {
                ParenthesizedExpressionSyntax parenthesizedExpression => BindParenthesizedExpression(parenthesizedExpression),
                LiteralExpressionSyntax literalExpression => BindLiteralExpression(literalExpression),
                NameExpressionSyntax nameExpression => BindNameExpression(nameExpression, context),
                VariableDeclarationExpressionSyntax variableDeclarationExpression => BindVariableDeclarationExpression(variableDeclarationExpression),
                UnaryExpressionSyntax unaryExpression => BindUnaryExpression(unaryExpression),
                BinaryExpressionSyntax binaryExpression => BindBinaryExpression(binaryExpression),
                CallExpressionSyntax callExpression => BindCallExpression(callExpression, context),
                ConstructorExpressionSyntax constructorExpression => BindConstructorExpression(constructorExpression),
                PostfixExpressionSyntax postfixExpression => BindPostfixExpression(postfixExpression),
                PrefixExpressionSyntax prefixExpression => BindPrefixExpression(prefixExpression),
                PropertyExpressionSyntax propertyExpression => BindPropertyExpression(propertyExpression),
                ArraySegmentExpressionSyntax arraySegmentExpression => BindArraySegmentExpression(arraySegmentExpression),
                AssignmentExpressionSyntax assignmentExpression => BindAssignmentExpression(assignmentExpression),

                _ => throw new UnexpectedSyntaxException(syntax),
            };
        }

        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
            => BindExpression(syntax.Expression);

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

            return new BoundVariableExpression(syntax, variable);
        }

        private BoundExpression BindVariableDeclarationExpression(VariableDeclarationExpressionSyntax syntax)
        {
            TypeSymbol? type = BindTypeClause(syntax.TypeClause);

            TypeSymbol? variableType = type;
            VariableSymbol variable = BindVariableDeclaration(syntax.IdentifierToken, syntax.ModifierClause, variableType);

            if (variable.Modifiers.HasFlag(Modifiers.Constant))
                diagnostics.ReportVariableNotInitialized(syntax.IdentifierToken.Location);

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
                diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundOperand.Type);
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
                diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression(syntax);
            }

            return new BoundBinaryExpression(syntax, boundLeft, boundOperator, boundRight);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax, Context? context = null)
        {
            context ??= Context.Default;

            var arguments = syntax.Arguments;
            if (context.MethodObject is not null)
            {
                arguments = new SeparatedSyntaxList<ModifiersWExpressionSyntax>(
                    new SyntaxNode[] {
                        new ModifiersWExpressionSyntax(context.MethodObject.SyntaxTree, new ModifierClauseSyntax(syntax.SyntaxTree, []), context.MethodObject),
                        new SyntaxToken(context.MethodObject.SyntaxTree, SyntaxKind.CommaToken, context.MethodObject.Span.Start, null, null, [], []),
                    }.Concat(arguments.GetWithSeparators())
                    .ToImmutableArray()
                );
            }

            ImmutableArray<BoundExpression>.Builder boundArguments = ImmutableArray.CreateBuilder<BoundExpression>(arguments.Count);

            foreach (ModifiersWExpressionSyntax argument in arguments)
                boundArguments.Add(BindExpression(argument.Expression, context: new Context()
                {
                    OutArgument = argument.ModifierClause.Modifiers.Any(token => token.Kind == SyntaxKind.OutModifier)
                }));

            FunctionSymbol? function = scope.TryLookupFunction(syntax.Identifier.Text, boundArguments.Select(arg => arg.Type!).ToList(), context.MethodObject is not null);

            if (function is null)
            {
                diagnostics.ReportUndefinedFunction(syntax.Identifier.Location, syntax.Identifier.Text);
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

                            TypeSymbol? paramGenericType = null;
                            if (param.Type == TypeSymbol.Generic)
                                paramGenericType = arg.Type;
                            else if (param.Type!.IsGenericDefinition && arg.Type!.IsGenericInstance)
                                paramGenericType = arg.Type.InnerType;

                            if (paramGenericType is not null && paramGenericType != TypeSymbol.Null)
                            {
                                if (genericType is null)
                                    genericType = paramGenericType;
                                else if (!genericType.GenericEquals(paramGenericType))
                                {
                                    genericType = null;
                                    break;
                                }
                            }
                        }
                    }

                    if (genericType is null)
                    {
                        diagnostics.ReportCannotInferGenericType(syntax.Location);
                        return new BoundErrorExpression(syntax);
                    }
                    else if (genericType.IsGenericInstance)
                    {
                        diagnostics.ReportGenericTypeRecursion(syntax.HasGenericParameter ? syntax.GenericTypeClause.Location : syntax.Location);
                        return new BoundErrorExpression(syntax);
                    }
                    else if (!function.AllowedGenericTypes.Value.Contains(genericType))
                    {
                        diagnostics.ReportSpecificGenericTypeNotAllowed(syntax.HasGenericParameter ? syntax.GenericTypeClause.Location : syntax.Location, genericType, function.AllowedGenericTypes.Value);
                        return new BoundErrorExpression(syntax);
                    }
                }
                else if (syntax.HasGenericParameter)
                {
                    diagnostics.ReportNonGenericMethodTypeArguments(new TextLocation(syntax.SyntaxTree.Text, TextSpan.FromBounds(syntax.LessThanToken.Span.Start, syntax.GreaterThanToken.Span.End)));
                    return new BoundErrorExpression(syntax);
                }
            }

            BoundArgumentClause? argumentClause = BindArgumentClause(new ArgumentClauseSyntax(syntax.ArgumentClause.SyntaxTree, syntax.ArgumentClause.OpenParenthesisToken, arguments, syntax.ArgumentClause.CloseParenthesisToken),
                function.Parameters
                    .Select(param => new ParameterSymbol(param.Name, param.Modifiers, fixType(param.Type)))
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

            TypeSymbol type = TypeSymbol.GetType(syntax.KeywordToken.Text);

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

        private BoundExpression BindPostfixExpression(PostfixExpressionSyntax syntax)
        {
            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return new BoundErrorExpression(syntax);

            if (variable.IsReadOnly)
                diagnostics.ReportCannotAssignReadOnlyVariable(syntax.OperatorToken.Location, variable.Name);

            PostfixKind kind;
            switch (syntax.OperatorToken.Kind)
            {
                case SyntaxKind.PlusPlusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = PostfixKind.Increment;
                    break;
                case SyntaxKind.MinusMinusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = PostfixKind.Decrement;
                    break;
                default:
                    diagnostics.ReportUndefinedPostfixOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, variable.Type);
                    return new BoundErrorExpression(syntax);
            }

            return new BoundPostfixExpression(syntax, variable, kind);
        }

        private BoundExpression BindPrefixExpression(PrefixExpressionSyntax syntax)
        {
            VariableSymbol? variable = BindVariableReference(syntax.IdentifierToken);
            if (variable is null)
                return new BoundErrorExpression(syntax);

            if (variable.IsReadOnly)
                diagnostics.ReportCannotAssignReadOnlyVariable(syntax.OperatorToken.Location, variable.Name);

            PrefixKind kind;
            switch (syntax.OperatorToken.Kind)
            {
                case SyntaxKind.PlusPlusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = PrefixKind.Increment;
                    break;
                case SyntaxKind.MinusMinusToken when variable.Type.Equals(TypeSymbol.Float):
                    kind = PrefixKind.Decrement;
                    break;
                default:
                    diagnostics.ReportUndefinedPrefixOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text, variable.Type);
                    return new BoundErrorExpression(syntax);
            }

            return new BoundPrefixExpression(syntax, variable, kind);
        }

        private BoundExpression BindPropertyExpression(PropertyExpressionSyntax syntax)
        {
            switch (syntax.Expression)
            {
                case NameExpressionSyntax:
                    {
                        PropertySymbol? prop = BindProperty(syntax);
                        if (prop is null)
                            return new BoundErrorExpression(syntax);
                        else
                            return new BoundVariableExpression(syntax, prop);
                    }
                case CallExpressionSyntax callExpression:
                    {
                        BoundExpression baseEx = BindExpression(syntax.BaseExpression);
                        return BindCallExpression(callExpression, new Context()
                        {
                            MethodObject = syntax.BaseExpression,
                        });
                    }
                default:
                    throw new UnexpectedSyntaxException(syntax.Expression);
            }
        }

        private BoundExpression BindArraySegmentExpression(ArraySegmentExpressionSyntax syntax)
        {
            if (syntax.Elements.Count == 0)
            {
                diagnostics.ReportEmptyArrayInitializer(new TextLocation(syntax.SyntaxTree.Text, TextSpan.FromBounds(syntax.OpenSquareToken.Span.Start, syntax.CloseSquareToken.Span.End)));
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

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var item = BindAssignment(syntax.Destination, syntax.AssignmentToken, syntax.Expression);

            if (item is null)
                return new BoundErrorExpression(syntax);

            var (variable, op, expression) = item.Value;

            if (op is null)
                return new BoundAssignmentExpression(syntax, variable, expression);
            else
                return new BoundCompoundAssignmentExpression(syntax, variable, op, expression);
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
                    diagnostics.ReportCannotConvert(diagnosticLocation, expression.Type, type);

                return new BoundErrorExpression(expression.Syntax);
            }

            if (!allowExplicit && conversion.Type == Conversion.TypeEnum.Explicit)
                diagnostics.ReportCannotConvertImplicitly(diagnosticLocation, expression.Type, type);

            if (conversion.IsIdentity || conversion.Type == Conversion.TypeEnum.Direct)
                return expression;

            return new BoundConversionExpression(expression.Syntax, type, expression);
        }

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, ModifierClauseSyntax modifierClause, TypeSymbol type)
        {
            string name = identifier.Text ?? "?";
            bool declare = !identifier.IsMissing;

            if (name == "_")
                diagnostics.ReportInvalidName(identifier.Location, name);

            Modifiers validModifiers = ModifiersE.GetValidModifiersFor(ModifierTarget.Variable, type);

            BoundModifierClause modifiers = BindModifierClause(modifierClause, ModifierTarget.Variable, item =>
            {
                var (token, modifier) = item;

                bool valid = validModifiers.HasFlag(modifier);
                if (!valid)
                    diagnostics.ReportInvalidModifierOnType(token.Location, modifier, type);

                return valid;
            });

            VariableSymbol variable = new BasicVariableSymbol(name, modifiers.Enum, type);

            if (declare && !scope.TryDeclareVariable(variable))
                diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);
            else if (name.Length > FancadeConstants.MaxVariableNameLength && !modifiers.Enum.HasFlag(Modifiers.Constant))
                diagnostics.ReportVariableNameTooLong(identifier.Location, name);

            return variable;
        }

        private VariableSymbol? BindVariableReference(SyntaxToken identifierToken, Context? context = null)
        {
            context ??= Context.Default;

            string name = identifierToken.Text;

            if (context.OutArgument && name == "_")
                return new NullVariableSymbol();

            switch (scope.TryLookupVariable(name))
            {
                case VariableSymbol variable:
                    return variable;

                default:
                    diagnostics.ReportUndefinedVariable(identifierToken.Location, name);
                    return null;
            }
        }

        private TypeSymbol BindTypeClause(TypeClauseSyntax? syntax)
        {
            if (syntax is null)
                return TypeSymbol.Error;

            TypeSymbol? type = LookupType(syntax.TypeToken.Text);
            if (type is null)
            {
                diagnostics.ReportUndefinedType(syntax.Location, syntax.TypeToken.Text);
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
                    diagnostics.ReportNotAGenericType(new TextLocation(syntax.SyntaxTree.Text, TextSpan.FromBounds(syntax.LessToken.Span.Start, syntax.GreaterToken.Span.End)));
                    return TypeSymbol.Error;
                }
            }
            else if (type.IsGenericDefinition)
            {
                diagnostics.ReportTypeMustHaveGenericParameter(syntax.Location);
                return TypeSymbol.Error;
            }

            return type;
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

                foreach (ModifiersWExpressionSyntax argument in syntax.Arguments)
                    boundArguments.Add(BindExpression(argument.Expression, context: new Context()
                    {
                        OutArgument = argument.ModifierClause.Modifiers.Any(token => token.Kind == SyntaxKind.OutModifier)
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
                diagnostics.ReportWrongArgumentCount(location, type, name, parameters.Length, syntax.Arguments.Count);
                return null;
            }

            var argModifiersBuilder = ImmutableArray.CreateBuilder<Modifiers>();

            for (int i = 0; i < syntax.Arguments.Count; i++)
            {
                TextLocation argumentLocation = syntax.Arguments[i].Expression.Location;
                BoundExpression argument = boundArguments[i];
                ParameterSymbol parameter = parameters[i];

                BoundModifierClause argMods = BindModifierClause(syntax.Arguments[i].ModifierClause, ModifierTarget.Argument, item =>
                {
                    var (token, modifier) = item;

                    bool valid = modifier == Modifiers.Ref ? parameter.Modifiers.HasFlag(Modifiers.Ref) : true &&
                        modifier == Modifiers.Out ? parameter.Modifiers.HasFlag(Modifiers.Out) : true;

                    if (!valid)
                        diagnostics.ReportArgumentCannotHaveModifier(token.Location, parameter.Name, modifier);

                    return valid;
                });

                argModifiersBuilder.Add(argMods.Enum);

                if (parameter.Modifiers.HasFlag(Modifiers.Ref) && !argMods.Enum.HasFlag(Modifiers.Ref))
                    diagnostics.ReportArgumentMustHaveModifier(argumentLocation, parameter.Name, Modifiers.Ref);
                else if (parameter.Modifiers.HasFlag(Modifiers.Out) && !argMods.Enum.HasFlag(Modifiers.Out))
                    diagnostics.ReportArgumentMustHaveModifier(argumentLocation, parameter.Name, Modifiers.Out);
                if (argMods.Enum.MakesTargetReference(out Modifiers? makesRefMod) && (argument is not BoundVariableExpression variable ||
                    (variable.Variable.IsReadOnly && argument.Syntax.Kind != SyntaxKind.VariableDeclarationExpression && variable.Variable is not NullVariableSymbol)))
                    diagnostics.ReportByRefArgMustBeVariable(argumentLocation, makesRefMod.Value);
                if (parameter.Modifiers.HasFlag(Modifiers.Constant) && argument.ConstantValue is null)
                    diagnostics.ReportValueMustBeConstant(argument.Syntax.Location);

                boundArguments[i] = BindConversion(argumentLocation, argument, parameter.Type);
            }

            return new BoundArgumentClause(syntax, argModifiersBuilder.ToImmutable(), boundArguments.ToImmutable());
        }

        private BoundModifierClause BindModifierClause(ModifierClauseSyntax modifierClause, ModifierTarget target, Func<(SyntaxToken, Modifiers), bool>? checkModifier = null)
        {
            if (!modifierClause.Modifiers.Any())
                return new BoundModifierClause(modifierClause, ImmutableArray<(SyntaxToken, Modifiers)>.Empty);

            checkModifier ??= item => true;

            List<(SyntaxToken Token, Modifiers Modifier)> modifiersAndTokens = modifierClause.Modifiers
                .Where(token =>
                {
                    // remove tokens that aren't modifiers
                    bool isModifier = token.Kind.IsModifier();

                    if (!isModifier)
                        diagnostics.ReportNotAModifier(token.Location, token.Text);

                    return isModifier;
                })
                .Select(token => (token, ModifiersE.FromKind(token.Kind)))
                .Where(item =>
                {
                    // remove modifiers not valid for this target
                    var (token, modifier) = item;
                    bool valid = modifier.GetTargets().Contains(target);

                    if (!valid)
                        diagnostics.ReportInvalidModifier(token.Location, modifier, target, modifier.GetTargets());

                    return valid;
                })
                .Where(checkModifier)
                .ToList();

            IEnumerable<Modifiers> boundMods = modifiersAndTokens.Select(item => item.Modifier);

            // remove conflicting modifiers
            for (int i = 0; i < modifiersAndTokens.Count; i++)
            {
                Modifiers? conflict = null;
                foreach (var possibleConflict in modifiersAndTokens[i].Modifier.GetConflictingModifiers())
                    if (boundMods.Contains(possibleConflict))
                    {
                        conflict = possibleConflict;
                        break;
                    }

                if (conflict is not null)
                {
                    diagnostics.ReportConflictingModifiers(modifiersAndTokens[i].Token.Location, conflict.Value, modifiersAndTokens[i].Modifier);

                    modifiersAndTokens.RemoveAt(i--);
                }
            }

            // find missing required
            foreach (var (token, mod) in modifiersAndTokens)
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
                    diagnostics.ReportMissignRequiredModifiers(token.Location, mod, required);
            }

            Modifiers modifiers = 0;

            for (int i = 0; i < modifiersAndTokens.Count; i++)
            {
                var (token, modifier) = modifiersAndTokens[i];

                if (modifiers.HasFlag(modifier))
                {
                    diagnostics.ReportDuplicateModifier(token.Location, modifier);
                    modifiersAndTokens.RemoveAt(i--);
                }
                else
                    modifiers |= modifier;
            }

            return new BoundModifierClause(modifierClause, modifiersAndTokens.ToImmutableArray());
        }

        private PropertySymbol? BindProperty(PropertyExpressionSyntax syntax)
        {
            if (syntax.Expression is not NameExpressionSyntax nameEx)
                return null;

            BoundExpression baseEx = BindExpression(syntax.BaseExpression);

            PropertyDefinitionSymbol? property = baseEx.Type.GetProperty(nameEx.IdentifierToken.Text);
            if (property is null)
            {
                diagnostics.ReportUndefinedProperty(nameEx.IdentifierToken.Location, baseEx.Type, nameEx.IdentifierToken.Text);
                return null;
            }

            return new PropertySymbol(property, baseEx);
        }

        private (VariableSymbol, BoundBinaryOperator?, BoundExpression)? BindAssignment(AssignableExpressionSyntax destination, SyntaxToken assignmentToken, ExpressionSyntax expression)
        {
            BoundExpression boundExpression = BindExpression(expression);

            VariableSymbol? variable;
            switch (destination)
            {
                case NameExpressionSyntax name:
                    {
                        variable = BindVariableReference(name.IdentifierToken);
                        if (variable is null)
                            return null;

                        if (variable.IsReadOnly && variable.Initialized)
                            diagnostics.ReportCannotAssignReadOnlyVariable(assignmentToken.Location, variable.Name);
                        else if (variable.Modifiers.HasFlag(Modifiers.Constant) && boundExpression.ConstantValue is null)
                            diagnostics.ReportValueMustBeConstant(expression.Location);

                        variable.Initialize(boundExpression.ConstantValue);
                    }
                    break;
                case PropertyExpressionSyntax prop:
                    {
                        var property = BindProperty(prop);

                        if (property is null)
                            return null;

                        if (property.IsReadOnly)
                            diagnostics.ReportCannotAssignReadOnlyProperty(assignmentToken.Location, property.Name);

                        variable = property;
                    }
                    break;
                default:
                    throw new UnexpectedSyntaxException(destination);
            }

            if (assignmentToken.Kind != SyntaxKind.EqualsToken)
            {
                SyntaxKind equivalentOperatorTokenKind = SyntaxFacts.GetBinaryOperatorOfAssignmentOperator(assignmentToken.Kind);
                BoundBinaryOperator? boundOperator = BoundBinaryOperator.Bind(equivalentOperatorTokenKind, variable.Type, boundExpression.Type);

                if (boundOperator is null)
                {
                    diagnostics.ReportUndefinedBinaryOperator(assignmentToken.Location, assignmentToken.Text, variable.Type, boundExpression.Type);
                    return null;
                }

                BoundExpression convertedExpression = bindConversion(boundOperator.RightType);

                return (variable, boundOperator, convertedExpression);
            }
            else
            {
                BoundExpression convertedExpression = bindConversion(variable.Type);
                return (variable, null, convertedExpression);
            }

            BoundExpression bindConversion(TypeSymbol targetType)
            {
                if (boundExpression.Type.NonGenericEquals(TypeSymbol.ArraySegment) && targetType.NonGenericEquals(TypeSymbol.Array) && boundExpression.Type.InnerType == targetType.InnerType)
                    return boundExpression;
                else
                    return BindConversion(expression.Location, boundExpression, targetType);
            }
        }
        #endregion

        private void spanProcessed(TextSpan span)
        {
            scope.AddSpan(span);
        }

        private void enterScope()
        {
            scope = scope.AddChild();
        }
        private void exitScope()
        {
            scope = scope.Parent!;
        }

        private class Context
        {
            public static readonly Context Default = new Context();

            public bool OutArgument { get; init; }
            public ExpressionSyntax? MethodObject { get; init; }
        }
    }
}
