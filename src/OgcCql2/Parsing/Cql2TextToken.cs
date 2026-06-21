namespace OgcCql2.Parsing;

/// <summary>
/// A lexer token referencing a slice of the source span. Purely lexical: the token carries its
/// kind and the matched characters; value conversion happens in the parser.
/// </summary>
readonly ref struct Cql2TextToken
{

    /// <summary>
    /// Initializes a token.
    /// </summary>
    /// <param name="kind">The token kind.</param>
    /// <param name="text">The slice of source text the token spans.</param>
    public Cql2TextToken(Cql2TextTokenKind kind, ReadOnlySpan<char> text)
    {
        Kind = kind;
        Text = text;
    }

    /// <summary>The token kind.</summary>
    public Cql2TextTokenKind Kind { get; }

    /// <summary>The matched characters, sliced from the source span.</summary>
    public ReadOnlySpan<char> Text { get; }

}
