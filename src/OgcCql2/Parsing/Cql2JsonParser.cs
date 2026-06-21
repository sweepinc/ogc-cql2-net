using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

using OgcCql2.Expressions;
using OgcCql2.Geometries;

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
            JsonTokenType.String => new Cql2StringExpression(reader.GetString()!),
            JsonTokenType.True => new Cql2BooleanExpression(true),
            JsonTokenType.False => new Cql2BooleanExpression(false),
            JsonTokenType.Null => throw new FormatException("CQL2 has no null literal; use the 'isNull' predicate."),
            JsonTokenType.Number => ParseNumber(ref reader),
            JsonTokenType.StartObject => ParseObjectOrGeometry(ref reader),
            JsonTokenType.StartArray => new Cql2ArrayExpression(ParseExpressionArray(ref reader).ToImmutableArray()),
            _ => throw new FormatException($"Unsupported JSON token: {reader.TokenType}")
        };
    }

    /// <summary>
    /// Dispatches a JSON object to GeoJSON geometry parsing or CQL2 expression-object parsing.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader positioned at the opening object.</param>
    /// <returns>The parsed expression.</returns>
    static Cql2Expression ParseObjectOrGeometry(ref Utf8JsonReader reader)
    {
        if (GeometryIo.LooksLikeGeometry(reader))
            return new Cql2GeometryExpression(GeometryIo.ReadGeoJson(ref reader));

        return ParseObject(ref reader);
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
        string? date = null;
        string? timestamp = null;
        List<Cql2Expression>? args = null;
        List<Cql2Expression?>? interval = null;
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
                    property = ExpectString(ref reader, "property");
                    break;
                case "op":
                    op = ExpectString(ref reader, "op");
                    break;
                case "args":
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new FormatException("'args' field must be an array.");

                    args = ParseExpressionArray(ref reader);
                    break;
                case "date":
                    date = ExpectString(ref reader, "date");
                    break;
                case "timestamp":
                    timestamp = ExpectString(ref reader, "timestamp");
                    break;
                case "interval":
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new FormatException("'interval' field must be an array.");

                    interval = ParseIntervalArray(ref reader);
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

        if (date is not null)
            return new Cql2DateExpression(Cql2TemporalText.ParseDate(date));

        if (timestamp is not null)
            return new Cql2TimestampExpression(Cql2TemporalText.ParseTimestamp(timestamp));

        if (interval is not null)
        {
            if (interval.Count != 2)
                throw new FormatException($"'interval' requires exactly 2 bounds but received {interval.Count}.");

            return new Cql2IntervalExpression(interval[0], interval[1]);
        }

        if (op is null)
            throw new FormatException("JSON expression object must have an 'op', 'property', or literal field.");

        args ??= new List<Cql2Expression>();

        return op.ToLowerInvariant() switch
        {
            "and" => ParseNaryBinary(Cql2BinaryOperator.And, args),
            "or" => ParseNaryBinary(Cql2BinaryOperator.Or, args),
            "not" when args.Count == 1 => new Cql2UnaryExpression(Cql2UnaryOperator.Not, args[0]),
            "not" => throw new FormatException($"Operator 'not' requires exactly 1 argument but received {args.Count}."),
            "isnull" when args.Count == 1 => new Cql2IsNullExpression(args[0]),
            "isnull" => throw new FormatException($"Operator 'isNull' requires exactly 1 argument but received {args.Count}."),
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
            _ => new Cql2FunctionCallExpression(op, args.ToImmutableArray())
        };
    }

    /// <summary>
    /// Reads the current token as a string, throwing when it is not a JSON string.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <param name="field">The field name used in error messages.</param>
    /// <returns>The string value.</returns>
    static string ExpectString(ref Utf8JsonReader reader, string field)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new FormatException($"'{field}' field must be a string.");

        return reader.GetString()!;
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
        if (!reader.TryGetDecimal(out var number))
            throw new FormatException("Numeric literal is outside the supported range.");

        return new Cql2NumberExpression(number);
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
    /// Parses an interval bounds array. Each item is a date/timestamp string (typed into a
    /// temporal node), the open-bound <c>".."</c> string (null), or a property/function object.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <returns>The parsed bounds; <see langword="null"/> entries denote open bounds.</returns>
    static List<Cql2Expression?> ParseIntervalArray(ref Utf8JsonReader reader)
    {
        var bounds = new List<Cql2Expression?>();
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndArray:
                    return bounds;
                case JsonTokenType.String:
                    var value = reader.GetString()!;
                    bounds.Add(value == Cql2Syntax.OpenBound ? null : Cql2TemporalText.ParseInstant(value));
                    break;
                case JsonTokenType.StartObject:
                    bounds.Add(ParseObjectOrGeometry(ref reader));
                    break;
                default:
                    throw new FormatException($"Unsupported interval bound token '{reader.TokenType}'.");
            }
        }

        throw new FormatException("Unexpected end of JSON while reading 'interval' array.");
    }

}
