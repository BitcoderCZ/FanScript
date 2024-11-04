using System.Collections.Immutable;
using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Lexing;
using FanScript.Compiler.Symbols;
using FanScript.Compiler.Text;

namespace FanScript.Compiler.Syntax
{
    internal sealed class Parser
    {
        private static readonly HashSet<string> AllowedTypes = TypeSymbol.BuiltInTypes
                .Select(type => type.Name)
                .ToHashSet();

        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private int _position;

        public Parser(SyntaxTree syntaxTree)
        {
            List<SyntaxToken> tokens = [];
            List<SyntaxToken> badTokens = [];

            Lexer lexer = new Lexer(syntaxTree);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind == SyntaxKind.BadToken)
                {
                    badTokens.Add(token);
                }
                else
                {
                    if (badTokens.Count > 0)
                    {
                        ImmutableArray<SyntaxTrivia>.Builder leadingTrivia = token.LeadingTrivia.ToBuilder();
                        int index = 0;

                        foreach (SyntaxToken badToken in badTokens)
                        {
                            foreach (SyntaxTrivia lt in badToken.LeadingTrivia)
                            {
                                leadingTrivia.Insert(index++, lt);
                            }

                            SyntaxTrivia trivia = new SyntaxTrivia(syntaxTree, SyntaxKind.SkippedTextTrivia, badToken.Position, badToken.Text);
                            leadingTrivia.Insert(index++, trivia);

                            foreach (SyntaxTrivia tt in badToken.TrailingTrivia)
                            {
                                leadingTrivia.Insert(index++, tt);
                            }
                        }

                        badTokens.Clear();
                        token = new SyntaxToken(token.SyntaxTree, token.Kind, token.Position, token.Text, token.Value, leadingTrivia.ToImmutable(), token.TrailingTrivia);
                    }

                    tokens.Add(token);
                }
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            _syntaxTree = syntaxTree;
            _text = syntaxTree.Text;
            _tokens = [.. tokens];
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private SyntaxToken Current => Peek(0);

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            ImmutableArray<MemberSyntax> members = ParseMembers();
            SyntaxToken endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(_syntaxTree, members, endOfFileToken);
        }

        private SyntaxToken Peek(int offset)
        {
            int index = _position + offset;
            return index >= _tokens.Length ? _tokens[^1] : _tokens[index];
        }

        private SyntaxToken NextToken()
        {
            SyntaxToken current = Current;
            _position++;
            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
            {
                return NextToken();
            }

            _diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kind);
            return new SyntaxToken(_syntaxTree, kind, Current.Position, null, null, [], []);
        }

        private SyntaxToken MatchToken(params SyntaxKind[] kinds)
        {
            foreach (SyntaxKind kind in kinds)
            {
                if (Current.Kind == kind)
                {
                    return NextToken();
                }
            }

            _diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kinds);
            return new SyntaxToken(_syntaxTree, kinds[0], Current.Position, null, null, [], []);
        }

        private SyntaxToken MatchIdentifier(string text)
        {
            if (Current.Kind == SyntaxKind.IdentifierToken && Current.Text == text)
            {
                return NextToken();
            }

            if (Current.Kind != SyntaxKind.IdentifierToken)
            {
                _diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, SyntaxKind.IdentifierToken);
            }
            else
            {
                _diagnostics.ReportUnexpectedIdentifier(Current.Location, Current.Text, text);
            }

            return new SyntaxToken(_syntaxTree, SyntaxKind.IdentifierToken, Current.Position, null, null, [], []);
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
                    {
                        return NextToken();
                    }
                }
            }

            if (Current.Kind != SyntaxKind.IdentifierToken)
            {
                _diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, SyntaxKind.IdentifierToken);
            }
            else
            {
                _diagnostics.ReportUnexpectedIdentifier(Current.Location, Current.Text, texts);
            }

            return new SyntaxToken(_syntaxTree, SyntaxKind.IdentifierToken, Current.Position, null, null, [], []);
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
                {
                    NextToken();
                }
            }

            return members.ToImmutable();
        }

        private MemberSyntax ParseMember()
            => Current.Kind == SyntaxKind.KeywordFunction ||
                (AreModifiersNow(out int nextToken) && Peek(nextToken).Kind == SyntaxKind.KeywordFunction)
                ? ParseFunctionDeclaration()
                : ParseGlobalStatement();

        private FunctionDeclarationSyntax ParseFunctionDeclaration()
        {
            ModifierClauseSyntax modifiers = ParseModifiers();
            SyntaxToken funcKeyword = MatchToken(SyntaxKind.KeywordFunction);
            TypeClauseSyntax? funcType = null;
            if (IsTypeClauseNow(out _))
            {
                funcType = ParseTypeClause(false);
            }

            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            ImmutableArray<SyntaxNode> parameters = ParseSeparatedList(SyntaxKind.CloseParenthesisToken, parameters: true);
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            BlockStatementSyntax body = ParseBlockStatement();
            return new FunctionDeclarationSyntax(_syntaxTree, modifiers, funcKeyword, funcType, identifier, openParenthesisToken, new SeparatedSyntaxList<ParameterSyntax>(parameters), closeParenthesisToken, body);
        }

        private ParameterSyntax ParseParameter()
        {
            ModifierClauseSyntax modifiers = ParseModifiers();
            TypeClauseSyntax typeClause = ParseTypeClause(false);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            return new ParameterSyntax(_syntaxTree, modifiers, typeClause, identifier);
        }

        private GlobalStatementSyntax ParseGlobalStatement()
            => new GlobalStatementSyntax(_syntaxTree, ParseStatement());

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
                        return IsTypeClauseNow(out _)
                            ? ParseVariableDeclarationStatement()
                            : Current.Kind.IsModifier()
                            ? ParseVariableDeclarationStatementWithModifiers()
                            : ParseExpressionStatement();
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
                {
                    NextToken();
                }
            }

            SyntaxToken closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);

            return new BlockStatementSyntax(_syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private EventStatementSyntax ParseEventStatement()
        {
            SyntaxToken onKeyword = MatchToken(SyntaxKind.KeywordOn);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            ArgumentClauseSyntax? argumentClause = null;
            if (Current.Kind == SyntaxKind.OpenParenthesisToken)
            {
                argumentClause = ParseArgumentClause();
            }

            BlockStatementSyntax block = ParseBlockStatement();

            return new EventStatementSyntax(_syntaxTree, onKeyword, identifier, argumentClause, block);
        }

        private PostfixStatementSyntax ParsePostfixStatement()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken operatorToken = MatchToken(SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken);

            return new PostfixStatementSyntax(_syntaxTree, identifierToken, operatorToken);
        }

        private PrefixStatementSyntax ParsePrefixStatement()
        {
            SyntaxToken operatorToken = MatchToken(SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken);
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);

            return new PrefixStatementSyntax(_syntaxTree, operatorToken, identifierToken);
        }

        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement(ModifierClauseSyntax? modifiers = null)
        {
            TypeClauseSyntax typeClause = ParseTypeClause(true);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);

            StatementSyntax? assignment = null;
            if (Current.Kind == SyntaxKind.EqualsToken)
            {
                SyntaxToken equals = MatchToken(SyntaxKind.EqualsToken);
                ExpressionSyntax initializer = ParseExpression();

                assignment = new AssignmentStatementSyntax(_syntaxTree, new NameExpressionSyntax(_syntaxTree, identifier), equals, initializer);
            }

            return new VariableDeclarationStatementSyntax(_syntaxTree, modifiers ?? new ModifierClauseSyntax(_syntaxTree, []), typeClause, identifier, assignment);
        }

        private IfStatementSyntax ParseIfStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordIf);
            ExpressionSyntax condition = ParseExpression();
            StatementSyntax statement = ParseStatement();
            ElseClauseSyntax? elseClause = ParseOptionalElseClause();
            return new IfStatementSyntax(_syntaxTree, keyword, condition, statement, elseClause);
        }

        private ElseClauseSyntax? ParseOptionalElseClause()
        {
            if (Current.Kind != SyntaxKind.KeywordElse)
            {
                return null;
            }

            SyntaxToken keyword = NextToken();
            StatementSyntax statement = ParseStatement();
            return new ElseClauseSyntax(_syntaxTree, keyword, statement);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordWhile);
            ExpressionSyntax condition = ParseExpression();
            StatementSyntax body = ParseStatement();
            return new WhileStatementSyntax(_syntaxTree, keyword, condition, body);
        }

        private DoWhileStatementSyntax ParseDoWhileStatement()
        {
            SyntaxToken doKeyword = MatchToken(SyntaxKind.KeywordDo);
            StatementSyntax body = ParseStatement();
            SyntaxToken whileKeyword = MatchToken(SyntaxKind.KeywordWhile);
            ExpressionSyntax condition = ParseExpression();
            return new DoWhileStatementSyntax(_syntaxTree, doKeyword, body, whileKeyword, condition);
        }

        /*private StatementSyntax ParseForStatement()
        {
            var keyword = MatchToken(SyntaxKind.ForKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var equalsToken = MatchToken(SyntaxKind.EqualsToken);
            var lowerBound = ParseExpression();
            var toKeyword = MatchToken(SyntaxKind.ToKeyword);
            var upperBound = ParseExpression();
            var body = ParseStatement();
            return new ForStatementSyntax(syntaxTree, keyword, identifier, equalsToken, lowerBound, toKeyword, upperBound, body);
        }*/

        private BreakStatementSyntax ParseBreakStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordBreak);
            return new BreakStatementSyntax(_syntaxTree, keyword);
        }

        private ContinueStatementSyntax ParseContinueStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordContinue);
            return new ContinueStatementSyntax(_syntaxTree, keyword);
        }

        private ReturnStatementSyntax ParseReturnStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordReturn);
            int keywordLine = _text.GetLineIndex(keyword.Span.Start);
            int currentLine = _text.GetLineIndex(Current.Span.Start);
            bool isEof = Current.Kind == SyntaxKind.EndOfFileToken;
            bool sameLine = !isEof && keywordLine == currentLine;
            ExpressionSyntax? expression = sameLine ? ParseExpression() : null;
            return new ReturnStatementSyntax(_syntaxTree, keyword, expression);
        }

        private BuildCommandStatementSyntax ParseBuildCommandStatement()
        {
            SyntaxToken hashtagToken = MatchToken(SyntaxKind.HashtagToken);
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new BuildCommandStatementSyntax(_syntaxTree, hashtagToken, identifierToken);
        }

        private CallStatementSyntax ParseCallStatement()
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

            return hasGegenericParam
                ? new CallStatementSyntax(_syntaxTree, identifier, lessThanToken, genericType, greaterThanToken, argumentClause)
                : new CallStatementSyntax(_syntaxTree, identifier, argumentClause);
        }

        private VariableDeclarationStatementSyntax ParseVariableDeclarationStatementWithModifiers()
            => ParseVariableDeclarationStatement(ParseModifiers());

        private StatementSyntax ParseExpressionStatement()
        {
            ExpressionSyntax expression = ParseExpression();

            return expression is AssignmentExpressionSyntax assignment
                ? new AssignmentStatementSyntax(_syntaxTree, assignment.Destination, assignment.AssignmentToken, assignment.Expression)
                : (StatementSyntax)new ExpressionStatementSyntax(_syntaxTree, expression);
        }

        private ExpressionSyntax ParseExpression()
            => ParseBinaryExpression();

        private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            int unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                SyntaxToken operatorToken = NextToken();
                ExpressionSyntax operand = ParseBinaryExpression(unaryOperatorPrecedence);
                left = new UnaryExpressionSyntax(_syntaxTree, operatorToken, operand);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                int precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                {
                    break;
                }

                SyntaxToken operatorToken = NextToken();
                ExpressionSyntax right = ParseBinaryExpression(precedence);
                left = new BinaryExpressionSyntax(_syntaxTree, left, operatorToken, right);
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
                expression = new PropertyExpressionSyntax(_syntaxTree, expression, dotToken, nameOrCall);
            }

            // handle assignment
            // a = b = c = 5
            // a = (b = (c = 5))
            if (expression is not AssignableExpressionSyntax assignableExpression)
            {
                return expression;
            }

            Stack<(AssignableExpressionSyntax, SyntaxToken)> assignmentStack = new();

#pragma warning disable SA1513 // Closing brace should be followed by blank line
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
                {
                    break;
                }

                assignableExpression = (AssignableExpressionSyntax)expression;
            }
#pragma warning restore SA1513 // Closing brace should be followed by blank line

            while (assignmentStack.Count > 0)
            {
                var (destination, assignmentToken) = assignmentStack.Pop();

                expression = new AssignmentExpressionSyntax(_syntaxTree, destination, assignmentToken, expression);
            }

            return expression;
        }

        private ExpressionSyntax ParsePrimaryExpressionInternal()
#pragma warning disable SA1513 // Closing brace should be followed by blank line
            => Current.Kind switch
            {
                SyntaxKind.OpenParenthesisToken => ParseParenthesizedExpression(),
                SyntaxKind.KeywordNull => ParseNullLiteral(),
                SyntaxKind.KeywordFalse or SyntaxKind.KeywordTrue => ParseBooleanLiteral(),
                SyntaxKind.FloatToken => ParseNumberLiteral(),
                SyntaxKind.StringToken => ParseStringLiteral(),
                SyntaxKind.IdentifierToken when Current.Text == TypeSymbol.Vector3.Name || Current.Text == TypeSymbol.Rotation.Name => ParseVectorConstructorExpresion(),
                SyntaxKind.IdentifierToken when Peek(1).Kind switch
                {
                    SyntaxKind.PlusPlusToken => true,
                    SyntaxKind.MinusMinusToken => true,
                    _ => false,
                } => ParsePostfixExpression(),
                SyntaxKind.PlusPlusToken or SyntaxKind.MinusMinusToken => ParsePrefixExpression(),
                SyntaxKind.OpenSquareToken => ParseArraySegmentExpression(),
                _ => ParseNameOrCallExpressions(),
            };
#pragma warning restore SA1513 // Closing brace should be followed by blank line

        private ParenthesizedExpressionSyntax ParseParenthesizedExpression()
        {
            SyntaxToken left = MatchToken(SyntaxKind.OpenParenthesisToken);
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken right = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ParenthesizedExpressionSyntax(_syntaxTree, left, expression, right);
        }

        private LiteralExpressionSyntax ParseNullLiteral()
            => new LiteralExpressionSyntax(_syntaxTree, MatchToken(SyntaxKind.KeywordNull));

        private LiteralExpressionSyntax ParseBooleanLiteral()
        {
            SyntaxToken keywordToken = MatchToken(SyntaxKind.KeywordTrue, SyntaxKind.KeywordFalse);
            bool isTrue = keywordToken.Kind == SyntaxKind.KeywordTrue;
            return new LiteralExpressionSyntax(_syntaxTree, keywordToken, isTrue);
        }

        private LiteralExpressionSyntax ParseNumberLiteral()
            => new LiteralExpressionSyntax(_syntaxTree, MatchToken(SyntaxKind.FloatToken));

        private ConstructorExpressionSyntax ParseVectorConstructorExpresion()
        {
            SyntaxToken keywordToken = MatchType([TypeSymbol.Vector3, TypeSymbol.Rotation]);

            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            ExpressionSyntax expressionX = ParseExpression();
            SyntaxToken comma0Token = MatchToken(SyntaxKind.CommaToken);
            ExpressionSyntax expressionY = ParseExpression();
            SyntaxToken comma1Token = MatchToken(SyntaxKind.CommaToken);
            ExpressionSyntax expressionZ = ParseExpression();
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new ConstructorExpressionSyntax(_syntaxTree, keywordToken, openParenthesisToken, expressionX, comma0Token, expressionY, comma1Token, expressionZ, closeParenthesisToken);
        }

        private PostfixExpressionSyntax ParsePostfixExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken operatorToken = MatchToken(SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken);

            return new PostfixExpressionSyntax(_syntaxTree, identifierToken, operatorToken);
        }

        private PrefixExpressionSyntax ParsePrefixExpression()
        {
            SyntaxToken operatorToken = MatchToken(SyntaxKind.PlusPlusToken, SyntaxKind.MinusMinusToken);
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);

            return new PrefixExpressionSyntax(_syntaxTree, operatorToken, identifierToken);
        }

        private ArraySegmentExpressionSyntax ParseArraySegmentExpression()
        {
            SyntaxToken openSquareToken = MatchToken(SyntaxKind.OpenSquareToken);
            var elements = ParseSeparatedList(SyntaxKind.CloseSquareToken);
            SyntaxToken closeSquareToken = MatchToken(SyntaxKind.CloseSquareToken);

            return new ArraySegmentExpressionSyntax(
                _syntaxTree,
                openSquareToken,
                new SeparatedSyntaxList<ExpressionSyntax>(
                    elements
                    .Select(node => node is ModifiersWExpressionSyntax modifierClause ? modifierClause.Expression : node)
                    .ToImmutableArray()),
                closeSquareToken);
        }

        private LiteralExpressionSyntax ParseStringLiteral()
            => new LiteralExpressionSyntax(_syntaxTree, MatchToken(SyntaxKind.StringToken));

        private ExpressionSyntax ParseNameOrCallExpressions()
            => IsCallNow() ? ParseCallExpression() : ParseNameExpression();

        private bool IsCallNow()
        {
            int offset = 0;
            return Peek(offset++).Kind == SyntaxKind.IdentifierToken &&
                (Peek(offset).Kind == SyntaxKind.OpenParenthesisToken ||
                (Peek(offset++).Kind == SyntaxKind.LessToken && IsTypeClauseNext(ref offset) && Peek(offset++).Kind == SyntaxKind.GreaterToken && Peek(offset).Kind == SyntaxKind.OpenParenthesisToken)); // not sure of a better way to do this, neccesary because of less than operator
        }

        private CallExpressionSyntax ParseCallExpression()
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

            return hasGegenericParam
                ? new CallExpressionSyntax(_syntaxTree, identifier, lessThanToken, genericType, greaterThanToken, argumentClause)
                : new CallExpressionSyntax(_syntaxTree, identifier, argumentClause);
        }

        private NameExpressionSyntax ParseNameExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new NameExpressionSyntax(_syntaxTree, identifierToken);
        }

        private ArgumentClauseSyntax ParseArgumentClause()
        {
            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var arguments = ParseSeparatedList(SyntaxKind.CloseParenthesisToken, true);
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ArgumentClauseSyntax(_syntaxTree, openParenthesisToken, new SeparatedSyntaxList<ModifiersWExpressionSyntax>(arguments), closeParenthesisToken);
        }

        private ImmutableArray<SyntaxNode> ParseSeparatedList(SyntaxKind listEnd, bool allowModifiers = false, bool parameters = false)
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            if (Current.Kind == listEnd)
            {
                goto skip;
            }

            while (//Current.Kind != listEnd &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                if (parameters)
                {
                    nodesAndSeparators.Add(ParseParameter());
                }
                else
                {
                    ModifierClauseSyntax modifiers = allowModifiers ? ParseModifiers() : new ModifierClauseSyntax(_syntaxTree, []);

                    if (IsTypeClauseNow(out _) && modifiers.Modifiers.Any(token => token.Kind == SyntaxKind.OutModifier))
                    {
                        TypeClauseSyntax typeClause = ParseTypeClause(true);
                        SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);

                        nodesAndSeparators.Add(new ModifiersWExpressionSyntax(
                            _syntaxTree,
                            new ModifierClauseSyntax(
                                _syntaxTree,
                                modifiers.Modifiers
                                    .Where(token => !ModifiersE.FromKind(token.Kind).GetTargets().Contains(ModifierTarget.Variable))
                                    .ToImmutableArray()),
                            new VariableDeclarationExpressionSyntax(
                                _syntaxTree,
                                new ModifierClauseSyntax(
                                    _syntaxTree,
                                    modifiers.Modifiers
                                        .Where(token => ModifiersE.FromKind(token.Kind).GetTargets().Contains(ModifierTarget.Variable))
                                        .ToImmutableArray()),
                                typeClause,
                                identifierToken)));
                    }
                    else
                    {
                        nodesAndSeparators.Add(new ModifiersWExpressionSyntax(_syntaxTree, modifiers, ParseExpression()));
                    }
                }

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    SyntaxToken comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                {
                    break;
                }
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
            if (Peek(nextTokenIndex).Kind == SyntaxKind.IdentifierToken && AllowedTypes.Contains(Peek(nextTokenIndex++).Text))
            {
                int temp = nextTokenIndex;

                if (Peek(nextTokenIndex++).Kind == SyntaxKind.LessToken &&
                    IsTypeClauseNext(ref nextTokenIndex) &&
                    Peek(nextTokenIndex++).Kind == SyntaxKind.GreaterToken)
                {
                    return true;
                }
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
                {
                    _diagnostics.ReportGenericTypeNotAllowed(typeToken.Location);
                }
                else if (gettingGenericParam)
                {
                    _diagnostics.ReportGenericTypeRecursion(new TextLocation(_syntaxTree.Text, TextSpan.FromBounds(lessToken.Span.Start, greaterToken.Span.End)));
                }

                return new TypeClauseSyntax(_syntaxTree, typeToken, lessToken, innerType, greaterToken);
            }
            else
            {
                return new TypeClauseSyntax(_syntaxTree, typeToken);
            }
        }

        private bool AreModifiersNow(out int nextTokenIndex)
        {
            nextTokenIndex = -1;

            if (!Current.Kind.IsModifier())
            {
                return false;
            }

            nextTokenIndex = 1;

            while (Peek(nextTokenIndex).Kind.IsModifier())
            {
                nextTokenIndex++;
            }

            return true;
        }

        private ModifierClauseSyntax ParseModifiers()
        {
            ImmutableArray<SyntaxToken>.Builder builder = ImmutableArray.CreateBuilder<SyntaxToken>();

            while (Current.Kind.IsModifier())
            {
                builder.Add(NextToken());
            }

            return new ModifierClauseSyntax(_syntaxTree, builder.ToImmutable());
        }
    }
}
