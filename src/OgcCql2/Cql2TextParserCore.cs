using System;
using System.Collections.Generic;
using OgcCql2.Expressions;

namespace OgcCql2;

/// <summary>
/// Recursive-descent parser over tokenized CQL2 text input.
/// </summary>
sealed class Cql2TextParserCore
{
    readonly Cql2TextLexer _lexer;
    Cql2TextToken _current;

    /// <summary>
    /// Initializes a parser instance.
    /// </summary>
    /// <param name="text">The CQL2 text input.</param>
    public Cql2TextParserCore(string text)
    {
        _lexer = new Cql2TextLexer(text);
        _current = _lexer.Next();
    }

    /// <summary>
    /// Parses an entire expression and validates end-of-input.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    public Cql2Expression Parse()
    {
        var expr = ParseOr();
        Expect(Cql2TextTokenKind.End);
        return expr;
    }

    /// <summary>
    /// Parses disjunction expressions.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParseOr()
    {
        var expr = ParseAnd();
        while (Match(Cql2TextTokenKind.Or))
            expr = new Cql2BinaryExpression(Cql2BinaryOperator.Or, expr, ParseAnd());

        return expr;
    }

    /// <summary>
    /// Parses conjunction expressions.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParseAnd()
    {
        var expr = ParseComparison();
        while (Match(Cql2TextTokenKind.And))
            expr = new Cql2BinaryExpression(Cql2BinaryOperator.And, expr, ParseComparison());

        return expr;
    }

    /// <summary>
    /// Parses comparison expressions.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParseComparison()
    {
        var left = ParseUnary();

        if (Match(Cql2TextTokenKind.Equal))
            return new Cql2BinaryExpression(Cql2BinaryOperator.Equal, left, ParseUnary());

        if (Match(Cql2TextTokenKind.NotEqual))
            return new Cql2BinaryExpression(Cql2BinaryOperator.NotEqual, left, ParseUnary());

        if (Match(Cql2TextTokenKind.Less))
            return new Cql2BinaryExpression(Cql2BinaryOperator.LessThan, left, ParseUnary());

        if (Match(Cql2TextTokenKind.LessOrEqual))
            return new Cql2BinaryExpression(Cql2BinaryOperator.LessThanOrEqual, left, ParseUnary());

        if (Match(Cql2TextTokenKind.Greater))
            return new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThan, left, ParseUnary());

        if (Match(Cql2TextTokenKind.GreaterOrEqual))
            return new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThanOrEqual, left, ParseUnary());

        return left;
    }

    /// <summary>
    /// Parses unary operators.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParseUnary()
    {
        if (Match(Cql2TextTokenKind.Not))
            return new Cql2UnaryExpression(Cql2UnaryOperator.Not, ParseUnary());

        return ParsePrimary();
    }

    /// <summary>
    /// Parses parenthesized expressions, literals, properties, and function calls.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParsePrimary()
    {
        if (Match(Cql2TextTokenKind.LeftParen))
        {
            var expr = ParseOr();
            Expect(Cql2TextTokenKind.RightParen);
            return expr;
        }

        if (_current.Kind == Cql2TextTokenKind.String ||
            _current.Kind == Cql2TextTokenKind.Number ||
            _current.Kind == Cql2TextTokenKind.True ||
            _current.Kind == Cql2TextTokenKind.False ||
            _current.Kind == Cql2TextTokenKind.Null)
        {
            var literal = _current.Value;
            Advance();
            return new Cql2LiteralExpression(literal);
        }

        if (_current.Kind == Cql2TextTokenKind.Identifier)
        {
            var name = _current.Text;
            Advance();

            if (Match(Cql2TextTokenKind.LeftParen))
            {
                var args = new List<Cql2Expression>();
                if (!Match(Cql2TextTokenKind.RightParen))
                {
                    do
                        args.Add(ParseOr());
                    while (Match(Cql2TextTokenKind.Comma));

                    Expect(Cql2TextTokenKind.RightParen);
                }

                return new Cql2FunctionCallExpression(name, args);
            }

            return new Cql2PropertyExpression(name);
        }

        throw new FormatException($"Unexpected token '{_current.Text}'");
    }

    /// <summary>
    /// Matches and consumes the current token if it is the expected kind.
    /// </summary>
    /// <param name="kind">The expected token kind.</param>
    /// <returns><see langword="true"/> when matched; otherwise <see langword="false"/>.</returns>
    bool Match(Cql2TextTokenKind kind)
    {
        if (_current.Kind != kind)
            return false;

        Advance();
        return true;
    }

    /// <summary>
    /// Ensures the current token matches the expected kind.
    /// </summary>
    /// <param name="kind">The expected token kind.</param>
    void Expect(Cql2TextTokenKind kind)
    {
        if (!Match(kind))
            throw new FormatException($"Expected {kind} but found '{_current.Text}'");
    }

    /// <summary>
    /// Advances to the next token.
    /// </summary>
    void Advance() => _current = _lexer.Next();
}
