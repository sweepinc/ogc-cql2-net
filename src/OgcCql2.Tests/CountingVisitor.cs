using OgcCql2.Expressions;

namespace OgcCql2.Tests;

/// <summary>
/// Visitor implementation that counts visited nodes.
/// </summary>
sealed class CountingVisitor : ICqlExpressionVisitor<int>
{
    /// <summary>
    /// Visits a string node.
    /// </summary>
    /// <param name="expression">The string expression.</param>
    /// <returns>The node count contribution.</returns>
    public int VisitString(Cql2StringExpression expression) => 1;

    /// <summary>
    /// Visits a number node.
    /// </summary>
    /// <param name="expression">The number expression.</param>
    /// <returns>The node count contribution.</returns>
    public int VisitNumber(Cql2NumberExpression expression) => 1;

    /// <summary>
    /// Visits a boolean node.
    /// </summary>
    /// <param name="expression">The boolean expression.</param>
    /// <returns>The node count contribution.</returns>
    public int VisitBoolean(Cql2BooleanExpression expression) => 1;

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

    /// <summary>
    /// Visits an array node.
    /// </summary>
    /// <param name="expression">The array expression.</param>
    /// <returns>The recursive node count.</returns>
    public int VisitArray(Cql2ArrayExpression expression)
    {
        var count = 1;
        foreach (var element in expression.Elements)
            count += element.Accept(this);

        return count;
    }

    /// <summary>
    /// Visits a date node.
    /// </summary>
    /// <param name="expression">The date expression.</param>
    /// <returns>The node count contribution.</returns>
    public int VisitDate(Cql2DateExpression expression) => 1;

    /// <summary>
    /// Visits a timestamp node.
    /// </summary>
    /// <param name="expression">The timestamp expression.</param>
    /// <returns>The node count contribution.</returns>
    public int VisitTimestamp(Cql2TimestampExpression expression) => 1;

    /// <summary>
    /// Visits an interval node.
    /// </summary>
    /// <param name="expression">The interval expression.</param>
    /// <returns>The node count contribution.</returns>
    public int VisitInterval(Cql2IntervalExpression expression)
        => 1 + (expression.Start?.Accept(this) ?? 0) + (expression.End?.Accept(this) ?? 0);

    /// <summary>
    /// Visits a geometry node.
    /// </summary>
    /// <param name="expression">The geometry expression.</param>
    /// <returns>The node count contribution.</returns>
    public int VisitGeometry(Cql2GeometryExpression expression) => 1;

    /// <summary>
    /// Visits an IS NULL node.
    /// </summary>
    /// <param name="expression">The is-null expression.</param>
    /// <returns>The recursive node count.</returns>
    public int VisitIsNull(Cql2IsNullExpression expression) => 1 + expression.Operand.Accept(this);
}
