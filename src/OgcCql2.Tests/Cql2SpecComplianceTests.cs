using System;
using Xunit;
using OgcCql2;
using OgcCql2.Expressions;
using OgcCql2.Formatting;
using OgcCql2.Parsing;

namespace OgcCql2.Tests;

/// <summary>
/// Tests covering specific OGC CQL2 conformance points: NOT precedence, the IS NULL predicate,
/// temporal and spatial literals, arrays, and the absence of a null literal.
/// </summary>
public class Cql2SpecComplianceTests
{
    /// <summary>
    /// Verifies that NOT binds looser than comparison: <c>NOT a = b</c> means <c>NOT (a = b)</c>.
    /// </summary>
    [Fact]
    public void Not_BindsLooserThanComparison()
    {
        var expression = Cql2TextParser.Parse("NOT bar >= 10");

        var unary = Assert.IsType<Cql2UnaryExpression>(expression);
        Assert.Equal(Cql2UnaryOperator.Not, unary.Operator);
        var comparison = Assert.IsType<Cql2BinaryExpression>(unary.Operand);
        Assert.Equal(Cql2BinaryOperator.GreaterThanOrEqual, comparison.Operator);
    }

    /// <summary>
    /// Verifies that IS NULL parses to an is-null predicate and emits the canonical <c>isNull</c> JSON.
    /// </summary>
    [Fact]
    public void IsNull_ProducesIsNullPredicateAndJson()
    {
        var expression = Cql2TextParser.Parse("foo IS NULL");

        var isNull = Assert.IsType<Cql2IsNullExpression>(expression);
        Assert.IsType<Cql2PropertyExpression>(isNull.Operand);
        Assert.Equal("""{"op":"isNull","args":[{"property":"foo"}]}""", Cql2JsonFormatter.Format(expression));
    }

    /// <summary>
    /// Verifies that IS NOT NULL is modeled as NOT(isNull) and round-trips to canonical text.
    /// </summary>
    [Fact]
    public void IsNotNull_IsNotOfIsNull()
    {
        var expression = Cql2TextParser.Parse("foo IS NOT NULL");

        var unary = Assert.IsType<Cql2UnaryExpression>(expression);
        Assert.IsType<Cql2IsNullExpression>(unary.Operand);
        Assert.Equal("foo IS NOT NULL", Cql2TextFormatter.Format(expression));
    }

    /// <summary>
    /// Verifies that CQL2 has no null literal in either syntax.
    /// </summary>
    [Theory]
    [InlineData("foo = NULL")]
    public void NullLiteral_InText_IsRejected(string input)
    {
        Assert.Throws<FormatException>(() => Cql2TextParser.Parse(input));
    }

    /// <summary>
    /// Verifies that a bare JSON null is rejected as a literal.
    /// </summary>
    [Fact]
    public void NullLiteral_InJson_IsRejected()
    {
        Assert.Throws<FormatException>(
            () => Cql2JsonParser.Parse("""{"op":"=","args":[{"property":"tag"},null]}"""));
    }

    /// <summary>
    /// Verifies that scientific-notation numbers are parsed.
    /// </summary>
    [Fact]
    public void ScientificNotation_IsParsed()
    {
        var expression = Cql2TextParser.Parse("foo = 1.5e-9");

        var comparison = Assert.IsType<Cql2BinaryExpression>(expression);
        var number = Assert.IsType<Cql2NumberExpression>(comparison.Right);
        Assert.Equal(1.5e-9m, number.Value);
    }

    /// <summary>
    /// Verifies that temporal literals carry distinct node types rather than being strings.
    /// </summary>
    [Fact]
    public void TemporalLiterals_HaveDistinctNodeTypes()
    {
        var date = Cql2TextParser.Parse("DATE('2020-01-01')");
        var timestamp = Cql2TextParser.Parse("TIMESTAMP('2020-01-01T00:00:00Z')");
        var interval = Cql2JsonParser.Parse("""{"interval":["2020-01-01",".."]}""");

        Assert.IsType<Cql2DateExpression>(date);
        Assert.IsType<Cql2TimestampExpression>(timestamp);
        var parsedInterval = Assert.IsType<Cql2IntervalExpression>(interval);
        Assert.IsType<Cql2DateExpression>(parsedInterval.Start);
        Assert.Null(parsedInterval.End);
    }

    /// <summary>
    /// Verifies that arrays hold expression elements (including property references).
    /// </summary>
    [Fact]
    public void Array_HoldsExpressionElements()
    {
        var expression = Cql2JsonParser.Parse("""{"op":"a_containedby","args":[{"property":"foo"},[{"property":"bar"},1]]}""");

        var function = Assert.IsType<Cql2FunctionCallExpression>(expression);
        var array = Assert.IsType<Cql2ArrayExpression>(function.Arguments[1]);
        Assert.IsType<Cql2PropertyExpression>(array.Elements[0]);
        Assert.IsType<Cql2NumberExpression>(array.Elements[1]);
    }

    /// <summary>
    /// Verifies that constant literals share the Cql2LiteralExpression base, while composite
    /// interval/array forms do not.
    /// </summary>
    [Fact]
    public void LiteralBase_GroupsConstantsOnly()
    {
        Assert.IsAssignableFrom<Cql2LiteralExpression>(Cql2TextParser.Parse("'x'"));
        Assert.IsAssignableFrom<Cql2LiteralExpression>(Cql2TextParser.Parse("1"));
        Assert.IsAssignableFrom<Cql2LiteralExpression>(Cql2TextParser.Parse("TRUE"));
        Assert.IsAssignableFrom<Cql2LiteralExpression>(Cql2TextParser.Parse("DATE('2020-01-01')"));
        Assert.IsAssignableFrom<Cql2LiteralExpression>(Cql2TextParser.Parse("TIMESTAMP('2020-01-01T00:00:00Z')"));
        Assert.IsAssignableFrom<Cql2LiteralExpression>(Cql2TextParser.Parse("POINT(0 0)"));

        Assert.IsNotAssignableFrom<Cql2LiteralExpression>(Cql2TextParser.Parse("(1, 2, 3)"));
        Assert.IsNotAssignableFrom<Cql2LiteralExpression>(Cql2TextParser.Parse("INTERVAL('2020-01-01', '..')"));
    }

    /// <summary>
    /// Verifies that the text parser accepts a character span sliced from a larger buffer.
    /// </summary>
    [Fact]
    public void TextParser_AcceptsSpanSlice()
    {
        var slice = "xx foo = 1 xx".AsSpan(3, 7);

        var expression = new Cql2TextParser(slice).Parse();

        Assert.Equal("foo = 1", Cql2TextFormatter.Format(expression));
    }

    /// <summary>
    /// Verifies that doubled single quotes inside a string literal are unescaped.
    /// </summary>
    [Fact]
    public void StringLiteral_UnescapesDoubledQuotes()
    {
        var expression = Cql2TextParser.Parse("name = 'it''s'");

        var comparison = Assert.IsType<Cql2BinaryExpression>(expression);
        var str = Assert.IsType<Cql2StringExpression>(comparison.Right);
        Assert.Equal("it's", str.Value);
        Assert.Equal("name = 'it''s'", Cql2TextFormatter.Format(expression));
    }

    /// <summary>
    /// Verifies that a geometry round-trips WKT (text) to GeoJSON (JSON) through the shared AST.
    /// </summary>
    [Fact]
    public void Geometry_RoundTripsAcrossTextAndJson()
    {
        var fromText = Cql2TextParser.Parse("s_intersects(geom, POINT(1 2))");
        var json = Cql2JsonFormatter.Format(fromText);

        Assert.Contains("\"type\":\"Point\"", json);
        Assert.Contains("\"coordinates\":[1,2]", json);

        var fromJson = Cql2JsonParser.Parse(json);
        Assert.Equal(Cql2TextFormatter.Format(fromText), Cql2TextFormatter.Format(fromJson));
    }
}
