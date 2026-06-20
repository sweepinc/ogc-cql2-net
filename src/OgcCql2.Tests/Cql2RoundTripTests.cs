using System.Collections.Generic;
using Xunit;

namespace OgcCql2.Tests;

/// <summary>
/// Round-trip and visitor tests for CQL2 text and JSON parsing/formatting.
/// </summary>
public class Cql2RoundTripTests
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

    /// <summary>
    /// Verifies that visitor traversal counts all nodes.
    /// </summary>
    [Fact]
    public void Visitor_WalksTree()
    {
        var expression = Cql2TextParser.Parse("foo = 1 AND bar = 2");
        var visitor = new CountingVisitor();

        var nodeCount = expression.Accept(visitor);

        Assert.Equal(7, nodeCount);
    }

    /// <summary>
    /// Visitor implementation that counts visited nodes.
    /// </summary>
    sealed class CountingVisitor : ICqlExpressionVisitor<int>
    {
        /// <summary>
        /// Visits a literal node.
        /// </summary>
        /// <param name="expression">The literal expression.</param>
        /// <returns>The node count contribution.</returns>
        public int VisitLiteral(Cql2LiteralExpression expression) => 1;

        /// <summary>
        /// Visits a property node.
        /// </summary>
        /// <param name="expression">The property expression.</param>
        /// <returns>The node count contribution.</returns>
        public int VisitProperty(Cql2PropertyExpression expression) => 1;

        /// <summary>
        /// Visits a unary node.
        /// </summary>
        /// <param name="expression">The unary expression.</param>
        /// <returns>The recursive node count.</returns>
        public int VisitUnary(Cql2UnaryExpression expression) => 1 + expression.Operand.Accept(this);

        /// <summary>
        /// Visits a binary node.
        /// </summary>
        /// <param name="expression">The binary expression.</param>
        /// <returns>The recursive node count.</returns>
        public int VisitBinary(Cql2BinaryExpression expression) => 1 + expression.Left.Accept(this) + expression.Right.Accept(this);

        /// <summary>
        /// Visits a function call node.
        /// </summary>
        /// <param name="expression">The function call expression.</param>
        /// <returns>The recursive node count.</returns>
        public int VisitFunctionCall(Cql2FunctionCallExpression expression)
        {
            var count = 1;
            foreach (var argument in expression.Arguments)
                count += argument.Accept(this);

            return count;
        }
    }
}
