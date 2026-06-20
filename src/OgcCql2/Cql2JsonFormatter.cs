using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace OgcCql2;

/// <summary>
/// Formats expression nodes into canonical CQL2 JSON.
/// </summary>
public static class Cql2JsonFormatter
{
    /// <summary>
    /// Formats an expression as canonical CQL2 JSON.
    /// </summary>
    /// <param name="expression">The expression to format.</param>
    /// <returns>The canonical JSON representation.</returns>
    public static string Format(Cql2Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return ToNode(expression).ToJsonString();
    }

    /// <summary>
    /// Converts an expression node to a JSON node.
    /// </summary>
    /// <param name="expression">The expression to convert.</param>
    /// <returns>The JSON node representation.</returns>
    static JsonNode ToNode(Cql2Expression expression)
    {
        return expression switch
        {
            Cql2LiteralExpression literal => ToLiteralNode(literal.Value),
            Cql2PropertyExpression property => new JsonObject { ["property"] = property.Name },
            Cql2UnaryExpression unary => new JsonObject
            {
                ["op"] = "not",
                ["args"] = new JsonArray(ToNode(unary.Operand))
            },
            Cql2BinaryExpression binary => ToBinaryNode(binary),
            Cql2FunctionCallExpression function => new JsonObject
            {
                ["op"] = function.Name,
                ["args"] = new JsonArray(function.Arguments.Select(ToNode).ToArray())
            },
            _ => throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}")
        };
    }

    /// <summary>
    /// Converts a binary expression to a JSON node.
    /// </summary>
    /// <param name="expression">The binary expression to convert.</param>
    /// <returns>The JSON node representation.</returns>
    static JsonNode ToBinaryNode(Cql2BinaryExpression expression)
    {
        if (expression.Operator is Cql2BinaryOperator.And or Cql2BinaryOperator.Or)
        {
            var opText = expression.Operator == Cql2BinaryOperator.And ? "and" : "or";
            var args = new List<JsonNode>();
            FlattenBinary(expression, expression.Operator, args);
            return new JsonObject
            {
                ["op"] = opText,
                ["args"] = new JsonArray(args.ToArray())
            };
        }

        return new JsonObject
        {
            ["op"] = OperatorText(expression.Operator),
            ["args"] = new JsonArray(ToNode(expression.Left), ToNode(expression.Right))
        };
    }

    /// <summary>
    /// Flattens an associative binary tree into argument nodes.
    /// </summary>
    /// <param name="expression">The expression to flatten.</param>
    /// <param name="targetOperator">The operator to flatten.</param>
    /// <param name="args">The collected arguments.</param>
    static void FlattenBinary(Cql2Expression expression, Cql2BinaryOperator targetOperator, List<JsonNode> args)
    {
        if (expression is Cql2BinaryExpression binary && binary.Operator == targetOperator)
        {
            FlattenBinary(binary.Left, targetOperator, args);
            FlattenBinary(binary.Right, targetOperator, args);
            return;
        }

        args.Add(ToNode(expression));
    }

    /// <summary>
    /// Converts a literal value to a JSON node.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <returns>The JSON node representation.</returns>
    static JsonNode ToLiteralNode(object? value)
    {
        return value switch
        {
            null => JsonValue.Create((string?)null)!,
            bool boolean => JsonValue.Create(boolean)!,
            string text => JsonValue.Create(text)!,
            sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal => JsonValue.Create(value)!,
            IReadOnlyList<object?> list => new JsonArray(list.Select(ToLiteralNode).ToArray()),
            _ => throw new NotSupportedException($"Unsupported literal type: {value.GetType().Name}")
        };
    }

    /// <summary>
    /// Maps a binary operator to canonical JSON operator text.
    /// </summary>
    /// <param name="op">The binary operator.</param>
    /// <returns>The JSON operator text.</returns>
    static string OperatorText(Cql2BinaryOperator op)
    {
        return op switch
        {
            Cql2BinaryOperator.Equal => "=",
            Cql2BinaryOperator.NotEqual => "<>",
            Cql2BinaryOperator.LessThan => "<",
            Cql2BinaryOperator.LessThanOrEqual => "<=",
            Cql2BinaryOperator.GreaterThan => ">",
            Cql2BinaryOperator.GreaterThanOrEqual => ">=",
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }
}
