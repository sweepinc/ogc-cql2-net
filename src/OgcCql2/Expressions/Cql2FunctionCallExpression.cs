using System.Collections.Immutable;

namespace OgcCql2.Expressions;

/// <summary>
/// Represents a function call expression node. Function names are open-ended in CQL2;
/// a server advertises the functions it supports.
/// </summary>
/// <param name="Name">The function name.</param>
/// <param name="Arguments">The function arguments.</param>
public sealed record Cql2FunctionCallExpression(string Name, ImmutableArray<Cql2Expression> Arguments) : Cql2Expression
{
    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    /// <typeparam name="T">The visitor return type.</typeparam>
    /// <param name="visitor">The visitor instance.</param>
    /// <returns>The visitor result.</returns>
    public override T Accept<T>(ICqlExpressionVisitor<T> visitor) => visitor.VisitFunctionCall(this);
}
