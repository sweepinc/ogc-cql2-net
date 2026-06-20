using System;
using System.Collections.Generic;
using OgcCql2.Expressions;

namespace OgcCql2;

/// <summary>
/// Validates CQL2 expression trees for structural correctness.
/// </summary>
public static class Cql2ExpressionValidator
{
    /// <summary>
    /// Validates an expression tree and throws when it contains unsupported or malformed nodes/values.
    /// </summary>
    /// <param name="expression">The expression tree to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when the expression tree contains invalid content.</exception>
    public static void Validate(Cql2Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ValidateExpression(expression, "$");
    }

    /// <summary>
    /// Validates a single expression node and its children.
    /// </summary>
    /// <param name="expression">The expression node.</param>
    /// <param name="path">The logical path used in error messages.</param>
    static void ValidateExpression(Cql2Expression expression, string path)
    {
        switch (expression)
        {
            case Cql2LiteralExpression literal:
                ValidateLiteralValue(literal.Value, $"{path}.literal");
                return;
            case Cql2PropertyExpression property:
                if (string.IsNullOrWhiteSpace(property.Name))
                    throw new FormatException($"Property name is required at {path}.");

                return;
            case Cql2UnaryExpression unary:
                if (unary.Operand is null)
                    throw new FormatException($"Unary operand is required at {path}.");

                ValidateExpression(unary.Operand, $"{path}.operand");
                return;
            case Cql2BinaryExpression binary:
                if (binary.Left is null)
                    throw new FormatException($"Binary left operand is required at {path}.");

                if (binary.Right is null)
                    throw new FormatException($"Binary right operand is required at {path}.");

                ValidateExpression(binary.Left, $"{path}.left");
                ValidateExpression(binary.Right, $"{path}.right");
                return;
            case Cql2FunctionCallExpression function:
                ValidateFunction(function, path);
                return;
            default:
                throw new FormatException($"Unsupported expression node type '{expression.GetType().Name}' at {path}.");
        }
    }

    /// <summary>
    /// Validates a function call expression and all arguments.
    /// </summary>
    /// <param name="function">The function expression to validate.</param>
    /// <param name="path">The logical path used in error messages.</param>
    static void ValidateFunction(Cql2FunctionCallExpression function, string path)
    {
        if (string.IsNullOrWhiteSpace(function.Name))
            throw new FormatException($"Function name is required at {path}.");

        if (function.Arguments is null)
            throw new FormatException($"Function arguments are required at {path}.");

        for (var i = 0; i < function.Arguments.Count; i++)
        {
            var argument = function.Arguments[i];
            if (argument is null)
                throw new FormatException($"Function argument is required at {path}.args[{i}].");

            ValidateExpression(argument, $"{path}.args[{i}]");
        }
    }

    /// <summary>
    /// Validates a literal value recursively.
    /// </summary>
    /// <param name="value">The literal value.</param>
    /// <param name="path">The logical path used in error messages.</param>
    static void ValidateLiteralValue(object? value, string path)
    {
        if (value is null)
            return;

        if (value is bool or string or sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal)
            return;

        if (value is IReadOnlyList<object?> list)
        {
            for (var i = 0; i < list.Count; i++)
                ValidateLiteralValue(list[i], $"{path}[{i}]");

            return;
        }

        throw new FormatException($"Unsupported literal value type '{value.GetType().Name}' at {path}.");
    }
}
