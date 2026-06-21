namespace OgcCql2.Expressions;

/// <summary>
/// Base type for all CQL2 expression nodes.
/// </summary>
public abstract record Cql2Expression
{
    /// <summary>
    /// Accepts a visitor and dispatches to the expression-specific visit method.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The value returned by the visitor.</returns>
    public abstract T Accept<T>(ICqlExpressionVisitor<T> visitor);
}
