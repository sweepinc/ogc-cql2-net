using Xunit;
using OgcCql2.Parsing;

namespace OgcCql2.Tests;

/// <summary>
/// Round-trip tests for CQL2 JSON parsing and formatting.
/// </summary>
public class Cql2JsonRoundTripTests
{
    /// <summary>
    /// Verifies that JSON expressions round-trip through the parser and formatter.
    /// </summary>
    [Fact]
    public void Json_RoundTrip_ProducesEquivalentCanonicalJson()
    {
        const string json = """
            {
              "op": "and",
              "args": [
                { "op": "=", "args": [ { "property": "foo" }, 1 ] },
                { "op": "not", "args": [ { "op": ">=", "args": [ { "property": "bar" }, 10 ] } ] }
              ]
            }
            """;

        var expression = Cql2JsonParser.Parse(json);
        var canonical = Cql2JsonFormatter.Format(expression);
        var reparsed = Cql2JsonParser.Parse(canonical);
        var canonicalAgain = Cql2JsonFormatter.Format(reparsed);

        Assert.Equal(canonical, canonicalAgain);
    }
}
