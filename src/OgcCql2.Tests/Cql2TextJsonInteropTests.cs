using Xunit;
using OgcCql2.Parsing;

namespace OgcCql2.Tests;

/// <summary>
/// Interop tests across CQL2 text and JSON using the shared AST.
/// </summary>
public class Cql2TextJsonInteropTests
{
    /// <summary>
    /// Verifies that text and JSON round-trip to the same canonical text.
    /// </summary>
    [Fact]
    public void Text_And_Json_RoundTrip_ToSameCanonicalText()
    {
        const string text = "foo = 1 AND NOT bar >= 10";

        var fromText = Cql2TextParser.Parse(text);
        var asJson = Cql2JsonFormatter.Format(fromText);
        var fromJson = Cql2JsonParser.Parse(asJson);

        Assert.Equal(Cql2TextFormatter.Format(fromText), Cql2TextFormatter.Format(fromJson));
    }
}
