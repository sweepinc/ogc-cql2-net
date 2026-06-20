using System;
using OgcCql2.Expressions;
using Xunit;

namespace OgcCql2.Tests;

/// <summary>
/// Tests for expression tree validation.
/// </summary>
public class Cql2ExpressionValidatorTests
{
    /// <summary>
    /// Verifies that a normal parsed expression validates successfully.
    /// </summary>
    [Fact]
    public void Validate_ValidExpression_DoesNotThrow()
    {
        var expression = Cql2TextParser.Parse("foo = 1 AND NOT bar >= 10");

        Cql2ExpressionValidator.Validate(expression);
    }

    /// <summary>
    /// Verifies that a property expression must include a name.
    /// </summary>
    [Fact]
    public void Validate_BlankPropertyName_ThrowsFormatException()
    {
        var expression = new Cql2PropertyExpression(string.Empty);

        Assert.Throws<FormatException>(() => Cql2ExpressionValidator.Validate(expression));
    }

    /// <summary>
    /// Verifies that function call arguments cannot contain null expression nodes.
    /// </summary>
    [Fact]
    public void Validate_FunctionWithNullArgument_ThrowsFormatException()
    {
        var expression = new Cql2FunctionCallExpression("contains", new Cql2Expression[] { null! });

        Assert.Throws<FormatException>(() => Cql2ExpressionValidator.Validate(expression));
    }

    /// <summary>
    /// Verifies that CQL2 numeric literals validate regardless of CLR numeric primitive choice.
    /// </summary>
    [Fact]
    public void Validate_NumericLiteral_DoesNotThrow()
    {
        var expression = new Cql2LiteralExpression(1m);

        Cql2ExpressionValidator.Validate(expression);
    }

    /// <summary>
    /// Verifies that unsupported literal value types are rejected.
    /// </summary>
    [Fact]
    public void Validate_UnsupportedLiteralType_ThrowsFormatException()
    {
        var expression = new Cql2LiteralExpression(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Throws<FormatException>(() => Cql2ExpressionValidator.Validate(expression));
    }
}
