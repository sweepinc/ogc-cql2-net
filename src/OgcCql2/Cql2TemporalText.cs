using System.Globalization;

using OgcCql2.Expressions;

namespace OgcCql2;

/// <summary>
/// Shared parsing and formatting for CQL2 temporal lexical forms (dates, timestamps, and
/// interval bounds), used by both the text and JSON parsers/formatters.
/// </summary>
static class Cql2TemporalText
{

    const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Parses a CQL2 date string (<c>YYYY-MM-DD</c>).
    /// </summary>
    /// <param name="value">The date string.</param>
    /// <returns>The parsed date.</returns>
    public static DateOnly ParseDate(string value)
    {
        if (!DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new FormatException($"Invalid CQL2 date '{value}'.");

        return date;
    }

    /// <summary>
    /// Parses a CQL2 timestamp string (RFC-3339 UTC, e.g. <c>YYYY-MM-DDThh:mm:ssZ</c>).
    /// </summary>
    /// <param name="value">The timestamp string.</param>
    /// <returns>The parsed instant normalized to UTC.</returns>
    public static DateTimeOffset ParseTimestamp(string value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
            throw new FormatException($"Invalid CQL2 timestamp '{value}'.");

        return timestamp;
    }

    /// <summary>
    /// Formats a date in CQL2 lexical form.
    /// </summary>
    /// <param name="value">The date.</param>
    /// <returns>The <c>YYYY-MM-DD</c> string.</returns>
    public static string FormatDate(DateOnly value) => value.ToString(DateFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a timestamp in CQL2 lexical form (UTC, trailing <c>Z</c>, fractional seconds only when present).
    /// </summary>
    /// <param name="value">The instant.</param>
    /// <returns>The RFC-3339 UTC string.</returns>
    public static string FormatTimestamp(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        var seconds = utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        var fraction = utc.ToString("FFFFFFF", CultureInfo.InvariantCulture);
        return fraction.Length == 0 ? $"{seconds}Z" : $"{seconds}.{fraction}Z";
    }

    /// <summary>
    /// Interprets an instant string as a typed temporal expression (date when no time component
    /// is present, otherwise a timestamp).
    /// </summary>
    /// <param name="value">The instant string.</param>
    /// <returns>The temporal expression.</returns>
    public static Cql2Expression ParseInstant(string value)
    {
        return value.Contains('T')
            ? new Cql2TimestampExpression(ParseTimestamp(value))
            : new Cql2DateExpression(ParseDate(value));
    }

    /// <summary>
    /// Formats a temporal instant expression as its CQL2 lexical string (used inside intervals).
    /// </summary>
    /// <param name="expression">A <see cref="Cql2DateExpression"/> or <see cref="Cql2TimestampExpression"/>.</param>
    /// <returns>The lexical string, or <see langword="null"/> when the expression is not a temporal instant.</returns>
    public static string? TryFormatInstant(Cql2Expression expression)
    {
        return expression switch
        {
            Cql2DateExpression date => FormatDate(date.Value),
            Cql2TimestampExpression timestamp => FormatTimestamp(timestamp.Value),
            _ => null
        };
    }

}
