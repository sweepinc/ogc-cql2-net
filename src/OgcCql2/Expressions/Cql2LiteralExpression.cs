namespace OgcCql2.Expressions;

/// <summary>
/// Represents a literal value node.
/// </summary>
/// <param name="Value">The literal value.</param>
public sealed record Cql2LiteralExpression(object? Value) : Cql2Expression
{
    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitLiteral(this);
}
