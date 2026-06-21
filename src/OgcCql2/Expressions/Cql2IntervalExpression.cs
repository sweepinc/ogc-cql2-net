namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 interval literal (<c>INTERVAL(start, end)</c>).
/// </summary>
/// <remarks>
/// Each bound is a temporal-valued expression — typically a <see cref="Cql2DateExpression"/> or
/// <see cref="Cql2TimestampExpression"/>, but the spec also permits a property reference or
/// function. A <see langword="null"/> bound denotes an open (unbounded) end, written as
/// <c>".."</c> on the wire.
/// </remarks>
/// <param name="Start">The lower bound, or <see langword="null"/> when open.</param>
/// <param name="End">The upper bound, or <see langword="null"/> when open.</param>
public sealed record Cql2IntervalExpression(Cql2Expression? Start, Cql2Expression? End) : Cql2Expression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitInterval(this);

}
