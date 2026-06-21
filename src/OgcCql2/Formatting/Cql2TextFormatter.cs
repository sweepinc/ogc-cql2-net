using System.Globalization;

using OgcCql2.Expressions;
using OgcCql2.Geometries;

namespace OgcCql2.Formatting;

/// <summary>
/// Formats expression nodes into canonical CQL2 text.
/// </summary>
public static class Cql2TextFormatter
{
    /// <summary>
    /// Formats an expression as canonical CQL2 text.
    /// </summary>
    /// <param name="expression">The expression to format.</param>
    /// <returns>The canonical CQL2 text representation.</returns>
    public static string Format(Cql2Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return Write(expression, parentPrecedence: 0);
    }

    /// <summary>
    /// Writes an expression with parent-precedence aware parenthesis insertion.
    /// </summary>
    /// <param name="expression">The expression to write.</param>
    /// <param name="parentPrecedence">The precedence of the parent expression.</param>
    /// <returns>The formatted expression text.</returns>
    static string Write(Cql2Expression expression, int parentPrecedence)
    {
        return expression switch
        {
            Cql2StringExpression str => Quote(EscapeString(str.Value)),
            Cql2NumberExpression number => number.Value.ToString(CultureInfo.InvariantCulture),
            Cql2BooleanExpression boolean => boolean.Value ? Cql2Syntax.True : Cql2Syntax.False,
            Cql2PropertyExpression property => property.Name,
            Cql2ArrayExpression array => InParens(string.Join(ListSeparator, array.Elements.Select(element => Write(element, 0)))),
            Cql2DateExpression date => $"{Cql2Syntax.Date}{InParens(Quote(Cql2TemporalText.FormatDate(date.Value)))}",
            Cql2TimestampExpression timestamp => $"{Cql2Syntax.Timestamp}{InParens(Quote(Cql2TemporalText.FormatTimestamp(timestamp.Value)))}",
            Cql2IntervalExpression interval => FormatInterval(interval),
            Cql2GeometryExpression geometry => GeometryIo.WriteWkt(geometry.Geometry),
            Cql2FunctionCallExpression function => $"{function.Name}{InParens(string.Join(ListSeparator, function.Arguments.Select(arg => Write(arg, 0))))}",
            Cql2IsNullExpression isNull => ParenthesizeIfNeeded($"{Write(isNull.Operand, PredicatePrecedence)} {Cql2Syntax.Is} {Cql2Syntax.Null}", PredicatePrecedence, parentPrecedence),
            Cql2UnaryExpression unary => FormatNot(unary, parentPrecedence),
            Cql2BinaryExpression binary => FormatBinary(binary, parentPrecedence),
            _ => throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}")
        };
    }

    /// <summary>
    /// Formats an interval literal with quoted instant bounds and <c>'..'</c> for open ends.
    /// </summary>
    /// <param name="interval">The interval expression.</param>
    /// <returns>The formatted interval text.</returns>
    static string FormatInterval(Cql2IntervalExpression interval)
        => $"{Cql2Syntax.Interval}{InParens($"{FormatIntervalBound(interval.Start)}{ListSeparator}{FormatIntervalBound(interval.End)}")}";

    /// <summary>
    /// Formats a single interval bound: <c>'..'</c> when open, a quoted lexical string for date/timestamp
    /// instants, or the formatted expression for property/function bounds.
    /// </summary>
    /// <param name="bound">The bound expression, or <see langword="null"/> when open.</param>
    /// <returns>The formatted bound text.</returns>
    static string FormatIntervalBound(Cql2Expression? bound)
    {
        if (bound is null)
            return Quote(Cql2Syntax.OpenBound);

        var instant = Cql2TemporalText.TryFormatInstant(bound);
        return instant is not null ? Quote(EscapeString(instant)) : Write(bound, 0);
    }

    /// <summary>Precedence assigned to comparison and IS NULL predicates.</summary>
    const int PredicatePrecedence = 4;

    /// <summary>Precedence assigned to the boolean NOT factor.</summary>
    const int NotPrecedence = 3;

    /// <summary>
    /// Formats a boolean NOT expression, rendering <c>IS NOT NULL</c> in its canonical surface form.
    /// </summary>
    /// <param name="expression">The unary expression.</param>
    /// <param name="parentPrecedence">The parent precedence.</param>
    /// <returns>The formatted text.</returns>
    static string FormatNot(Cql2UnaryExpression expression, int parentPrecedence)
    {
        if (expression.Operand is Cql2IsNullExpression isNull)
        {
            var text = $"{Write(isNull.Operand, PredicatePrecedence)} {Cql2Syntax.Is} {Cql2Syntax.Not} {Cql2Syntax.Null}";
            return ParenthesizeIfNeeded(text, PredicatePrecedence, parentPrecedence);
        }

        var inner = $"{Cql2Syntax.Not} {Write(expression.Operand, NotPrecedence)}";
        return ParenthesizeIfNeeded(inner, NotPrecedence, parentPrecedence);
    }

    /// <summary>
    /// Formats a binary expression.
    /// </summary>
    /// <param name="expression">The binary expression.</param>
    /// <param name="parentPrecedence">The parent precedence.</param>
    /// <returns>The formatted binary expression text.</returns>
    static string FormatBinary(Cql2BinaryExpression expression, int parentPrecedence)
    {
        var precedence = Precedence(expression);
        var left = Write(expression.Left, precedence);
        var right = Write(expression.Right, precedence + 1);
        var text = $"{left} {OperatorText(expression.Operator)} {right}";
        return ParenthesizeIfNeeded(text, precedence, parentPrecedence);
    }

    /// <summary>
    /// Adds parentheses when the current precedence is lower than the parent precedence.
    /// </summary>
    /// <param name="text">The expression text.</param>
    /// <param name="currentPrecedence">The current expression precedence.</param>
    /// <param name="parentPrecedence">The parent expression precedence.</param>
    /// <returns>The original or parenthesized expression text.</returns>
    static string ParenthesizeIfNeeded(string text, int currentPrecedence, int parentPrecedence)
    {
        return currentPrecedence < parentPrecedence ? InParens(text) : text;
    }

    /// <summary>
    /// Gets operator precedence for the specified expression. Higher binds tighter:
    /// OR(1) &lt; AND(2) &lt; NOT(3) &lt; comparison/IS NULL(4) &lt; atoms(5).
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The precedence value.</returns>
    static int Precedence(Cql2Expression expression)
    {
        return expression switch
        {
            Cql2BinaryExpression { Operator: Cql2BinaryOperator.Or } => 1,
            Cql2BinaryExpression { Operator: Cql2BinaryOperator.And } => 2,
            Cql2UnaryExpression => NotPrecedence,
            Cql2BinaryExpression => PredicatePrecedence,
            Cql2IsNullExpression => PredicatePrecedence,
            _ => 5
        };
    }

    /// <summary>
    /// Maps a binary operator enum value to canonical CQL2 text.
    /// </summary>
    /// <param name="op">The binary operator.</param>
    /// <returns>The operator text.</returns>
    static string OperatorText(Cql2BinaryOperator op)
    {
        return op switch
        {
            Cql2BinaryOperator.And => Cql2Syntax.And,
            Cql2BinaryOperator.Or => Cql2Syntax.Or,
            Cql2BinaryOperator.Equal => Cql2Syntax.EqualOperator,
            Cql2BinaryOperator.NotEqual => Cql2Syntax.NotEqualOperator,
            Cql2BinaryOperator.LessThan => Cql2Syntax.LessThanOperator,
            Cql2BinaryOperator.LessThanOrEqual => Cql2Syntax.LessThanOrEqualOperator,
            Cql2BinaryOperator.GreaterThan => Cql2Syntax.GreaterThanOperator,
            Cql2BinaryOperator.GreaterThanOrEqual => Cql2Syntax.GreaterThanOrEqualOperator,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    /// <summary>The rendered separator between array elements and function arguments.</summary>
    static readonly string ListSeparator = $"{Cql2Syntax.Comma} ";

    /// <summary>The CQL2 single-quote string delimiter, as text.</summary>
    static readonly string SingleQuoteText = Cql2Syntax.SingleQuote.ToString();

    /// <summary>The escaped (doubled) single-quote sequence, as text.</summary>
    static readonly string EscapedSingleQuoteText = SingleQuoteText + SingleQuoteText;

    /// <summary>
    /// Wraps a value in single quotes.
    /// </summary>
    /// <param name="value">The value to quote.</param>
    /// <returns>The quoted text.</returns>
    static string Quote(string value) => $"{Cql2Syntax.SingleQuote}{value}{Cql2Syntax.SingleQuote}";

    /// <summary>
    /// Wraps text in parentheses.
    /// </summary>
    /// <param name="inner">The text to wrap.</param>
    /// <returns>The parenthesized text.</returns>
    static string InParens(string inner) => $"{Cql2Syntax.LeftParen}{inner}{Cql2Syntax.RightParen}";

    /// <summary>
    /// Escapes single quotes within a CQL2 character string by doubling them.
    /// </summary>
    /// <param name="value">The raw string value.</param>
    /// <returns>The escaped string.</returns>
    static string EscapeString(string value) => value.Replace(SingleQuoteText, EscapedSingleQuoteText, StringComparison.Ordinal);
}
