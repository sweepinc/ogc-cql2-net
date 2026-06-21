using NetTopologySuite.Geometries;

namespace OgcCql2.Expressions;

/// <summary>
/// Represents a CQL2 spatial literal: WKT in CQL2-Text, GeoJSON in CQL2-JSON.
/// The geometry is a NetTopologySuite <see cref="Geometry"/>.
/// </summary>
/// <param name="Geometry">The geometry value.</param>
public sealed record Cql2GeometryExpression(Geometry Geometry) : Cql2LiteralExpression
{

    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitGeometry(this);

}
