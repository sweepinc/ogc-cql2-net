using System;

namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 timestamp instant literal (<c>TIMESTAMP('YYYY-MM-DDThh:mm:ssZ')</c>).
/// CQL2 timestamps are always UTC.
/// </summary>
/// <param name="Value">The instant, normalized to UTC (zero offset).</param>
public sealed record Cql2TimestampExpression(DateTimeOffset Value) : Cql2LiteralExpression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitTimestamp(this);

}
