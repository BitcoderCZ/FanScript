﻿using FanScript.Compiler.Diagnostics;
using FanScript.Compiler.Lexing;
using FanScript.Compiler.Text;
using System.Collections.Immutable;

namespace FanScript.Compiler.Syntax
{
    internal sealed class Parser
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly ImmutableArray<SyntaxToken> _tokens;
        private int _position;

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

            _syntaxTree = syntaxTree;
            _text = syntaxTree.Text;
            _tokens = tokens.ToImmutableArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        public DiagnosticBag Diagnostics => _diagnostics;

        private SyntaxToken Peek(int offset)
        {
            int index = _position + offset;
            if (index >= _tokens.Length)
                return _tokens[_tokens.Length - 1];

            return _tokens[index];
        }

        private SyntaxToken Current => Peek(0);

        private SyntaxToken NextToken()
        {
            SyntaxToken current = Current;
            _position++;
            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            _diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kind);
            return new SyntaxToken(_syntaxTree, kind, Current.Position, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
        }
        private SyntaxToken MatchToken(params SyntaxKind[] kinds)
        {
            foreach (SyntaxKind kind in kinds)
                if (Current.Kind == kind)
                    return NextToken();

            _diagnostics.ReportUnexpectedToken(Current.Location, Current.Kind, kinds);
            return new SyntaxToken(_syntaxTree, kinds[0], Current.Position, null, null, ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            ImmutableArray<MemberSyntax> members = ParseMembers();
            SyntaxToken endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new CompilationUnitSyntax(_syntaxTree, members, endOfFileToken);
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
            if (Current.Kind == SyntaxKind.KeywordFunction)
                throw new NotImplementedException();//return ParseFunctionDeclaration();

            return ParseGlobalStatement();
        }

        /*private MemberSyntax ParseFunctionDeclaration()
        {
            var functionKeyword = MatchToken(SyntaxKind.FunctionKeyword);
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            var parameters = ParseParameterList();
            var closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            var type = ParseOptionalTypeClause();
            var body = ParseBlockStatement();
            return new FunctionDeclarationSyntax(_syntaxTree, functionKeyword, identifier, openParenthesisToken, parameters, closeParenthesisToken, type, body);
        }

        private SeparatedSyntaxList<ParameterSyntax> ParseParameterList()
        {
            var nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            var parseNextParameter = true;
            while (parseNextParameter &&
                   Current.Kind != SyntaxKind.CloseParenthesisToken &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                var parameter = ParseParameter();
                nodesAndSeparators.Add(parameter);

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    var comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                {
                    parseNextParameter = false;
                }
            }

            return new SeparatedSyntaxList<ParameterSyntax>(nodesAndSeparators.ToImmutable());
        }

        private ParameterSyntax ParseParameter()
        {
            var identifier = MatchToken(SyntaxKind.IdentifierToken);
            var type = ParseTypeClause();
            return new ParameterSyntax(_syntaxTree, identifier, type);
        }*/

        private MemberSyntax ParseGlobalStatement()
        {
            return new GlobalStatementSyntax(_syntaxTree, ParseStatement());
        }

        private StatementSyntax ParseStatement()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                    return ParseBlockStatement();
                case SyntaxKind.KeywordOn:
                    return ParseSpecialBlockStatement();
                case SyntaxKind.KeywordFloat:
                case SyntaxKind.KeywordVector3:
                case SyntaxKind.KeywordRotation:
                case SyntaxKind.KeywordBool:
                case SyntaxKind.KeywordObject:
                case SyntaxKind.KeywordArray:
                    return ParseVariableDeclarationStatement();
                case SyntaxKind.IdentifierToken when Peek(1).Kind switch
                {
                    SyntaxKind.EqualsToken => true,
                    SyntaxKind.PlusEqualsToken => true,
                    SyntaxKind.MinusEqualsToken => true,
                    SyntaxKind.StarEqualsToken => true,
                    SyntaxKind.SlashEqualsToken => true,
                    SyntaxKind.PercentEqualsToken => true,
                    _ => false,
                }:
                    return ParseAssignmentStatement();
                case SyntaxKind.KeywordIf:
                    return ParseIfStatement();
                case SyntaxKind.KeywordWhile:
                    return ParseWhileStatement();
                //case SyntaxKind.DoKeyword:
                //    return ParseDoWhileStatement();
                //case SyntaxKind.ForKeyword:
                //    return ParseForStatement();
                case SyntaxKind.KeywordBreak:
                    return ParseBreakStatement();
                case SyntaxKind.KeywordContinue:
                    return ParseContinueStatement();
                //case SyntaxKind.ReturnKeyword:
                //    return ParseReturnStatement();
                default:
                    {
                        if (Current.Kind.IsModifier())
                            return ParseModifiers();

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

            return new BlockStatementSyntax(_syntaxTree, openBraceToken, statements.ToImmutable(), closeBraceToken);
        }

        private StatementSyntax ParseSpecialBlockStatement()
        {
            SyntaxToken onKeyword = MatchToken(SyntaxKind.KeywordOn);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            SeparatedSyntaxList<ExpressionSyntax> arguments = ParseSeparatedList(SyntaxKind.CloseParenthesisToken);
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            BlockStatementSyntax block = ParseBlockStatement();

            return new SpecialBlockStatementSyntax(_syntaxTree, onKeyword, identifier, openParenthesisToken, arguments, closeParenthesisToken, block);
        }

        private StatementSyntax ParseVariableDeclarationStatement(ImmutableArray<SyntaxToken>? modifiers = null)
        {
            TypeClauseSyntax typeClause = ParseTypeClause(true);
            SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);

            StatementSyntax? assignment = null;
            if (Current.Kind == SyntaxKind.EqualsToken)
            {
                SyntaxToken equals = MatchToken(SyntaxKind.EqualsToken);

                if (Current.Kind == SyntaxKind.OpenSquareToken)
                {
                    SyntaxToken openSquareToken = MatchToken(SyntaxKind.OpenSquareToken);
                    SeparatedSyntaxList<ExpressionSyntax> elements = ParseSeparatedList(SyntaxKind.CloseSquareToken);
                    SyntaxToken closeSquareToken = MatchToken(SyntaxKind.CloseSquareToken);

                    assignment = new ArrayInitializerStatementSyntax(_syntaxTree, identifier, equals, openSquareToken, elements, closeSquareToken);
                }
                else
                {
                    ExpressionSyntax initializer = ParseExpression();

                    assignment = new AssignmentStatementSyntax(_syntaxTree, identifier, equals, initializer);
                }
            }
            return new VariableDeclarationSyntax(_syntaxTree, modifiers ?? ImmutableArray<SyntaxToken>.Empty, typeClause, identifier, assignment);
        }

        private StatementSyntax ParseAssignmentStatement()
        {
            if (Peek(2).Kind == SyntaxKind.OpenSquareToken)
                return ParseArrayInitializerStatement();

            SyntaxToken identifierToken = NextToken();
            SyntaxToken operatorToken = NextToken();
            ExpressionSyntax right = ParseExpression();
            return new AssignmentStatementSyntax(_syntaxTree, identifierToken, operatorToken, right);
        }

        private StatementSyntax ParseArrayInitializerStatement()
        {
            SyntaxToken identifierToken = NextToken();
            SyntaxToken equalsToken = MatchToken(SyntaxKind.EqualsToken);
            SyntaxToken openSquareToken = MatchToken(SyntaxKind.OpenSquareToken);
            SeparatedSyntaxList<ExpressionSyntax> elements = ParseSeparatedList(SyntaxKind.CloseSquareToken);
            SyntaxToken closeSquareToken = MatchToken(SyntaxKind.CloseSquareToken);

            return new ArrayInitializerStatementSyntax(_syntaxTree, identifierToken, equalsToken, openSquareToken, elements, closeSquareToken);
        }

        private StatementSyntax ParseIfStatement()
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
                return null;

            SyntaxToken keyword = NextToken();
            StatementSyntax statement = ParseStatement();
            return new ElseClauseSyntax(_syntaxTree, keyword, statement);
        }

        private StatementSyntax ParseWhileStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordWhile);
            ExpressionSyntax condition = ParseExpression();
            StatementSyntax body = ParseStatement();
            return new WhileStatementSyntax(_syntaxTree, keyword, condition, body);
        }

        //private StatementSyntax ParseDoWhileStatement()
        //{
        //    var doKeyword = MatchToken(SyntaxKind.DoKeyword);
        //    var body = ParseStatement();
        //    var whileKeyword = MatchToken(SyntaxKind.WhileKeyword);
        //    var condition = ParseExpression();
        //    return new DoWhileStatementSyntax(_syntaxTree, doKeyword, body, whileKeyword, condition);
        //}

        //private StatementSyntax ParseForStatement()
        //{
        //    var keyword = MatchToken(SyntaxKind.ForKeyword);
        //    var identifier = MatchToken(SyntaxKind.IdentifierToken);
        //    var equalsToken = MatchToken(SyntaxKind.EqualsToken);
        //    var lowerBound = ParseExpression();
        //    var toKeyword = MatchToken(SyntaxKind.ToKeyword);
        //    var upperBound = ParseExpression();
        //    var body = ParseStatement();
        //    return new ForStatementSyntax(_syntaxTree, keyword, identifier, equalsToken, lowerBound, toKeyword, upperBound, body);
        //}

        private StatementSyntax ParseBreakStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordBreak);
            return new BreakStatementSyntax(_syntaxTree, keyword);
        }

        private StatementSyntax ParseContinueStatement()
        {
            SyntaxToken keyword = MatchToken(SyntaxKind.KeywordContinue);
            return new ContinueStatementSyntax(_syntaxTree, keyword);
        }

        //private StatementSyntax ParseReturnStatement()
        //{
        //    var keyword = MatchToken(SyntaxKind.ReturnKeyword);
        //    var keywordLine = _text.GetLineIndex(keyword.Span.Start);
        //    var currentLine = _text.GetLineIndex(Current.Span.Start);
        //    var isEof = Current.Kind == SyntaxKind.EndOfFileToken;
        //    var sameLine = !isEof && keywordLine == currentLine;
        //    var expression = sameLine ? ParseExpression() : null;
        //    return new ReturnStatementSyntax(_syntaxTree, keyword, expression);
        //}

        private StatementSyntax ParseModifiers()
        {
            ImmutableArray<SyntaxToken>.Builder builder = ImmutableArray.CreateBuilder<SyntaxToken>();

            while (Current.Kind.IsModifier())
                builder.Add(NextToken());

            return ParseVariableDeclarationStatement(builder.ToImmutable());
        }

        private ExpressionStatementSyntax ParseExpressionStatement()
            => new ExpressionStatementSyntax(_syntaxTree, ParseExpression());

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
                left = new UnaryExpressionSyntax(_syntaxTree, operatorToken, operand);
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
                left = new BinaryExpressionSyntax(_syntaxTree, left, operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenthesisToken:
                    return ParseParenthesizedExpression();

                case SyntaxKind.KeywordFalse:
                case SyntaxKind.KeywordTrue:
                    return ParseBooleanLiteral();

                case SyntaxKind.FloatToken:
                    return ParseNumberLiteral();

                case SyntaxKind.KeywordVector3:
                case SyntaxKind.KeywordRotation:
                    return ParseVectorConstructorExpresion();

                //case SyntaxKind.StringToken:
                //    return ParseStringLiteral();

                case SyntaxKind.IdentifierToken:
                default:
                    return ParseNameOrCallExpression();
            }
        }

        private ExpressionSyntax ParseParenthesizedExpression()
        {
            SyntaxToken left = MatchToken(SyntaxKind.OpenParenthesisToken);
            ExpressionSyntax expression = ParseExpression();
            SyntaxToken right = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ParenthesizedExpressionSyntax(_syntaxTree, left, expression, right);
        }

        private ExpressionSyntax ParseBooleanLiteral()
        {
            SyntaxToken keywordToken = MatchToken(SyntaxKind.KeywordTrue, SyntaxKind.KeywordFalse);
            bool isTrue = keywordToken.Kind == SyntaxKind.KeywordTrue;
            return new LiteralExpressionSyntax(_syntaxTree, keywordToken, isTrue);
        }

        private ExpressionSyntax ParseNumberLiteral()
            => new LiteralExpressionSyntax(_syntaxTree, MatchToken(SyntaxKind.FloatToken));

        private ExpressionSyntax ParseVectorConstructorExpresion()
        {
            SyntaxToken keywordToken = MatchToken(SyntaxKind.KeywordVector3, SyntaxKind.KeywordRotation);

            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            ExpressionSyntax expressionX = ParseExpression();
            SyntaxToken comma0Token = MatchToken(SyntaxKind.CommaToken);
            ExpressionSyntax expressionY = ParseExpression();
            SyntaxToken comma1Token = MatchToken(SyntaxKind.CommaToken);
            ExpressionSyntax expressionZ = ParseExpression();
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);

            return new ConstructorExpressionSyntax(_syntaxTree, keywordToken, openParenthesisToken, expressionX, comma0Token, expressionY, comma1Token, expressionZ, closeParenthesisToken);
        }

        //private ExpressionSyntax ParseStringLiteral()
        //    => new LiteralExpressionSyntax(_syntaxTree, MatchToken(SyntaxKind.StringToken));

        private ExpressionSyntax ParseNameOrCallExpression()
        {
            if (Peek(0).Kind == SyntaxKind.IdentifierToken &&
                (Peek(1).Kind == SyntaxKind.OpenParenthesisToken ||
                (Peek(1).Kind == SyntaxKind.LessToken && isType(Peek(2).Kind)))) // not sure of a better way to do this, neccesary becose of less than operator, make some tryParseType method that doesn't consume tokens?
                return ParseCallExpression();

            return ParseNameExpression();

            bool isType(SyntaxKind kind)
                => kind switch
                {
                    SyntaxKind.KeywordBool => true,
                    SyntaxKind.KeywordFloat => true,
                    SyntaxKind.KeywordVector3 => true,
                    SyntaxKind.KeywordRotation => true,
                    SyntaxKind.KeywordObject => true,
                    _ => false,
                };
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

            SyntaxToken openParenthesisToken = MatchToken(SyntaxKind.OpenParenthesisToken);
            SeparatedSyntaxList<ExpressionSyntax> arguments = ParseSeparatedList(SyntaxKind.CloseParenthesisToken);
            SyntaxToken closeParenthesisToken = MatchToken(SyntaxKind.CloseParenthesisToken);
            if (hasGegenericParam)
                return new CallExpressionSyntax(_syntaxTree, identifier, lessThanToken, genericType, greaterThanToken, openParenthesisToken, arguments, closeParenthesisToken);
            else
                return new CallExpressionSyntax(_syntaxTree, identifier, openParenthesisToken, arguments, closeParenthesisToken);
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseSeparatedList(SyntaxKind listEnd)
        {
            ImmutableArray<SyntaxNode>.Builder nodesAndSeparators = ImmutableArray.CreateBuilder<SyntaxNode>();

            bool parseNextArgument = true;
            while (parseNextArgument &&
                   Current.Kind != listEnd &&
                   Current.Kind != SyntaxKind.EndOfFileToken)
            {
                ExpressionSyntax expression = ParseExpression();
                nodesAndSeparators.Add(expression);

                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    SyntaxToken comma = MatchToken(SyntaxKind.CommaToken);
                    nodesAndSeparators.Add(comma);
                }
                else
                    parseNextArgument = false; // TODO: this can just be break, right?
            }

            return new SeparatedSyntaxList<ExpressionSyntax>(nodesAndSeparators.ToImmutable());
        }

        private ExpressionSyntax ParseNameExpression()
        {
            SyntaxToken identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new NameExpressionSyntax(_syntaxTree, identifierToken);
        }

        private TypeClauseSyntax ParseTypeClause(bool allowGeneric, bool gettingGenericParam = false)
        {
            SyntaxToken typeToken = MatchToken(SyntaxKind.KeywordFloat, SyntaxKind.KeywordBool, SyntaxKind.KeywordVector3, SyntaxKind.KeywordRotation, SyntaxKind.KeywordObject, SyntaxKind.KeywordArray);

            if (Current.Kind == SyntaxKind.LessToken)
            {
                if (!allowGeneric)
                    _diagnostics.ReportGenericTypeNotAllowed(typeToken.Location);
                else if (gettingGenericParam)
                    _diagnostics.ReportGenericTypeRecursion(typeToken.Location);

                SyntaxToken lessToken = MatchToken(SyntaxKind.LessToken);
                TypeClauseSyntax innerType = ParseTypeClause(allowGeneric, true);
                SyntaxToken greaterToken = MatchToken(SyntaxKind.GreaterToken);

                return new TypeClauseSyntax(_syntaxTree, typeToken, lessToken, innerType, greaterToken);
            }
            else
                return new TypeClauseSyntax(_syntaxTree, typeToken);
        }
    }
}
