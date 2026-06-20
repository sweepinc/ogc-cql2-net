namespace OgcCql2;

public sealed record Cql2UnaryExpression(Cql2UnaryOperator Operator, Cql2Expression Operand) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitUnary(this);
}
