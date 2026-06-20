using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OgcCql2;

public static class Cql2JsonParser
{
    public static Cql2Expression Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        if (!reader.Read())
        {
            throw new FormatException("JSON input is empty.");
        }

        var expression = ParseExpression(ref reader);
        if (reader.Read())
        {
            throw new FormatException("Unexpected trailing JSON content.");
        }

        return expression;
    }

    private static Cql2Expression ParseExpression(ref Utf8JsonReader reader)
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

    private static Cql2Expression ParseObject(ref Utf8JsonReader reader)
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
            {
                throw new FormatException($"Expected property name but found {reader.TokenType}");
            }

            var propertyName = reader.GetString();
            if (!reader.Read())
            {
                throw new FormatException("Unexpected end of JSON while reading object value.");
            }

            switch (propertyName)
            {
                case "property":
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new FormatException("'property' field must be a string.");
                    }

                    property = reader.GetString();
                    break;
                case "op":
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new FormatException("'op' field must be a string.");
                    }

                    op = reader.GetString();
                    break;
                case "args":
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        throw new FormatException("'args' field must be an array.");
                    }

                    args = ParseExpressionArray(ref reader);
                    break;
                default:
                    SkipValue(ref reader);
                    break;
            }
        }

        if (!endedObject)
        {
            throw new FormatException("Unexpected end of JSON while reading expression object.");
        }

        if (property is not null)
        {
            return new Cql2PropertyExpression(property);
        }

        if (op is null)
        {
            throw new FormatException("JSON expression object must have an 'op' or 'property' field");
        }

        args ??= new List<Cql2Expression>();

        return op.ToLowerInvariant() switch
        {
            "and" => ParseNaryBinary(Cql2BinaryOperator.And, args),
            "or" => ParseNaryBinary(Cql2BinaryOperator.Or, args),
            "not" when args.Count == 1 => new Cql2UnaryExpression(Cql2UnaryOperator.Not, args[0]),
            "=" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.Equal, args[0], args[1]),
            "<>" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.NotEqual, args[0], args[1]),
            "<" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.LessThan, args[0], args[1]),
            "<=" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.LessThanOrEqual, args[0], args[1]),
            ">" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThan, args[0], args[1]),
            ">=" when args.Count == 2 => new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThanOrEqual, args[0], args[1]),
            _ => new Cql2FunctionCallExpression(op, args)
        };
    }

    private static Cql2Expression ParseNaryBinary(Cql2BinaryOperator op, IReadOnlyList<Cql2Expression> args)
    {
        if (args.Count < 2)
        {
            throw new FormatException($"Operator requires at least two arguments: {op}");
        }

        var expression = new Cql2BinaryExpression(op, args[0], args[1]);
        for (var i = 2; i < args.Count; i++)
        {
            expression = new Cql2BinaryExpression(op, expression, args[i]);
        }

        return expression;
    }

    private static Cql2Expression ParseNumber(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt64(out var integer))
        {
            return new Cql2LiteralExpression(integer);
        }

        return new Cql2LiteralExpression(reader.GetDouble());
    }

    private static List<Cql2Expression> ParseExpressionArray(ref Utf8JsonReader reader)
    {
        var values = new List<Cql2Expression>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values;
            }

            values.Add(ParseExpression(ref reader));
        }

        throw new FormatException("Unexpected end of JSON while reading args array.");
    }

    private static IReadOnlyList<object?> ParseLiteralArray(ref Utf8JsonReader reader)
    {
        var values = new List<object?>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values;
            }

            values.Add(ParseLiteralValue(ref reader));
        }

        throw new FormatException("Unexpected end of JSON while reading literal array.");
    }

    private static object? ParseLiteralValue(ref Utf8JsonReader reader)
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

    private static void SkipValue(ref Utf8JsonReader reader)
    {
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
        {
            reader.Skip();
        }
    }
}
