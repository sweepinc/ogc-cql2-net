using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OgcCql2;

public static class Cql2JsonParser
{
    public static Cql2Expression Parse(string json)
    {
        using var document = JsonDocument.Parse(json ?? throw new ArgumentNullException(nameof(json)));
        return ParseExpression(document.RootElement);
    }

    private static Cql2Expression ParseExpression(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => new Cql2LiteralExpression(element.GetString()),
            JsonValueKind.True => new Cql2LiteralExpression(true),
            JsonValueKind.False => new Cql2LiteralExpression(false),
            JsonValueKind.Null => new Cql2LiteralExpression(null),
            JsonValueKind.Number => ParseNumber(element),
            JsonValueKind.Object => ParseObject(element),
            JsonValueKind.Array => new Cql2LiteralExpression(ParseLiteralArray(element)),
            _ => throw new FormatException($"Unsupported JSON value kind: {element.ValueKind}")
        };
    }

    private static Cql2Expression ParseObject(JsonElement element)
    {
        if (element.TryGetProperty("property", out var propertyElement) && propertyElement.ValueKind == JsonValueKind.String)
        {
            return new Cql2PropertyExpression(propertyElement.GetString()!);
        }

        if (!element.TryGetProperty("op", out var opElement) || opElement.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("JSON expression object must have an 'op' or 'property' field");
        }

        var op = opElement.GetString()!;
        var args = element.TryGetProperty("args", out var argsElement)
            ? argsElement.EnumerateArray().Select(ParseExpression).ToList()
            : new List<Cql2Expression>();

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

    private static Cql2Expression ParseNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var integer))
        {
            return new Cql2LiteralExpression(integer);
        }

        return new Cql2LiteralExpression(element.GetDouble());
    }

    private static IReadOnlyList<object?> ParseLiteralArray(JsonElement element)
    {
        return element.EnumerateArray().Select(ParseLiteralValue).ToList();
    }

    private static object? ParseLiteralValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.Array => element.EnumerateArray().Select(ParseLiteralValue).ToList(),
            _ => throw new FormatException($"Unsupported literal JSON value kind: {element.ValueKind}")
        };
    }
}
