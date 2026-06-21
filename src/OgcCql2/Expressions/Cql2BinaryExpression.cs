namespace OgcCql2.Expressions;

/// <summary>
/// Represents a binary expression node.
/// </summary>
/// <param name="Operator">The binary operator.</param>
/// <param name="Left">The left operand expression.</param>
/// <param name="Right">The right operand expression.</param>
public sealed record Cql2BinaryExpression(Cql2BinaryOperator Operator, Cql2Expression Left, Cql2Expression Right) : Cql2Expression
{
    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitBinary(this);
}
