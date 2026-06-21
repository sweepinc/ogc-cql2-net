using Xunit;
using OgcCql2.Formatting;
using OgcCql2.Parsing;

namespace OgcCql2.Tests;

/// <summary>
/// Round-trip tests for CQL2 text parsing and formatting.
/// </summary>
public class Cql2TextRoundTripTests
{
    /// <summary>
    /// Verifies that text expressions produce the expected canonical text and are stable across parse/format cycles.
    /// </summary>
    /// <param name="input">The input CQL2 text expression.</param>
    /// <param name="expectedCanonical">The expected canonical text after formatting.</param>
    [Theory]
    [InlineData("foo = 1", "foo = 1")]
    [InlineData("foo = 1 AND bar <> 'x'", "foo = 1 AND bar <> 'x'")]
    [InlineData("NOT (foo <= 42 OR bar >= 10)", "NOT (foo <= 42 OR bar >= 10)")]
    [InlineData("contains(name, 'abc')", "contains(name, 'abc')")]
    [InlineData("foo = TRUE", "foo = TRUE")]
    [InlineData("foo = FALSE", "foo = FALSE")]
    [InlineData("foo IS NULL", "foo IS NULL")]
    [InlineData("foo IS NOT NULL", "foo IS NOT NULL")]
    [InlineData("NOT foo = 1", "NOT foo = 1")]
    [InlineData("foo = 1 AND NOT bar >= 10", "foo = 1 AND NOT bar >= 10")]
    [InlineData("foo = 1 OR bar = 2", "foo = 1 OR bar = 2")]
    [InlineData("a = 1 AND b = 2 OR c = 3", "a = 1 AND b = 2 OR c = 3")]
    [InlineData("foo = -1", "foo = -1")]
    [InlineData("foo = 3.14", "foo = 3.14")]
    [InlineData("upper(name) = 'TEST'", "upper(name) = 'TEST'")]
    [InlineData("d = DATE('2020-01-01')", "d = DATE('2020-01-01')")]
    [InlineData("ts > TIMESTAMP('2020-01-01T00:00:00Z')", "ts > TIMESTAMP('2020-01-01T00:00:00Z')")]
    [InlineData("t_intersects(event, INTERVAL('2020-01-01', '2020-12-31'))", "t_intersects(event, INTERVAL('2020-01-01', '2020-12-31'))")]
    [InlineData("t_intersects(event, INTERVAL('2020-01-01', '..'))", "t_intersects(event, INTERVAL('2020-01-01', '..'))")]
    [InlineData("a_containedby(foo, (1, 2, 3))", "a_containedby(foo, (1, 2, 3))")]
    [InlineData("s_intersects(geom, POINT(0 0))", "s_intersects(geom, POINT (0 0))")]
    public void Text_RoundTrip_ProducesExpectedCanonicalText(string input, string expectedCanonical)
    {
        var expression = Cql2TextParser.Parse(input);
        var canonical = Cql2TextFormatter.Format(expression);

        Assert.Equal(expectedCanonical, canonical);

        var reparsed = Cql2TextParser.Parse(canonical);
        var canonicalAgain = Cql2TextFormatter.Format(reparsed);

        Assert.Equal(canonical, canonicalAgain);
    }

    /// <summary>
    /// Verifies that parsing a null string throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void Text_Parse_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Cql2TextParser.Parse(null!));
    }

    /// <summary>
    /// Verifies that malformed text expressions throw a FormatException.
    /// </summary>
    /// <param name="malformedInput">The malformed CQL2 text input.</param>
    [Theory]
    [InlineData("foo =")]
    [InlineData("AND foo = 1")]
    [InlineData("foo = (")]
    [InlineData("'unterminated")]
    public void Text_Parse_MalformedInput_ThrowsFormatException(string malformedInput)
    {
        Assert.Throws<FormatException>(() => Cql2TextParser.Parse(malformedInput));
    }
}
