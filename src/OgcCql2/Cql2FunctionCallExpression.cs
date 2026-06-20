using System.Collections.Generic;

namespace OgcCql2;

public sealed record Cql2FunctionCallExpression(string Name, IReadOnlyList<Cql2Expression> Arguments) : Cql2Expression
{
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitFunctionCall(this);
}
