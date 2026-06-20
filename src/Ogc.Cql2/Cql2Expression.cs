using System.Collections.Generic;

namespace Ogc.Cql2;

public abstract record Cql2Expression
{
    public abstract T Accept<T>(ICqlExpressionVisitor<T> visitor);
}

public interface ICqlExpressionVisitor<T>
{
    T VisitLiteral(Cql2LiteralExpression expression);
    T VisitProperty(Cql2PropertyExpression expression);
    T VisitUnary(Cql2UnaryExpression expression);
    T VisitBinary(Cql2BinaryExpression expression);
    T VisitFunctionCall(Cql2FunctionCallExpression expression);
}

public enum Cql2UnaryOperator
{
    Not
}

public enum Cql2BinaryOperator
{
    And,
    Or,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
}

public sealed record Cql2LiteralExpression(object? Value) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitLiteral(this);
}

public sealed record Cql2PropertyExpression(string Name) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitProperty(this);
}

public sealed record Cql2UnaryExpression(Cql2UnaryOperator Operator, Cql2Expression Operand) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitUnary(this);
}

public sealed record Cql2BinaryExpression(Cql2BinaryOperator Operator, Cql2Expression Left, Cql2Expression Right) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitBinary(this);
}

public sealed record Cql2FunctionCallExpression(string Name, IReadOnlyList<Cql2Expression> Arguments) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitFunctionCall(this);
}
