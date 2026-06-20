namespace OgcCql2;

/// <summary>
/// Immutable token value returned by the lexer.
/// </summary>
/// <param name="Kind">The token kind.</param>
/// <param name="Text">The token text.</param>
/// <param name="Value">The parsed literal value, when applicable.</param>
readonly record struct Cql2TextToken(Cql2TextTokenKind Kind, string Text, object? Value = null);
