namespace OgcCql2;

public sealed record Cql2PropertyExpression(string Name) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitProperty(this);
}
