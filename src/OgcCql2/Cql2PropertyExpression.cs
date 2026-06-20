namespace OgcCql2;

/// <summary>
/// Represents a property reference node.
/// </summary>
/// <param name="Name">The property name.</param>
public sealed record Cql2PropertyExpression(string Name) : Cql2Expression
{
    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitProperty(this);
}
