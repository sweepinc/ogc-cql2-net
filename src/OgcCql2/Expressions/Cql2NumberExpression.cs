namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 numeric literal. Values use <see cref="decimal"/> so that integer
/// identifiers and decimal fractions round-trip exactly within the supported range.
/// </summary>
/// <param name="Value">The numeric value.</param>
public sealed record Cql2NumberExpression(decimal Value) : Cql2LiteralExpression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitNumber(this);

}
