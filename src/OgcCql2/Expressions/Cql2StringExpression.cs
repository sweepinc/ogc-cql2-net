namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 character string literal.
/// </summary>
/// <param name="Value">The string value.</param>
public sealed record Cql2StringExpression(string Value) : Cql2LiteralExpression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitString(this);

}
