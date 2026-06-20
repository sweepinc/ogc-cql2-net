using System;
using OgcCql2.Expressions;

namespace OgcCql2.Parsing;

/// <summary>
/// Parses CQL2 text expressions into expression nodes.
/// </summary>
public static class Cql2TextParser
{
    /// <summary>
    /// Parses a CQL2 text expression.
    /// </summary>
    /// <param name="text">The CQL2 text input.</param>
    /// <returns>The parsed expression tree.</returns>
    public static Cql2Expression Parse(string text)
    {
        var parser = new Cql2TextParserCore(text ?? throw new ArgumentNullException(nameof(text)));
        return parser.Parse();
    }
}
