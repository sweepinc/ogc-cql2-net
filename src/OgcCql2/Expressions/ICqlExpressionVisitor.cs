namespace OgcCql2.Expressions;

/// <summary>
/// Visitor contract for traversing CQL2 expression nodes.
/// </summary>
/// <typeparam name="T">The return type produced by each visit method.</typeparam>
public interface ICqlExpressionVisitor<T>
{
    /// <summary>
    /// Visits a string literal expression.
    /// </summary>
    /// <param name="expression">The string expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitString(Cql2StringExpression expression);

    /// <summary>
    /// Visits a numeric literal expression.
    /// </summary>
    /// <param name="expression">The number expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitNumber(Cql2NumberExpression expression);

    /// <summary>
    /// Visits a boolean literal expression.
    /// </summary>
    /// <param name="expression">The boolean expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitBoolean(Cql2BooleanExpression expression);

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

    /// <summary>
    /// Visits an array expression.
    /// </summary>
    /// <param name="expression">The array expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitArray(Cql2ArrayExpression expression);

    /// <summary>
    /// Visits a date literal expression.
    /// </summary>
    /// <param name="expression">The date expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitDate(Cql2DateExpression expression);

    /// <summary>
    /// Visits a timestamp literal expression.
    /// </summary>
    /// <param name="expression">The timestamp expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitTimestamp(Cql2TimestampExpression expression);

    /// <summary>
    /// Visits an interval literal expression.
    /// </summary>
    /// <param name="expression">The interval expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitInterval(Cql2IntervalExpression expression);

    /// <summary>
    /// Visits a geometry literal expression.
    /// </summary>
    /// <param name="expression">The geometry expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitGeometry(Cql2GeometryExpression expression);

    /// <summary>
    /// Visits an <c>IS NULL</c> predicate expression.
    /// </summary>
    /// <param name="expression">The is-null expression.</param>
    /// <returns>The visitor result.</returns>
    T VisitIsNull(Cql2IsNullExpression expression);
}
