using Xunit;
using OgcCql2.Parsing;

namespace OgcCql2.Tests;

/// <summary>
/// Round-trip tests for CQL2 JSON parsing and formatting.
/// </summary>
public class Cql2JsonRoundTripTests
{
    /// <summary>
    /// Verifies that JSON expressions produce the expected canonical JSON and are stable across parse/format cycles.
    /// </summary>
    /// <param name="json">The input CQL2 JSON expression.</param>
    /// <param name="expectedCanonical">The expected canonical JSON after formatting.</param>
    [Theory]
    [InlineData(
        """{"op":"=","args":[{"property":"foo"},1]}""",
        """{"op":"=","args":[{"property":"foo"},1]}""")]
    [InlineData(
        """{"op":"not","args":[{"op":"=","args":[{"property":"foo"},1]}]}""",
        """{"op":"not","args":[{"op":"=","args":[{"property":"foo"},1]}]}""")]
    [InlineData(
        """{"op":"and","args":[{"op":"=","args":[{"property":"foo"},1]},{"op":"not","args":[{"op":">=","args":[{"property":"bar"},10]}]}]}""",
        """{"op":"and","args":[{"op":"=","args":[{"property":"foo"},1]},{"op":"not","args":[{"op":">=","args":[{"property":"bar"},10]}]}]}""")]
    [InlineData(
        """{"op":"or","args":[{"op":"=","args":[{"property":"foo"},1]},{"op":"=","args":[{"property":"bar"},2]}]}""",
        """{"op":"or","args":[{"op":"=","args":[{"property":"foo"},1]},{"op":"=","args":[{"property":"bar"},2]}]}""")]
    [InlineData(
        """{"op":"=","args":[{"property":"active"},true]}""",
        """{"op":"=","args":[{"property":"active"},true]}""")]
    [InlineData(
        """{"op":"=","args":[{"property":"tag"},null]}""",
        """{"op":"=","args":[{"property":"tag"},null]}""")]
    public void Json_RoundTrip_ProducesExpectedCanonicalJson(string json, string expectedCanonical)
    {
        var expression = Cql2JsonParser.Parse(json);
        var canonical = Cql2JsonFormatter.Format(expression);

        Assert.Equal(expectedCanonical, canonical);

        var reparsed = Cql2JsonParser.Parse(canonical);
        var canonicalAgain = Cql2JsonFormatter.Format(reparsed);

        Assert.Equal(canonical, canonicalAgain);
    }

    /// <summary>
    /// Verifies that parsing a null string throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void Json_Parse_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Cql2JsonParser.Parse((string)null!));
    }

    /// <summary>
    /// Verifies that malformed JSON expressions throw a FormatException.
    /// </summary>
    /// <param name="malformedJson">The malformed CQL2 JSON input.</param>
    [Theory]
    [InlineData("")]
    [InlineData("""{"op":"="}""")]
    [InlineData("""{"op":"not","args":[]}""")]
    public void Json_Parse_MalformedInput_ThrowsFormatException(string malformedJson)
    {
        Assert.Throws<FormatException>(() => Cql2JsonParser.Parse(malformedJson));
    }
}
