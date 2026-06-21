namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 boolean literal (<c>TRUE</c> or <c>FALSE</c>).
/// </summary>
/// <param name="Value">The boolean value.</param>
public sealed record Cql2BooleanExpression(bool Value) : Cql2LiteralExpression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitBoolean(this);

}
