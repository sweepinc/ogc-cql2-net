namespace OgcCql2;

public interface ICqlExpressionVisitor<T>
{
    T VisitLiteral(Cql2LiteralExpression expression);
    T VisitProperty(Cql2PropertyExpression expression);
    T VisitUnary(Cql2UnaryExpression expression);
    T VisitBinary(Cql2BinaryExpression expression);
    T VisitFunctionCall(Cql2FunctionCallExpression expression);
}
