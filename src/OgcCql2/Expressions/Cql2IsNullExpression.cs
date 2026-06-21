namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 <c>IS NULL</c> predicate (<c>op</c> <c>isNull</c>).
/// </summary>
/// <remarks>
/// <c>IS NOT NULL</c> is represented as a <see cref="Cql2UnaryExpression"/> with
/// <see cref="Cql2UnaryOperator.Not"/> wrapping an <see cref="Cql2IsNullExpression"/>.
/// </remarks>
/// <param name="Operand">The operand tested for null.</param>
public sealed record Cql2IsNullExpression(Cql2Expression Operand) : Cql2Expression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitIsNull(this);

}
