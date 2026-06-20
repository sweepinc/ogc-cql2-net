using System.Collections.Generic;
using Xunit;

namespace Ogc.Cql2.Tests;

public class Cql2RoundTripTests
{
    [Theory]
    [InlineData("foo = 1")]
    [InlineData("foo = 1 AND bar <> 'x'")]
    [InlineData("NOT (foo <= 42 OR bar >= 10)")]
    [InlineData("contains(name, 'abc')")]
    public void Text_RoundTrip_ProducesEquivalentCanonicalText(string input)
    {
        var expression = Cql2.ParseText(input);
        var canonical = Cql2.ToText(expression);
        var reparsed = Cql2.ParseText(canonical);
        var canonicalAgain = Cql2.ToText(reparsed);

        Assert.Equal(canonical, canonicalAgain);
    }

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

        var expression = Cql2.ParseJson(json);
        var canonical = Cql2.ToJson(expression);
        var reparsed = Cql2.ParseJson(canonical);
        var canonicalAgain = Cql2.ToJson(reparsed);

        Assert.Equal(canonical, canonicalAgain);
    }

    [Fact]
    public void Text_And_Json_RoundTrip_ToSameCanonicalText()
    {
        const string text = "foo = 1 AND NOT bar >= 10";

        var fromText = Cql2.ParseText(text);
        var asJson = Cql2.ToJson(fromText);
        var fromJson = Cql2.ParseJson(asJson);

        Assert.Equal(Cql2.ToText(fromText), Cql2.ToText(fromJson));
    }

    [Fact]
    public void Visitor_WalksTree()
    {
        var expression = Cql2.ParseText("foo = 1 AND bar = 2");
        var visitor = new CountingVisitor();

        var nodeCount = expression.Accept(visitor);

        Assert.Equal(7, nodeCount);
    }

    private sealed class CountingVisitor : ICqlExpressionVisitor<int>
    {
        public int VisitLiteral(Cql2LiteralExpression expression) => 1;

        public int VisitProperty(Cql2PropertyExpression expression) => 1;

        public int VisitUnary(Cql2UnaryExpression expression) => 1 + expression.Operand.Accept(this);

        public int VisitBinary(Cql2BinaryExpression expression) => 1 + expression.Left.Accept(this) + expression.Right.Accept(this);

        public int VisitFunctionCall(Cql2FunctionCallExpression expression)
        {
            var count = 1;
            foreach (var argument in expression.Arguments)
            {
                count += argument.Accept(this);
            }

            return count;
        }
    }
}
