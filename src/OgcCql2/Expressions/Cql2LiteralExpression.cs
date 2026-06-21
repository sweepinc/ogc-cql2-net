namespace OgcCql2.Expressions;

/// <summary>
/// Base type for CQL2 literal expressions — constant scalar, temporal-instant, and spatial values.
/// </summary>
/// <remarks>
/// Concrete literals are <see cref="Cql2StringExpression"/>, <see cref="Cql2NumberExpression"/>,
/// <see cref="Cql2BooleanExpression"/>, <see cref="Cql2DateExpression"/>,
/// <see cref="Cql2TimestampExpression"/>, and <see cref="Cql2GeometryExpression"/>.
/// Composite forms whose parts may be non-constant expressions — intervals and arrays — are not
/// literals and derive from <see cref="Cql2Expression"/> directly.
/// </remarks>
public abstract record Cql2LiteralExpression : Cql2Expression;
