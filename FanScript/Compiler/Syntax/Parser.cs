using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Lexing;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Text;
using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    internal sealed class Parser
    {
        private static readonly HashSet<string> allowedTypes = TypeSymbol.BuiltInTypes
                .Select(type => type.Name)
                .ToHashSet();

        private readonly DiagnosticBag diagnostics = new DiagnosticBag();
        private readonly SyntaxTree syntaxTree;
        private readonly SourceText text;
        private readonly ImmutableArray<SyntaxToken> tokens;
        private int position;

        public Parser(SyntaxTree syntaxTree)
        {
            List<SyntaxToken> tokens = new();
            List<SyntaxToken> badTokens = new();

            Lexer lexer = new Lexer(syntaxTree);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind == SyntaxKind.BadToken)
                    badTokens.Add(token);
                else
                {
                    if (badTokens.Count > 0)
                    {
                        ImmutableArray<SyntaxTrivia>.Builder leadingTrivia = token.LeadingTrivia.ToBuilder();
                        int index = 0;

                        foreach (SyntaxToken badToken in badTokens)
                        {
                            foreach (SyntaxTrivia lt in badToken.LeadingTrivia)
                                leadingTrivia.Insert(index++, lt);

                            SyntaxTrivia trivia = new SyntaxTrivia(syntaxTree, SyntaxKind.SkippedTextTrivia, badToken.Position, badToken.Text);
                            leadingTrivia.Insert(index++, trivia);

                            foreach (SyntaxTrivia tt in badToken.TrailingTrivia)
                                leadingTrivia.Insert(index++, tt);
                        }

                        badTokens.Clear();
                        token = new SyntaxToken(token.SyntaxTree, token.Kind, token.Position, token.Text, token.Value, leadingTrivia.ToImmutable(), token.TrailingTrivia);
                    }

                    tokens.Add(token);
                }
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            this.syntaxTree = syntaxTree;
            text = syntaxTree.Text;
            this.tokens = tokens.ToImmutableArray();
            diagnostics.AddRange(lexer.Diagnostics);
        }

        public DiagnosticBag Diagnostics => diagnostics;

        private SyntaxToken Peek(int offset)
        {
            int index = position + offset;
            if (index >= tokens.Length)
                return tokens[tokens.Length - 1];

            return tokens[index];
        }

        private SyntaxToken Current => Peek(0);

        private SyntaxToken NextToken()
        {
            SyntaxToken current = Current;
            position++;
            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kind);
            return new SyntaxToken(syntaxTree, kind, Current.Position, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
        }
        private SyntaxToken MatchToken(params SyntaxKind[] kinds)
        {
            foreach (SyntaxKind kind in kinds)
                if (Current.Kind == kind)
                    return NextToken();

            diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kinds);
            return new SyntaxToken(syntaxTree, kinds[0], Current.Position, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
        }
        private SyntaxToken MatchIdentifier(string text)
        {
            if (Current.Kind == SyntaxKind.IdentifierToken && Current.Text == text)
                return NextToken();

            if (Current.Kind != SyntaxKind.IdentifierToken)
                diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, SyntaxKind.IdentifierToken);
            else
                diagnostics.ReportUnexpectedIdentifier(Current.Location, Current.Text, text);
            return new SyntaxToken(syntaxTree, SyntaxKind.IdentifierToken, Current.Position, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
        }
        private SyntaxToken MatchType(IEnumerable<TypeSymbol> types)
            => MatchIdentifier(types.Select(type => type.Name));
        private SyntaxToken MatchIdentifier(IEnumerable<string> texts)
        {
            if (Current.Kind == SyntaxKind.IdentifierToken)
            {
                foreach (string text in texts)
                {
                    if (Current.Text == text)
                        return NextToken();
                }
            }

            if (Current.Kind != SyntaxKind.IdentifierToken)
                diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, SyntaxKind.IdentifierToken);
            else
                diagnostics.ReportUnexpectedIdentifier(Current.Location, Current.Text, texts);
            return new SyntaxToken(syntaxTree, SyntaxKind.IdentifierToken, Current.Position, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            ImmutableArray<MemberSyntax> members = ParseMembers();
            SyntaxToken endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(syntaxTree, members, endOfFileToken);
        }

        private ImmutableArray<MemberSyntax> ParseMembers()
        {
            ImmutableArray<MemberSyntax>.Builder members = ImmutableArray.CreateBuilder<MemberSyntax>();

            while (Current.Kind != SyntaxKind.EndOfFileToken)
            {
                SyntaxToken startToken = Current;

                MemberSyntax member = ParseMember();
                members.Add(member);

                // If ParseMember() did not consume any tokens,
                // we need to skip the current token and continue
                // in order to avoid an infinite loop.
                //
                // We don't need to report an error, because we'll
                // already tried to parse an expression statement
                // and reported one.
                if (Current == startToken)
                    NextToken();
            }

            return members.ToImmutable();
        }

        private MemberSyntax ParseMember()
        {
            if (Current.Kind == SyntaxKind.KeywordFunction ||
                (AreModifiersNow(out int nextToken) && Peek(nextToken).Kind == SyntaxKind.KeywordFunction))
                return ParseFunctionDeclaration();

            return ParseGlobalStatement();
        }

        private MemberSyntax ParseFunctionDeclaration()
        {
            ImmutableArray<SyntaxToken> modifiers = ParseModifiers();
            SyntaxToken funcKeyword = MatchToken(SyntaxKind.KeywordFunction);
            TypeClauseSyntax? funcType = null;
            if (IsTypeClauseNow(out _))
                funcType = ParseTypeClause(false);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            ImmutableArray<SyntaxNode> parameters = ParseSeparatedList(SyntaxKind.CloseParenthesisToken, parameters: true);
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            BlockStatementSyntax body = ParseBlockStatement();
            return new FunctionDeclarationSyntax(syntaxTree, modifiers, funcKeyword, funcType, identifier, openParenthesisToken, new SeparatedSyntaxList<ParameterSyntax>(parameters), closeParenthesisToken, body);
        }

        private ParameterSyntax ParseParameter()
        {
            ImmutableArray<SyntaxToken> modifiers = ParseModifiers();
            TypeClauseSyntax typeClause = ParseTypeClause(false);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            return new ParameterSyntax(syntaxTree, modifiers, typeClause, identifier);
        }

        private MemberSyntax ParseGlobalStatement()
        {
            return new GlobalStatementSyntax(syntaxTree, ParseStatement());
        }

        private StatementSyntax ParseStatement()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                    return ParseBlockStatement();
                case SyntaxKind.KeywordOn:
                    return ParseEventStatement();
                case SyntaxKind.IdentifierToken when Peek(1).Kind switch
                {
                    SyntaxKind.PlusPlusToken => true,
                    SyntaxKind.MinusMinusToken => true,
                    _ => false,
                }:
                    return ParsePostfixStatement();
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusMinusToken:
                    return ParsePrefixStatement();
                case SyntaxKind.KeywordIf:
                    return ParseIfStatement();
                case SyntaxKind.KeywordWhile:
                    return ParseWhileStatement();
                case SyntaxKind.KeywordDo:
                    return ParseDoWhileStatement();
                //case SyntaxKind.ForKeyword:
                //    return ParseForStatement();
                case SyntaxKind.KeywordBreak:
                    return ParseBreakStatement();
                case SyntaxKind.KeywordContinue:
                    return ParseContinueStatement();
                case SyntaxKind.KeywordReturn:
                    return ParseReturnStatement();
                case SyntaxKind.HashtagToken:
                    return ParseBuildCommandStatement();
                case SyntaxKind.IdentifierToken when IsCallNow():
                    return ParseCallStatement();
                default:
                    {
                        if (IsTypeClauseNow(out _))
                            return ParseVariableDeclarationStatement();
                        else if (Current.Kind.IsModifier())
                            return ParseVariableDeclarationStatementWithModifiers();

                        return ParseExpressionStatement();
                    }
            }
        }

        private BlockStatementSyntax ParseBlockStatement()
        {
            ImmutableArray<StatementSyntax>.Builder statements = ImmutableArray.CreateBuilder<StatementSyntax>();

            SyntaxToken openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

            while (Current.Kind != SyntaxKind.EndOfFileToken &&
                   Current.Kind != SyntaxKind.CloseBraceToken)
            {
                SyntaxToken startToken = Current;

                StatementSyntax statement = ParseStatement();
                statements.Add(statement);

                // If ParseStatement() did not consume any tokens,
                // we need to skip the current token and continue
                // in order to avoid an infinite loop.
                //
                // We don't need to report an error, because we'll
                // already tried to parse an expression statement
                // and reported one.
                if (Current == startToken)
                    NextToken();
            }

            SyntaxToken closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

            return new BlockStatementSyntax(syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private StatementSyntax ParseEventStatement()
        {
            SyntaxToken onKeyword = MatchToken(SyntaxKind.KeywordOn);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            ArgumentClauseSyntax? argumentClause = null;
            if (Current.Kind == SyntaxKind.OpenParenthesisToken)
                argumentClause = ParseArgumentClause();
            BlockStatementSyntax block = ParseBlockStatement();

            return new EventStatementSyntax(syntaxTree, onKeyword, identifier, argumentClause, block);
        }

        private StatementSyntax ParsePostfixStatement()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken operatorToken = MatchToken(SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken);

            return new PostfixStatementSyntax(syntaxTree, identifierToken, operatorToken);
        }

        private StatementSyntax ParsePrefixStatement()
        {
            SyntaxToken operatorToken = MatchToken(SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken);
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);

            return new PrefixStatementSyntax(syntaxTree, operatorToken, identifierToken);
        }

        private StatementSyntax ParseVariableDeclarationStatement(ImmutableArray<SyntaxToken>? modifiers = null)
        {
            TypeClauseSyntax typeClause = ParseTypeClause(true);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);

            StatementSyntax? assignment = null;
            if (Current.Kind == SyntaxKind.EqualsToken)
            {
                SyntaxToken equals = MatchToken(SyntaxKind.EqualsToken);
                ExpressionSyntax initializer = ParseExpression();

                assignment = new AssignmentStatementSyntax(syntaxTree, new NameExpressionSyntax(syntaxTree, identifier), equals, initializer);
            }

            return new VariableDeclarationStatementSyntax(syntaxTree, modifiers ?? ImmutableArray<SyntaxToken>.Empty, typeClause, identifier, assignment);
        }

        private StatementSyntax ParseIfStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordIf);
            ExpressionSyntax condition = ParseExpression();
            StatementSyntax statement = ParseStatement();
            ElseClauseSyntax? elseClause = ParseOptionalElseClause();
            return new IfStatementSyntax(syntaxTree, keyword, condition, statement, elseClause);
        }

        private ElseClauseSyntax? ParseOptionalElseClause()
        {
            if (Current.Kind != SyntaxKind.KeywordElse)
                return null;

            SyntaxToken keyword = NextToken();
            StatementSyntax statement = ParseStatement();
            return new ElseClauseSyntax(syntaxTree, keyword, statement);
        }

        private StatementSyntax ParseWhileStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordWhile);
            ExpressionSyntax condition = ParseExpression();
            StatementSyntax body = ParseStatement();
            return new WhileStatementSyntax(syntaxTree, keyword, condition, body);
        }

        private StatementSyntax ParseDoWhileStatement()
        {
            SyntaxToken doKeyword = MatchToken(SyntaxKind.KeywordDo);
            StatementSyntax body = ParseStatement();
            SyntaxToken whileKeyword = MatchToken(SyntaxKind.KeywordWhile);
            ExpressionSyntax condition = ParseExpression();
            return new DoWhileStatementSyntax(syntaxTree, doKeyword, body, whileKeyword, condition);
        }

        //private StatementSyntax ParseForStatement()
        //{
        //    var keyword = MatchToken(SyntaxKind.ForKeyword);
        //    var identifier = MatchToken(SyntaxKind.IdentifierToken);
        //    var equalsToken = MatchToken(SyntaxKind.EqualsToken);
        //    var lowerBound = ParseExpression();
        //    var toKeyword = MatchToken(SyntaxKind.ToKeyword);
        //    var upperBound = ParseExpression();
        //    var body = ParseStatement();
        //    return new ForStatementSyntax(syntaxTree, keyword, identifier, equalsToken, lowerBound, toKeyword, upperBound, body);
        //}

        private StatementSyntax ParseBreakStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordBreak);
            return new BreakStatementSyntax(syntaxTree, keyword);
        }

        private StatementSyntax ParseContinueStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordContinue);
            return new ContinueStatementSyntax(syntaxTree, keyword);
        }

        private StatementSyntax ParseReturnStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordReturn);
            int keywordLine = text.GetLineIndex(keyword.Span.Start);
            int currentLine = text.GetLineIndex(Current.Span.Start);
            bool isEof = Current.Kind == SyntaxKind.EndOfFileToken;
            bool sameLine = !isEof && keywordLine == currentLine;
            ExpressionSyntax? expression = sameLine ? ParseExpression() : null;
            return new ReturnStatementSyntax(syntaxTree, keyword, expression);
        }

        private StatementSyntax ParseBuildCommandStatement()
        {
            SyntaxToken hashtagToken = MatchToken(SyntaxKind.HashtagToken);
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new BuildCommandStatementSyntax(syntaxTree, hashtagToken, identifierToken);
        }

        private StatementSyntax ParseCallStatement()
        {
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);

            bool hasGegenericParam = Current.Kind == SyntaxKind.LessToken;
            SyntaxToken lessThanToken = null!;
            TypeClauseSyntax genericType = null!;
            SyntaxToken greaterThanToken = null!;
            if (hasGegenericParam)
            {
                lessThanToken = MatchToken(SyntaxKind.LessToken);
                genericType = ParseTypeClause(false);
                greaterThanToken = MatchToken(SyntaxKind.GreaterToken);
            }

            ArgumentClauseSyntax argumentClause = ParseArgumentClause();

            if (hasGegenericParam)
                return new CallStatementSyntax(syntaxTree, identifier, lessThanToken, genericType, greaterThanToken, argumentClause);
            else
                return new CallStatementSyntax(syntaxTree, identifier, argumentClause);
        }

        private StatementSyntax ParseVariableDeclarationStatementWithModifiers()
        {
            return ParseVariableDeclarationStatement(ParseModifiers());
        }

        private StatementSyntax ParseExpressionStatement()
        {
            ExpressionSyntax expression = ParseExpression();

            if (expression is AssignmentExpressionSyntax assignment)
                return new AssignmentStatementSyntax(syntaxTree, assignment.Destination, assignment.AssignmentToken, assignment.Expression);

            return new ExpressionStatementSyntax(syntaxTree, expression);
        }

        private ExpressionSyntax ParseExpression()
        {
            return ParseBinaryExpression();
        }

        private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            int unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                SyntaxToken operatorToken = NextToken();
                ExpressionSyntax operand = ParseBinaryExpression(unaryOperatorPrecedence);
                left = new UnaryExpressionSyntax(syntaxTree, operatorToken, operand);
            }
            else
                left = ParsePrimaryExpression();

            while (true)
            {
                int precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                SyntaxToken operatorToken = NextToken();
                ExpressionSyntax right = ParseBinaryExpression(precedence);
                left = new BinaryExpressionSyntax(syntaxTree, left, operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            ExpressionSyntax expression = ParsePrimaryExpressionInternal();

            // handle properties
            while (Current.Kind == SyntaxKind.DotToken)
            {
                SyntaxToken dotToken = MatchToken(SyntaxKind.DotToken);
                ExpressionSyntax nameOrCall = ParseNameOrCallExpressions();
                expression = new PropertyExpressionSyntax(syntaxTree, expression, dotToken, nameOrCall);
            }

            // handle assignment
            // a = b = c = 5
            // a = (b = (c = 5))

            if (expression is not AssignableExpressionSyntax assignableExpression)
                return expression;

            Stack<(AssignableExpressionSyntax, SyntaxToken)> assignmentStack = new();

            while (Current.Kind switch
            {
                SyntaxKind.EqualsToken => true,
                SyntaxKind.PlusEqualsToken => true,
                SyntaxKind.MinusEqualsToken => true,
                SyntaxKind.StarEqualsToken => true,
                SyntaxKind.SlashEqualsToken => true,
                SyntaxKind.PercentEqualsToken => true,
                _ => false,
            } && (assignableExpression is not PropertyExpressionSyntax prop || prop.Expression.Kind == SyntaxKind.NameExpression))
            {
                assignmentStack.Push((assignableExpression, MatchToken(SyntaxKind.EqualsToken, SyntaxKind.PlusEqualsToken, SyntaxKind.MinusEqualsToken, SyntaxKind.StarEqualsToken, SyntaxKind.SlashEqualsToken, SyntaxKind.PercentEqualsToken)));

                expression = ParseExpression();

                if (expression is not AssignableExpressionSyntax)
                    break;

                assignableExpression = (AssignableExpressionSyntax)expression;
            }

            while (assignmentStack.Count > 0)
            {
                var (destination, assignmentToken) = assignmentStack.Pop();

                expression = new AssignmentExpressionSyntax(syntaxTree, destination, assignmentToken, expression);
            }

            return expression;
        }

        private ExpressionSyntax ParsePrimaryExpressionInternal()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenthesisToken:
                    return ParseParenthesizedExpression();

                case SyntaxKind.KeywordNull:
                    return ParseNullLiteral();

                case SyntaxKind.KeywordFalse:
                case SyntaxKind.KeywordTrue:
                    return ParseBooleanLiteral();

                case SyntaxKind.FloatToken:
                    return ParseNumberLiteral();

                case SyntaxKind.StringToken:
                    return ParseStringLiteral();

                case SyntaxKind.IdentifierToken when (Current.Text == TypeSymbol.Vector3.Name || Current.Text == TypeSymbol.Rotation.Name):
                    return ParseVectorConstructorExpresion();

                case SyntaxKind.IdentifierToken when Peek(1).Kind switch
                {
                    SyntaxKind.PlusPlusToken => true,
                    SyntaxKind.MinusMinusToken => true,
                    _ => false,
                }:
                    return ParsePostfixExpression();

                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusMinusToken:
                    return ParsePrefixExpression();

                case SyntaxKind.OpenSquareToken:
                    return ParseArraySegmentExpression();

                case SyntaxKind.IdentifierToken:
                default:
                    return ParseNameOrCallExpressions();
            }
        }

        private ExpressionSyntax ParseParenthesizedExpression()
        {
            SyntaxToken left = MatchToken(SyntaxKind.OpenParenthesisToken);
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken right = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ParenthesizedExpressionSyntax(syntaxTree, left, expression, right);
        }

        private ExpressionSyntax ParseNullLiteral()
            => new LiteralExpressionSyntax(syntaxTree, MatchToken(SyntaxKind.KeywordNull));

        private ExpressionSyntax ParseBooleanLiteral()
        {
            SyntaxToken keywordToken = MatchToken(SyntaxKind.KeywordTrue, SyntaxKind.KeywordFalse);
            bool isTrue = keywordToken.Kind == SyntaxKind.KeywordTrue;
            return new LiteralExpressionSyntax(syntaxTree, keywordToken, isTrue);
        }

        private ExpressionSyntax ParseNumberLiteral()
            => new LiteralExpressionSyntax(syntaxTree, MatchToken(SyntaxKind.FloatToken));

        private ExpressionSyntax ParseVectorConstructorExpresion()
        {
            SyntaxToken keywordToken = MatchType([TypeSymbol.Vector3, TypeSymbol.Rotation]);

            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            ExpressionSyntax expressionX = ParseExpression();
            SyntaxToken comma0Token = MatchToken(SyntaxKind.CommaToken);
            ExpressionSyntax expressionY = ParseExpression();
            SyntaxToken comma1Token = MatchToken(SyntaxKind.CommaToken);
            ExpressionSyntax expressionZ = ParseExpression();
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new ConstructorExpressionSyntax(syntaxTree, keywordToken, openParenthesisToken, expressionX, comma0Token, expressionY, comma1Token, expressionZ, closeParenthesisToken);
        }

        private ExpressionSyntax ParsePostfixExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken operatorToken = MatchToken(SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken);

            return new PostfixExpressionSyntax(syntaxTree, identifierToken, operatorToken);
        }
        private ExpressionSyntax ParsePrefixExpression()
        {
            SyntaxToken operatorToken = MatchToken(SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken);
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);

            return new PrefixExpressionSyntax(syntaxTree, operatorToken, identifierToken);
        }

        private ExpressionSyntax ParseArraySegmentExpression()
        {
            SyntaxToken openSquareToken = MatchToken(SyntaxKind.OpenSquareToken);
            var elements = ParseSeparatedList(SyntaxKind.CloseSquareToken);
            SyntaxToken closeSquareToken = MatchToken(SyntaxKind.CloseSquareToken);

            return new ArraySegmentExpressionSyntax(syntaxTree, openSquareToken, new SeparatedSyntaxList<ExpressionSyntax>(elements
                .Select(node =>
                {
                    if (node is ModifierClauseSyntax modifierClause)
                        return modifierClause.Expression;

                    return node;
                })
                .ToImmutableArray()), closeSquareToken);
        }

        private ExpressionSyntax ParseStringLiteral()
            => new LiteralExpressionSyntax(syntaxTree, MatchToken(SyntaxKind.StringToken));

        private ExpressionSyntax ParseNameOrCallExpressions()
        {
            if (IsCallNow())
                return ParseCallExpression();

            return ParseNameExpression();
        }

        private bool IsCallNow()
        {
            int offset = 0;
            return Peek(offset++).Kind == SyntaxKind.IdentifierToken &&
                (Peek(offset).Kind == SyntaxKind.OpenParenthesisToken ||
                (Peek(offset++).Kind == SyntaxKind.LessToken && IsTypeClauseNext(ref offset) && Peek(offset++).Kind == SyntaxKind.GreaterToken && Peek(offset).Kind == SyntaxKind.OpenParenthesisToken)); // not sure of a better way to do this, neccesary because of less than operator
        }
        private ExpressionSyntax ParseCallExpression()
        {
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);

            bool hasGegenericParam = Current.Kind == SyntaxKind.LessToken;
            SyntaxToken lessThanToken = null!;
            TypeClauseSyntax genericType = null!;
            SyntaxToken greaterThanToken = null!;
            if (hasGegenericParam)
            {
                lessThanToken = MatchToken(SyntaxKind.LessToken);
                genericType = ParseTypeClause(false);
                greaterThanToken = MatchToken(SyntaxKind.GreaterToken);
            }

            ArgumentClauseSyntax argumentClause = ParseArgumentClause();

            if (hasGegenericParam)
                return new CallExpressionSyntax(syntaxTree, identifier, lessThanToken, genericType, greaterThanToken, argumentClause);
            else
                return new CallExpressionSyntax(syntaxTree, identifier, argumentClause);
        }

        private ExpressionSyntax ParseNameExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new NameExpressionSyntax(syntaxTree, identifierToken);
        }

        private ArgumentClauseSyntax ParseArgumentClause()
        {
            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var arguments = ParseSeparatedList(SyntaxKind.CloseParenthesisToken, true);
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ArgumentClauseSyntax(syntaxTree, openParenthesisToken, new SeparatedSyntaxList<ModifierClauseSyntax>(arguments), closeParenthesisToken);
        }
        private ImmutableArray<SyntaxNode> ParseSeparatedList(SyntaxKind listEnd, bool allowModifiers = false, bool parameters = false)
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            if (Current.Kind == listEnd)
                goto skip;

            while (//Current.Kind != listEnd &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (parameters)
                    nodesAndSeparators.Add(ParseParameter());
                else
                {
                    ImmutableArray<SyntaxToken> modifiers;
                    if (allowModifiers)
                        modifiers = ParseModifiers();
                    else
                        modifiers = ImmutableArray<SyntaxToken>.Empty;

                    if (IsTypeClauseNow(out _) && modifiers.Any(token => token.Kind == SyntaxKind.OutModifier))
                    {
                        TypeClauseSyntax typeClause = ParseTypeClause(true);
                        SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);

                        nodesAndSeparators.Add(new ModifierClauseSyntax(
                            syntaxTree,
                            modifiers
                                .Where(token => !ModifiersE.FromKind(token.Kind).GetTargets().Contains(ModifierTarget.Variable))
                                .ToImmutableArray(),
                            new VariableDeclarationExpressionSyntax(
                                syntaxTree,
                                modifiers
                                    .Where(token => ModifiersE.FromKind(token.Kind).GetTargets().Contains(ModifierTarget.Variable))
                                    .ToImmutableArray(),
                                typeClause,
                                identifierToken
                            )
                        ));
                    }
                    else
                        nodesAndSeparators.Add(new ModifierClauseSyntax(syntaxTree, modifiers, ParseExpression()));
                }

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    SyntaxToken comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                    break;
            }

        skip:
            return nodesAndSeparators.ToImmutable();
        }

        private bool IsTypeClauseNow(out int nextTokenIndex)
        {
            nextTokenIndex = 0;
            return IsTypeClauseNext(ref nextTokenIndex);
        }
        private bool IsTypeClauseNext(ref int nextTokenIndex)
        {
            if (Peek(nextTokenIndex).Kind == SyntaxKind.IdentifierToken && allowedTypes.Contains(Peek(nextTokenIndex++).Text))
            {
                int temp = nextTokenIndex;

                if (Peek(nextTokenIndex++).Kind == SyntaxKind.LessToken &&
                    IsTypeClauseNext(ref nextTokenIndex) &&
                    Peek(nextTokenIndex++).Kind == SyntaxKind.GreaterToken)
                    return true;
                else
                {
                    nextTokenIndex = temp;
                    return true;
                }
            }

            return false;
        }
        private TypeClauseSyntax ParseTypeClause(bool allowGeneric, bool gettingGenericParam = false)
        {
            SyntaxToken typeToken = MatchType(TypeSymbol.BuiltInTypes);

            if (Current.Kind == SyntaxKind.LessToken)
            {
                SyntaxToken lessToken = MatchToken(SyntaxKind.LessToken);
                TypeClauseSyntax innerType = ParseTypeClause(allowGeneric, true);
                SyntaxToken greaterToken = MatchToken(SyntaxKind.GreaterToken);

                if (!allowGeneric)
                    diagnostics.ReportGenericTypeNotAllowed(typeToken.Location);
                else if (gettingGenericParam)
                    diagnostics.ReportGenericTypeRecursion(new TextLocation(syntaxTree.Text, TextSpan.FromBounds(lessToken.Span.Start, greaterToken.Span.End)));

                return new TypeClauseSyntax(syntaxTree, typeToken, lessToken, innerType, greaterToken);
            }
            else
                return new TypeClauseSyntax(syntaxTree, typeToken);
        }

        private bool AreModifiersNow(out int nextTokenIndex)
        {
            nextTokenIndex = -1;

            if (!Current.Kind.IsModifier())
                return false;

            nextTokenIndex = 1;

            while (Peek(nextTokenIndex).Kind.IsModifier())
                nextTokenIndex++;

            return true;
        }
        private ImmutableArray<SyntaxToken> ParseModifiers()
        {
            ImmutableArray<SyntaxToken>.Builder builder = ImmutableArray.CreateBuilder<SyntaxToken>();

            while (Current.Kind.IsModifier())
                builder.Add(NextToken());

            return builder.ToImmutable();
        }
    }
}
