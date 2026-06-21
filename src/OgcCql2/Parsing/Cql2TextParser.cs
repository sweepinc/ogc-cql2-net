using System.Collections.Immutable;
using System.Globalization;

using OgcCql2.Expressions;
using OgcCql2.Geometries;

namespace OgcCql2.Parsing;

/// <summary>
/// Recursive-descent parser over a span of CQL2 text. Tokens slice directly from the source;
/// strings are allocated only when a parsed AST node requires one.
/// </summary>
/// <remarks>
/// Use the static <see cref="Parse(string)"/> for the common case, or construct the parser over a
/// <see cref="ReadOnlySpan{Char}"/> and call <see cref="Parse()"/> to parse a slice without
/// allocating a backing string.
/// </remarks>
public ref struct Cql2TextParser
{

    Cql2TextLexer _lexer;
    Cql2TextToken _current;

    /// <summary>
    /// Initializes a parser over a span of CQL2 text.
    /// </summary>
    /// <param name="text">The CQL2 text input.</param>
    public Cql2TextParser(ReadOnlySpan<char> text)
    {
        _lexer = new Cql2TextLexer(text);
        _current = _lexer.Next();
    }

    /// <summary>
    /// Parses a CQL2 text expression.
    /// </summary>
    /// <param name="text">The CQL2 text input.</param>
    /// <returns>The parsed expression tree.</returns>
    public static Cql2Expression Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new Cql2TextParser(text).Parse();
    }

    /// <summary>
    /// Parses the entire input as an expression and validates end-of-input.
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
        var expr = ParseNot();
        while (Match(Cql2TextTokenKind.And))
            expr = new Cql2BinaryExpression(Cql2BinaryOperator.And, expr, ParseNot());

        return expr;
    }

    /// <summary>
    /// Parses the boolean NOT factor. Per <c>booleanFactor = ["NOT"] booleanPrimary</c>,
    /// NOT binds looser than comparison but tighter than AND.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParseNot()
    {
        if (Match(Cql2TextTokenKind.Not))
            return new Cql2UnaryExpression(Cql2UnaryOperator.Not, ParseNot());

        return ParseComparison();
    }

    /// <summary>
    /// Parses comparison and <c>IS [NOT] NULL</c> predicates.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParseComparison()
    {
        var left = ParsePrimary();

        if (Match(Cql2TextTokenKind.Equal))
            return new Cql2BinaryExpression(Cql2BinaryOperator.Equal, left, ParsePrimary());

        if (Match(Cql2TextTokenKind.NotEqual))
            return new Cql2BinaryExpression(Cql2BinaryOperator.NotEqual, left, ParsePrimary());

        if (Match(Cql2TextTokenKind.Less))
            return new Cql2BinaryExpression(Cql2BinaryOperator.LessThan, left, ParsePrimary());

        if (Match(Cql2TextTokenKind.LessOrEqual))
            return new Cql2BinaryExpression(Cql2BinaryOperator.LessThanOrEqual, left, ParsePrimary());

        if (Match(Cql2TextTokenKind.Greater))
            return new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThan, left, ParsePrimary());

        if (Match(Cql2TextTokenKind.GreaterOrEqual))
            return new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThanOrEqual, left, ParsePrimary());

        if (Match(Cql2TextTokenKind.Is))
            return ParseIsNull(left);

        return left;
    }

    /// <summary>
    /// Parses the remainder of an <c>IS [NOT] NULL</c> predicate after the <c>IS</c> keyword.
    /// </summary>
    /// <param name="operand">The operand tested for null.</param>
    /// <returns>The parsed predicate.</returns>
    Cql2Expression ParseIsNull(Cql2Expression operand)
    {
        var negated = Match(Cql2TextTokenKind.Not);
        Expect(Cql2TextTokenKind.Null);
        Cql2Expression predicate = new Cql2IsNullExpression(operand);
        return negated ? new Cql2UnaryExpression(Cql2UnaryOperator.Not, predicate) : predicate;
    }

    /// <summary>
    /// Parses parenthesized groups, arrays, literals, geometry, temporal constructors,
    /// properties, and function calls.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParsePrimary()
    {
        if (Match(Cql2TextTokenKind.LeftParen))
            return ParseParenthesized();

        if (_current.Kind == Cql2TextTokenKind.Geometry)
        {
            var wkt = _current.Text.ToString();
            Advance();
            return new Cql2GeometryExpression(GeometryIo.ReadWkt(wkt));
        }

        if (_current.Kind == Cql2TextTokenKind.String)
        {
            var value = UnescapeString(_current.Text);
            Advance();
            return new Cql2StringExpression(value);
        }

        if (_current.Kind == Cql2TextTokenKind.Number)
        {
            var value = ParseDecimal(_current.Text);
            Advance();
            return new Cql2NumberExpression(value);
        }

        if (_current.Kind is Cql2TextTokenKind.True or Cql2TextTokenKind.False)
        {
            var value = _current.Kind == Cql2TextTokenKind.True;
            Advance();
            return new Cql2BooleanExpression(value);
        }

        if (_current.Kind == Cql2TextTokenKind.Identifier)
        {
            var name = _current.Text.ToString();
            Advance();

            if (Match(Cql2TextTokenKind.LeftParen))
                return ParseCallOrConstructor(name);

            return new Cql2PropertyExpression(name);
        }

        throw new FormatException($"Unexpected token '{_current.Text}'");
    }

    /// <summary>
    /// Parses a parenthesized group or array literal after the opening parenthesis.
    /// A single inner expression is a grouping; a comma-separated list is an array.
    /// </summary>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParseParenthesized()
    {
        if (Match(Cql2TextTokenKind.RightParen))
            return new Cql2ArrayExpression(ImmutableArray<Cql2Expression>.Empty);

        var first = ParseOr();
        if (_current.Kind != Cql2TextTokenKind.Comma)
        {
            Expect(Cql2TextTokenKind.RightParen);
            return first;
        }

        var elements = ImmutableArray.CreateBuilder<Cql2Expression>();
        elements.Add(first);
        while (Match(Cql2TextTokenKind.Comma))
            elements.Add(ParseOr());

        Expect(Cql2TextTokenKind.RightParen);
        return new Cql2ArrayExpression(elements.ToImmutable());
    }

    /// <summary>
    /// Parses a function call, or a DATE/TIMESTAMP/INTERVAL temporal literal constructor,
    /// after the function name and opening parenthesis have been consumed.
    /// </summary>
    /// <param name="name">The identifier preceding the parenthesis.</param>
    /// <returns>The parsed expression.</returns>
    Cql2Expression ParseCallOrConstructor(string name)
    {
        switch (name.ToUpperInvariant())
        {
            case Cql2Syntax.Date:
                {
                    var value = ExpectStringLiteral();
                    Expect(Cql2TextTokenKind.RightParen);
                    return new Cql2DateExpression(Cql2TemporalText.ParseDate(value));
                }
            case Cql2Syntax.Timestamp:
                {
                    var value = ExpectStringLiteral();
                    Expect(Cql2TextTokenKind.RightParen);
                    return new Cql2TimestampExpression(Cql2TemporalText.ParseTimestamp(value));
                }
            case Cql2Syntax.Interval:
                {
                    var start = ParseIntervalBound();
                    Expect(Cql2TextTokenKind.Comma);
                    var end = ParseIntervalBound();
                    Expect(Cql2TextTokenKind.RightParen);
                    return new Cql2IntervalExpression(start, end);
                }
        }

        var args = ImmutableArray.CreateBuilder<Cql2Expression>();
        if (!Match(Cql2TextTokenKind.RightParen))
        {
            do
                args.Add(ParseOr());
            while (Match(Cql2TextTokenKind.Comma));

            Expect(Cql2TextTokenKind.RightParen);
        }

        return new Cql2FunctionCallExpression(name, args.ToImmutable());
    }

    /// <summary>
    /// Parses a single interval bound. A <c>'..'</c> string denotes an open bound (null); a date
    /// or timestamp string becomes a typed temporal node; anything else is a general expression
    /// (such as a property reference or function).
    /// </summary>
    /// <returns>The bound expression, or <see langword="null"/> when open.</returns>
    Cql2Expression? ParseIntervalBound()
    {
        if (_current.Kind == Cql2TextTokenKind.String)
        {
            var value = UnescapeString(_current.Text);
            Advance();
            return value == Cql2Syntax.OpenBound ? null : Cql2TemporalText.ParseInstant(value);
        }

        return ParseOr();
    }

    /// <summary>
    /// Consumes the current token as a string literal and returns its unescaped value.
    /// </summary>
    /// <returns>The string literal value.</returns>
    string ExpectStringLiteral()
    {
        if (_current.Kind != Cql2TextTokenKind.String)
            throw new FormatException($"Expected a string literal but found '{_current.Text}'.");

        var value = UnescapeString(_current.Text);
        Advance();
        return value;
    }

    /// <summary>
    /// Unescapes a CQL2 string literal's raw inner content by collapsing doubled single quotes.
    /// </summary>
    /// <param name="content">The raw inner content span.</param>
    /// <returns>The unescaped string.</returns>
    static string UnescapeString(ReadOnlySpan<char> content)
    {
        return content.IndexOf(Cql2Syntax.SingleQuote) < 0
            ? content.ToString()
            : content.ToString().Replace("''", "'");
    }

    /// <summary>
    /// Parses a numeric literal span as a <see cref="decimal"/>.
    /// </summary>
    /// <param name="text">The numeric span.</param>
    /// <returns>The parsed value.</returns>
    static decimal ParseDecimal(ReadOnlySpan<char> text)
    {
        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new FormatException($"Invalid numeric literal '{text}'.");

        return value;
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
