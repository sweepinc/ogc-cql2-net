using System.Collections.Immutable;

namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 array expression whose elements are themselves expressions.
/// </summary>
/// <param name="Elements">The array element expressions.</param>
public sealed record Cql2ArrayExpression(ImmutableArray<Cql2Expression> Elements) : Cql2Expression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitArray(this);

}
