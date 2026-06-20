namespace OgcCql2;

public sealed record Cql2BinaryExpression(Cql2BinaryOperator Operator, Cql2Expression Left, Cql2Expression Right) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitBinary(this);
}
