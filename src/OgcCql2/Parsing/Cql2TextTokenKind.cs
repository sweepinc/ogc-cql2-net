namespace OgcCql2.Parsing;

/// <summary>
/// Token kinds used by the CQL2 lexer.
/// </summary>
enum Cql2TextTokenKind
{
    End,
    Identifier,
    String,
    Number,
    True,
    False,
    Null,
    LeftParen,
    RightParen,
    Comma,
    Equal,
    NotEqual,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,
    And,
    Or,
    Not
}
