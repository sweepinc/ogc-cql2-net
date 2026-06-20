namespace OgcCql2;

public sealed record Cql2LiteralExpression(object? Value) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitLiteral(this);
}
