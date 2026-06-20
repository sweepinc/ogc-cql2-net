using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OgcCql2;

/// <summary>
/// Formats expression nodes into canonical CQL2 text.
/// </summary>
public static class Cql2TextFormatter
{
    /// <summary>
    /// Formats an expression as canonical CQL2 text.
    /// </summary>
    /// <param name="expression">The expression to format.</param>
    /// <returns>The canonical CQL2 text representation.</returns>
    public static string Format(Cql2Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return Write(expression, parentPrecedence: 0);
    }

    /// <summary>
    /// Writes an expression with parent-precedence aware parenthesis insertion.
    /// </summary>
    /// <param name="expression">The expression to write.</param>
    /// <param name="parentPrecedence">The precedence of the parent expression.</param>
    /// <returns>The formatted expression text.</returns>
    static string Write(Cql2Expression expression, int parentPrecedence)
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

    /// <summary>
    /// Formats a binary expression.
    /// </summary>
    /// <param name="expression">The binary expression.</param>
    /// <param name="parentPrecedence">The parent precedence.</param>
    /// <returns>The formatted binary expression text.</returns>
    static string FormatBinary(Cql2BinaryExpression expression, int parentPrecedence)
    {
        var precedence = Precedence(expression);
        var left = Write(expression.Left, precedence);
        var right = Write(expression.Right, precedence + 1);
        var text = $"{left} {OperatorText(expression.Operator)} {right}";
        return ParenthesizeIfNeeded(text, precedence, parentPrecedence);
    }

    /// <summary>
    /// Adds parentheses when the current precedence is lower than the parent precedence.
    /// </summary>
    /// <param name="text">The expression text.</param>
    /// <param name="currentPrecedence">The current expression precedence.</param>
    /// <param name="parentPrecedence">The parent expression precedence.</param>
    /// <returns>The original or parenthesized expression text.</returns>
    static string ParenthesizeIfNeeded(string text, int currentPrecedence, int parentPrecedence)
    {
        return currentPrecedence < parentPrecedence ? $"({text})" : text;
    }

    /// <summary>
    /// Gets operator precedence for the specified expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The precedence value.</returns>
    static int Precedence(Cql2Expression expression)
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

    /// <summary>
    /// Maps a binary operator enum value to canonical CQL2 text.
    /// </summary>
    /// <param name="op">The binary operator.</param>
    /// <returns>The operator text.</returns>
    static string OperatorText(Cql2BinaryOperator op)
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

    /// <summary>
    /// Formats a literal value as canonical CQL2 text.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>The formatted literal text.</returns>
    static string FormatLiteral(object? value)
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
