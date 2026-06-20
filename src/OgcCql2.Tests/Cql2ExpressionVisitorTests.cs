using Xunit;
using OgcCql2.Parsing;

namespace OgcCql2.Tests;

/// <summary>
/// Tests for expression visitor traversal.
/// </summary>
public class Cql2ExpressionVisitorTests
{
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
}
