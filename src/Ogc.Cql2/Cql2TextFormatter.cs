using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ogc.Cql2;

public static class Cql2TextFormatter
{
    public static string Format(Cql2Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return Write(expression, parentPrecedence: 0);
    }

    private static string Write(Cql2Expression expression, int parentPrecedence)
    {
        return expression switch
        {
            Cql2LiteralExpression literal => FormatLiteral(literal.Value),
            Cql2PropertyExpression property => property.Name,
            Cql2FunctionCallExpression function => $"{function.Name}({string.Join(", ", function.Arguments.Select(arg => Write(arg, 0)))})",
            Cql2UnaryExpression unary => ParenthesizeIfNeeded($"NOT {Write(unary.Operand, Precedence(unary))}", Precedence(unary), parentPrecedence),
            Cql2BinaryExpression binary => FormatBinary(binary, parentPrecedence),
            _ => throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}")
        };
    }

    private static string FormatBinary(Cql2BinaryExpression expression, int parentPrecedence)
    {
        var precedence = Precedence(expression);
        var left = Write(expression.Left, precedence);
        var right = Write(expression.Right, precedence + 1);
        var text = $"{left} {OperatorText(expression.Operator)} {right}";
        return ParenthesizeIfNeeded(text, precedence, parentPrecedence);
    }

    private static string ParenthesizeIfNeeded(string text, int currentPrecedence, int parentPrecedence)
    {
        return currentPrecedence < parentPrecedence ? $"({text})" : text;
    }

    private static int Precedence(Cql2Expression expression)
    {
        return expression switch
        {
            Cql2BinaryExpression { Operator: Cql2BinaryOperator.Or } => 1,
            Cql2BinaryExpression { Operator: Cql2BinaryOperator.And } => 2,
            Cql2BinaryExpression => 3,
            Cql2UnaryExpression => 4,
            _ => 5
        };
    }

    private static string OperatorText(Cql2BinaryOperator op)
    {
        return op switch
        {
            Cql2BinaryOperator.And => "AND",
            Cql2BinaryOperator.Or => "OR",
            Cql2BinaryOperator.Equal => "=",
            Cql2BinaryOperator.NotEqual => "<>",
            Cql2BinaryOperator.LessThan => "<",
            Cql2BinaryOperator.LessThanOrEqual => "<=",
            Cql2BinaryOperator.GreaterThan => ">",
            Cql2BinaryOperator.GreaterThanOrEqual => ">=",
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    private static string FormatLiteral(object? value)
    {
        return value switch
        {
            null => "NULL",
            bool boolean => boolean ? "TRUE" : "FALSE",
            string text => $"'{text.Replace("'", "''", StringComparison.Ordinal)}'",
            sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                Convert.ToString(value, CultureInfo.InvariantCulture)!,
            IReadOnlyList<object?> list => $"[{string.Join(", ", list.Select(FormatLiteral))}]",
            _ => throw new NotSupportedException($"Unsupported literal type: {value.GetType().Name}")
        };
    }
}
