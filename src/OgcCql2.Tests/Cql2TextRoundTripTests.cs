using Xunit;

namespace OgcCql2.Tests;

/// <summary>
/// Round-trip tests for CQL2 text parsing and formatting.
/// </summary>
public class Cql2TextRoundTripTests
{
    /// <summary>
    /// Verifies that text expressions round-trip through the parser and formatter.
    /// </summary>
    /// <param name="input">The input CQL2 text expression.</param>
    [Theory]
    [InlineData("foo = 1")]
    [InlineData("foo = 1 AND bar <> 'x'")]
    [InlineData("NOT (foo <= 42 OR bar >= 10)")]
    [InlineData("contains(name, 'abc')")]
    public void Text_RoundTrip_ProducesEquivalentCanonicalText(string input)
    {
        var expression = Cql2TextParser.Parse(input);
        var canonical = Cql2TextFormatter.Format(expression);
        var reparsed = Cql2TextParser.Parse(canonical);
        var canonicalAgain = Cql2TextFormatter.Format(reparsed);

        Assert.Equal(canonical, canonicalAgain);
    }
}
