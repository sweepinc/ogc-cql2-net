using System;
using System.Globalization;

namespace OgcCql2.Parsing;

/// <summary>
/// Lexical tokenizer for CQL2 text input.
/// </summary>
sealed class Cql2TextLexer
{
    readonly string _text;
    int _index;

    /// <summary>
    /// Initializes a lexer instance.
    /// </summary>
    /// <param name="text">The CQL2 text input.</param>
    public Cql2TextLexer(string text)
    {
        _text = text;
    }

    /// <summary>
    /// Reads the next token from the input stream.
    /// </summary>
    /// <returns>The next token.</returns>
    public Cql2TextToken Next()
    {
        SkipWhitespace();
        if (_index >= _text.Length)
            return new Cql2TextToken(Cql2TextTokenKind.End, string.Empty);

        var ch = _text[_index];
        if (char.IsLetter(ch) || ch == '_')
        {
            var start = _index++;
            while (_index < _text.Length && (char.IsLetterOrDigit(_text[_index]) || _text[_index] == '_' || _text[_index] == '.'))
                _index++;

            var text = _text[start.._index];
            return text.ToUpperInvariant() switch
            {
                "AND" => new Cql2TextToken(Cql2TextTokenKind.And, text),
                "OR" => new Cql2TextToken(Cql2TextTokenKind.Or, text),
                "NOT" => new Cql2TextToken(Cql2TextTokenKind.Not, text),
                "TRUE" => new Cql2TextToken(Cql2TextTokenKind.True, text, true),
                "FALSE" => new Cql2TextToken(Cql2TextTokenKind.False, text, false),
                "NULL" => new Cql2TextToken(Cql2TextTokenKind.Null, text, null),
                _ => new Cql2TextToken(Cql2TextTokenKind.Identifier, text)
            };
        }

        if (char.IsDigit(ch) || ch == '-')
        {
            var start = _index++;
            while (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] == '.'))
                _index++;

            var text = _text[start.._index];
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                return new Cql2TextToken(Cql2TextTokenKind.Number, text, integer);

            return new Cql2TextToken(Cql2TextTokenKind.Number, text, double.Parse(text, CultureInfo.InvariantCulture));
        }

        if (ch == '\'')
        {
            _index++;
            var value = string.Empty;
            while (_index < _text.Length)
            {
                var current = _text[_index++];
                if (current == '\'')
                {
                    if (_index < _text.Length && _text[_index] == '\'')
                    {
                        _index++;
                        value += '\'';
                        continue;
                    }

                    return new Cql2TextToken(Cql2TextTokenKind.String, value, value);
                }

                value += current;
            }

            throw new FormatException("Unterminated string literal");
        }

        _index++;
        return ch switch
        {
            '(' => new Cql2TextToken(Cql2TextTokenKind.LeftParen, "("),
            ')' => new Cql2TextToken(Cql2TextTokenKind.RightParen, ")"),
            ',' => new Cql2TextToken(Cql2TextTokenKind.Comma, ","),
            '=' => new Cql2TextToken(Cql2TextTokenKind.Equal, "="),
            '<' when Match('=') => new Cql2TextToken(Cql2TextTokenKind.LessOrEqual, "<="),
            '<' when Match('>') => new Cql2TextToken(Cql2TextTokenKind.NotEqual, "<>"),
            '<' => new Cql2TextToken(Cql2TextTokenKind.Less, "<"),
            '>' when Match('=') => new Cql2TextToken(Cql2TextTokenKind.GreaterOrEqual, ">="),
            '>' => new Cql2TextToken(Cql2TextTokenKind.Greater, ">"),
            _ => throw new FormatException($"Unexpected character '{ch}'")
        };
    }

    /// <summary>
    /// Matches the next character and advances the cursor when it matches.
    /// </summary>
    /// <param name="expected">The expected character.</param>
    /// <returns><see langword="true"/> when matched; otherwise <see langword="false"/>.</returns>
    bool Match(char expected)
    {
        if (_index < _text.Length && _text[_index] == expected)
        {
            _index++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Advances beyond any whitespace characters.
    /// </summary>
    void SkipWhitespace()
    {
        while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            _index++;
    }
}
