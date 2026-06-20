namespace OgcCql2;

/// <summary>
/// Represents a unary expression node.
/// </summary>
/// <param name="Operator">The unary operator.</param>
/// <param name="Operand">The operand expression.</param>
public sealed record Cql2UnaryExpression(Cql2UnaryOperator Operator, Cql2Expression Operand) : Cql2Expression
{
    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitUnary(this);
}
