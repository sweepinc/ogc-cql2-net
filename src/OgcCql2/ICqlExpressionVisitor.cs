namespace OgcCql2.Expressions;

/// <summary>
/// Visitor contract for traversing CQL2 expression nodes.
/// </summary>
/// <typeparam name="T">The return type produced by each visit method.</typeparam>
public interface ICqlExpressionVisitor<T>
{
    /// <summary>
    /// Visits a literal expression.
    /// </summary>
    /// <param name="expression">The literal expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitLiteral(Cql2LiteralExpression expression);

    /// <summary>
    /// Visits a property expression.
    /// </summary>
    /// <param name="expression">The property expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitProperty(Cql2PropertyExpression expression);

    /// <summary>
    /// Visits a unary expression.
    /// </summary>
    /// <param name="expression">The unary expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitUnary(Cql2UnaryExpression expression);

    /// <summary>
    /// Visits a binary expression.
    /// </summary>
    /// <param name="expression">The binary expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitBinary(Cql2BinaryExpression expression);

    /// <summary>
    /// Visits a function call expression.
    /// </summary>
    /// <param name="expression">The function call expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitFunctionCall(Cql2FunctionCallExpression expression);
}
