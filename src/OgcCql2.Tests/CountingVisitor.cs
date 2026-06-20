using OgcCql2.Expressions;

namespace OgcCql2.Tests;

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
