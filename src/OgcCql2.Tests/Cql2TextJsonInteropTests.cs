using Xunit;
using OgcCql2.Formatting;
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
        // NOT binds looser than comparison (booleanFactor = ["NOT"] booleanPrimary),
        // so it applies to the whole comparison without needing parentheses.
        const string text = "foo = 1 AND NOT bar >= 10";
        const string expectedCanonical = "foo = 1 AND NOT bar >= 10";

        var fromText = Cql2TextParser.Parse(text);
        var asJson = Cql2JsonFormatter.Format(fromText);
        var fromJson = Cql2JsonParser.Parse(asJson);

        var canonicalFromText = Cql2TextFormatter.Format(fromText);
        var canonicalFromJson = Cql2TextFormatter.Format(fromJson);

        Assert.Equal(expectedCanonical, canonicalFromText);
        Assert.Equal(canonicalFromText, canonicalFromJson);
    }

    /// <summary>
    /// Verifies that a JSON expression converts to the expected CQL2 text.
    /// </summary>
    [Fact]
    public void Json_Converts_ToExpectedCanonicalText()
    {
        const string json = """{"op":"=","args":[{"property":"foo"},1]}""";
        const string expectedText = "foo = 1";

        var expression = Cql2JsonParser.Parse(json);
        var text = Cql2TextFormatter.Format(expression);

        Assert.Equal(expectedText, text);
    }
}
