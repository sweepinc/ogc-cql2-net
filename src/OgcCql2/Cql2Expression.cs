namespace OgcCql2;

public abstract record Cql2Expression
{
    public abstract T Accept<T>(ICqlExpressionVisitor<T> visitor);
}
