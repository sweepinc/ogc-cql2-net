using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

using OgcCql2.Expressions;
using OgcCql2.Geometries;

namespace OgcCql2.Formatting;

/// <summary>
/// Formats expression nodes into canonical CQL2 JSON.
/// </summary>
public static class Cql2JsonFormatter
{
    static readonly JsonWriterOptions s_writerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Formats an expression as canonical CQL2 JSON.
    /// </summary>
    /// <param name="expression">The expression to format.</param>
    /// <returns>The canonical JSON representation.</returns>
    public static string Format(Cql2Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var node = ToNode(expression);
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, s_writerOptions);
        node.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
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
            Cql2StringExpression str => JsonValue.Create(str.Value),
            Cql2NumberExpression number => JsonValue.Create(number.Value),
            Cql2BooleanExpression boolean => JsonValue.Create(boolean.Value),
            Cql2PropertyExpression property => new JsonObject { ["property"] = property.Name },
            Cql2ArrayExpression array => new JsonArray(array.Elements.Select(ToNode).ToArray()),
            Cql2DateExpression date => new JsonObject { ["date"] = Cql2TemporalText.FormatDate(date.Value) },
            Cql2TimestampExpression timestamp => new JsonObject { ["timestamp"] = Cql2TemporalText.FormatTimestamp(timestamp.Value) },
            Cql2IntervalExpression interval => new JsonObject
            {
                ["interval"] = new JsonArray(IntervalBoundNode(interval.Start), IntervalBoundNode(interval.End))
            },
            Cql2GeometryExpression geometry => GeometryIo.WriteGeoJson(geometry.Geometry),
            Cql2IsNullExpression isNull => new JsonObject
            {
                ["op"] = "isNull",
                ["args"] = new JsonArray(ToNode(isNull.Operand))
            },
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
    /// Converts an interval bound to its JSON node: <c>".."</c> for an open bound, a lexical date or
    /// timestamp string for an instant, or the bound's expression node for property/function bounds.
    /// </summary>
    /// <param name="bound">The bound expression, or <see langword="null"/> when open.</param>
    /// <returns>The JSON node representation.</returns>
    static JsonNode IntervalBoundNode(Cql2Expression? bound)
    {
        if (bound is null)
            return JsonValue.Create(Cql2Syntax.OpenBound);

        var instant = Cql2TemporalText.TryFormatInstant(bound);
        return instant is not null ? JsonValue.Create(instant) : ToNode(bound);
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
