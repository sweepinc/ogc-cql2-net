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
        ValidateExpression(expression, "$", null);
    }

    /// <summary>
    /// Validates an expression tree and enforces that function calls use known function names.
    /// </summary>
    /// <param name="expression">The expression tree to validate.</param>
    /// <param name="knownFunctions">The function names allowed by the current CQL2 profile.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> or <paramref name="knownFunctions"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when the expression tree contains invalid content or unknown functions.</exception>
    public static void Validate(Cql2Expression expression, IEnumerable<string> knownFunctions)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(knownFunctions);
        ValidateExpression(expression, "$", new HashSet<string>(knownFunctions, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates a single expression node and its children.
    /// </summary>
    /// <param name="expression">The expression node.</param>
    /// <param name="path">The logical path used in error messages.</param>
    /// <param name="knownFunctions">The optional set of function names allowed by the current CQL2 profile.</param>
    static void ValidateExpression(Cql2Expression expression, string path, ISet<string>? knownFunctions)
    {
        switch (expression)
        {
            case Cql2StringExpression str:
                if (str.Value is null)
                    throw new FormatException($"String literal value is required at {path}.");

                return;
            case Cql2NumberExpression:
            case Cql2BooleanExpression:
                return;
            case Cql2PropertyExpression property:
                if (string.IsNullOrWhiteSpace(property.Name))
                    throw new FormatException($"Property name is required at {path}.");

                return;
            case Cql2UnaryExpression unary:
                if (unary.Operand is null)
                    throw new FormatException($"Unary operand is required at {path}.");

                ValidateExpression(unary.Operand, $"{path}.operand", knownFunctions);
                return;
            case Cql2BinaryExpression binary:
                if (binary.Left is null)
                    throw new FormatException($"Binary left operand is required at {path}.");

                if (binary.Right is null)
                    throw new FormatException($"Binary right operand is required at {path}.");

                ValidateExpression(binary.Left, $"{path}.left", knownFunctions);
                ValidateExpression(binary.Right, $"{path}.right", knownFunctions);
                return;
            case Cql2FunctionCallExpression function:
                ValidateFunction(function, path, knownFunctions);
                return;
            case Cql2IsNullExpression isNull:
                if (isNull.Operand is null)
                    throw new FormatException($"IS NULL operand is required at {path}.");

                ValidateExpression(isNull.Operand, $"{path}.operand", knownFunctions);
                return;
            case Cql2ArrayExpression array:
                if (array.Elements.IsDefault)
                    throw new FormatException($"Array elements are required at {path}.");

                for (var i = 0; i < array.Elements.Length; i++)
                {
                    var element = array.Elements[i];
                    if (element is null)
                        throw new FormatException($"Array element is required at {path}[{i}].");

                    ValidateExpression(element, $"{path}[{i}]", knownFunctions);
                }

                return;
            case Cql2DateExpression:
            case Cql2TimestampExpression:
                return;
            case Cql2IntervalExpression interval:
                if (interval.Start is null && interval.End is null)
                    throw new FormatException($"An interval must have at least one bounded end at {path}.");

                if (interval.Start is not null)
                    ValidateExpression(interval.Start, $"{path}.start", knownFunctions);

                if (interval.End is not null)
                    ValidateExpression(interval.End, $"{path}.end", knownFunctions);

                return;
            case Cql2GeometryExpression geometry:
                if (geometry.Geometry is null)
                    throw new FormatException($"Geometry value is required at {path}.");

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
    /// <param name="knownFunctions">The optional set of function names allowed by the current CQL2 profile.</param>
    static void ValidateFunction(Cql2FunctionCallExpression function, string path, ISet<string>? knownFunctions)
    {
        if (string.IsNullOrWhiteSpace(function.Name))
            throw new FormatException($"Function name is required at {path}.");

        if (knownFunctions is not null && !knownFunctions.Contains(function.Name))
            throw new FormatException($"Unknown function '{function.Name}' at {path}.");

        if (function.Arguments.IsDefault)
            throw new FormatException($"Function arguments are required at {path}.");

        for (var i = 0; i < function.Arguments.Length; i++)
        {
            var argument = function.Arguments[i];
            if (argument is null)
                throw new FormatException($"Function argument is required at {path}.args[{i}].");

            ValidateExpression(argument, $"{path}.args[{i}]", knownFunctions);
        }
    }

}
