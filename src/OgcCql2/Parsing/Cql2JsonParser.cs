using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using OgcCql2.Expressions;

namespace OgcCql2.Parsing;

/// <summary>
/// Parses CQL2 JSON payloads into expression nodes.
/// </summary>
public static class Cql2JsonParser
{
    /// <summary>
    /// Parses a CQL2 JSON expression from a UTF-16 JSON string.
    /// </summary>
    /// <param name="json">The JSON expression text.</param>
    /// <returns>The parsed expression tree.</returns>
    /// <remarks>
    /// This overload encodes the input string as UTF-8. For performance-sensitive paths,
    /// prefer <see cref="Parse(ReadOnlySpan{byte})"/> to avoid an extra string-to-byte copy.
    /// </remarks>
    public static Cql2Expression Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var rented = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(json.Length));
        try
        {
            var byteCount = Encoding.UTF8.GetBytes(json, rented);
            return Parse(rented.AsSpan(0, byteCount));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Parses a CQL2 JSON expression from UTF-8 JSON bytes using a forward-only reader.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 JSON payload.</param>
    /// <returns>The parsed expression tree.</returns>
    public static Cql2Expression Parse(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            var reader = new Utf8JsonReader(utf8Json);
            if (!reader.Read())
                throw new FormatException("JSON input is empty.");

            var expression = ParseExpression(ref reader);
            if (reader.Read())
                throw new FormatException("Unexpected trailing JSON content.");

            return expression;
        }
        catch (JsonException ex)
        {
            throw new FormatException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Parses the current token as a CQL2 expression.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <returns>The parsed expression.</returns>
    static Cql2Expression ParseExpression(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new Cql2LiteralExpression(reader.GetString()),
            JsonTokenType.True => new Cql2LiteralExpression(true),
            JsonTokenType.False => new Cql2LiteralExpression(false),
            JsonTokenType.Null => new Cql2LiteralExpression(null),
            JsonTokenType.Number => ParseNumber(ref reader),
            JsonTokenType.StartObject => ParseObject(ref reader),
            JsonTokenType.StartArray => new Cql2LiteralExpression(ParseLiteralArray(ref reader)),
            _ => throw new FormatException($"Unsupported JSON token: {reader.TokenType}")
        };
    }

    /// <summary>
    /// Parses an expression object with <c>property</c> or <c>op</c>/<c>args</c> members.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <returns>The parsed expression.</returns>
    static Cql2Expression ParseObject(ref Utf8JsonReader reader)
    {
        string? property = null;
        string? op = null;
        List<Cql2Expression>? args = null;
        var endedObject = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                endedObject = true;
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new FormatException($"Expected property name but found {reader.TokenType}");

            var propertyName = reader.GetString();
            if (!reader.Read())
                throw new FormatException("Unexpected end of JSON while reading object value.");

            switch (propertyName)
            {
                case "property":
                    if (reader.TokenType != JsonTokenType.String)
                        throw new FormatException("'property' field must be a string.");

                    property = reader.GetString();
                    break;
                case "op":
                    if (reader.TokenType != JsonTokenType.String)
                        throw new FormatException("'op' field must be a string.");

                    op = reader.GetString();
                    break;
                case "args":
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new FormatException("'args' field must be an array.");

                    args = ParseExpressionArray(ref reader);
                    break;
                default:
                    if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                        reader.Skip();

                    break;
            }
        }

        if (!endedObject)
            throw new FormatException("Unexpected end of JSON while reading expression object.");

        if (property is not null)
            return new Cql2PropertyExpression(property);

        if (op is null)
            throw new FormatException("JSON expression object must have an 'op' or 'property' field");

        args ??= new List<Cql2Expression>();

        return op.ToLowerInvariant() switch
        {
            "and" => ParseNaryBinary(Cql2BinaryOperator.And, args),
            "or" => ParseNaryBinary(Cql2BinaryOperator.Or, args),
            "not" when args.Count == 1 => new Cql2UnaryExpression(Cql2UnaryOperator.Not, args[0]),
            "not" => throw new FormatException($"Operator 'not' requires exactly 1 argument but received {args.Count}."),
            "=" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.Equal, args[0], args[1]),
            "=" => throw new FormatException($"Operator '=' requires exactly 2 arguments but received {args.Count}."),
            "<>" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.NotEqual, args[0], args[1]),
            "<>" => throw new FormatException($"Operator '<>' requires exactly 2 arguments but received {args.Count}."),
            "<" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.LessThan, args[0], args[1]),
            "<" => throw new FormatException($"Operator '<' requires exactly 2 arguments but received {args.Count}."),
            "<=" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.LessThanOrEqual, args[0], args[1]),
            "<=" => throw new FormatException($"Operator '<=' requires exactly 2 arguments but received {args.Count}."),
            ">" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThan, args[0], args[1]),
            ">" => throw new FormatException($"Operator '>' requires exactly 2 arguments but received {args.Count}."),
            ">=" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThanOrEqual, args[0], args[1]),
            ">=" => throw new FormatException($"Operator '>=' requires exactly 2 arguments but received {args.Count}."),
            _ => new Cql2FunctionCallExpression(op, args)
        };
    }

    /// <summary>
    /// Rebuilds left-associated trees from n-ary logical operators.
    /// </summary>
    /// <param name="op">The target binary operator.</param>
    /// <param name="args">The operator arguments.</param>
    /// <returns>The rebuilt binary expression tree.</returns>
    static Cql2Expression ParseNaryBinary(Cql2BinaryOperator op, IReadOnlyList<Cql2Expression> args)
    {
        if (args.Count < 2)
            throw new FormatException($"Operator requires at least two arguments: {op}");

        var expression = new Cql2BinaryExpression(op, args[0], args[1]);
        for (var i = 2; i < args.Count; i++)
            expression = new Cql2BinaryExpression(op, expression, args[i]);

        return expression;
    }

    /// <summary>
    /// Parses the current numeric token as an integer or floating-point literal.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <returns>The parsed numeric literal expression.</returns>
    static Cql2Expression ParseNumber(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt64(out var integer))
            return new Cql2LiteralExpression(integer);

        return new Cql2LiteralExpression(reader.GetDouble());
    }

    /// <summary>
    /// Parses an expression argument array.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <returns>The parsed argument expressions.</returns>
    static List<Cql2Expression> ParseExpressionArray(ref Utf8JsonReader reader)
    {
        var values = new List<Cql2Expression>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return values;

            values.Add(ParseExpression(ref reader));
        }

        throw new FormatException("Unexpected end of JSON while reading args array.");
    }

    /// <summary>
    /// Parses a JSON literal array value.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <returns>The parsed literal list.</returns>
    static IReadOnlyList<object?> ParseLiteralArray(ref Utf8JsonReader reader)
    {
        var values = new List<object?>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return values;

            values.Add(ParseLiteralValue(ref reader));
        }

        throw new FormatException("Unexpected end of JSON while reading literal array.");
    }

    /// <summary>
    /// Parses a JSON scalar or nested literal array value.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <returns>The parsed literal value.</returns>
    static object? ParseLiteralValue(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt64(out var integer) => integer,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.StartArray => ParseLiteralArray(ref reader),
            _ => throw new FormatException($"Unsupported literal JSON token: {reader.TokenType}")
        };
    }
}
