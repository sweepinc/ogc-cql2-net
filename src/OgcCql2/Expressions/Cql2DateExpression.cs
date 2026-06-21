using System;

namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 date instant literal (<c>DATE('YYYY-MM-DD')</c>), a local date with no time zone.
/// </summary>
/// <param name="Value">The local date.</param>
public sealed record Cql2DateExpression(DateOnly Value) : Cql2LiteralExpression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitDate(this);

}
