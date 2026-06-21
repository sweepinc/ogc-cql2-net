namespace OgcCql2.Parsing;

/// <summary>
/// Lexical tokenizer for CQL2 text input. Operates over a <see cref="ReadOnlySpan{Char}"/> and
/// emits tokens that slice directly out of the source without allocating substrings.
/// </summary>
ref struct Cql2TextLexer
{
    readonly ReadOnlySpan<char> _text;
    int _index;

    /// <summary>
    /// Initializes a lexer instance.
    /// </summary>
    /// <param name="text">The CQL2 text input.</param>
    public Cql2TextLexer(ReadOnlySpan<char> text)
    {
        _text = text;
        _index = 0;
    }

    /// <summary>
    /// Reads the next token from the input stream.
    /// </summary>
    /// <returns>The next token.</returns>
    public Cql2TextToken Next()
    {
        SkipWhitespace();
        if (_index >= _text.Length)
            return new Cql2TextToken(Cql2TextTokenKind.End, ReadOnlySpan<char>.Empty);

        var ch = _text[_index];
        if (char.IsLetter(ch) || ch == Cql2Syntax.Underscore)
        {
            // scan to the end of the word
            var start = _index++;
            while (_index < _text.Length && (char.IsLetterOrDigit(_text[_index]) || _text[_index] == Cql2Syntax.Underscore || _text[_index] == Cql2Syntax.Dot))
                _index++;

            var word = _text[start.._index];

            if (IsGeometryKeyword(word))
            {
                // remember where to rewind if this isn't really a WKT literal
                var save = _index;
                SkipWhitespace();
                ConsumeDimensionTag();
                SkipWhitespace();
                if (_index < _text.Length && _text[_index] == Cql2Syntax.LeftParen)
                {
                    // keyword + balanced parens become one opaque geometry token
                    ConsumeBalancedParens();
                    return new Cql2TextToken(Cql2TextTokenKind.Geometry, _text[start.._index]);
                }

                // just a bare word, not a geometry literal
                _index = save;
            }

            return KeywordOrIdentifier(word);
        }

        if (char.IsDigit(ch) || ch == Cql2Syntax.Minus)
        {
            // integer and fraction digits
            var start = _index++;
            while (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] == Cql2Syntax.Dot))
                _index++;

            // optional scientific exponent
            if (_index < _text.Length && (_text[_index] == Cql2Syntax.ExponentLower || _text[_index] == Cql2Syntax.ExponentUpper))
            {
                _index++;
                // optional exponent sign
                if (_index < _text.Length && (_text[_index] == Cql2Syntax.Plus || _text[_index] == Cql2Syntax.Minus))
                    _index++;

                while (_index < _text.Length && char.IsDigit(_text[_index]))
                    _index++;
            }

            return new Cql2TextToken(Cql2TextTokenKind.Number, _text[start.._index]);
        }

        if (ch == Cql2Syntax.SingleQuote)
            return ReadString();

        // consume the operator char; the slices below rewind by 1 or 2 to include it
        _index++;
        return ch switch
        {
            Cql2Syntax.LeftParen => new Cql2TextToken(Cql2TextTokenKind.LeftParen, _text.Slice(_index - 1, 1)),
            Cql2Syntax.RightParen => new Cql2TextToken(Cql2TextTokenKind.RightParen, _text.Slice(_index - 1, 1)),
            Cql2Syntax.Comma => new Cql2TextToken(Cql2TextTokenKind.Comma, _text.Slice(_index - 1, 1)),
            Cql2Syntax.EqualOp => new Cql2TextToken(Cql2TextTokenKind.Equal, _text.Slice(_index - 1, 1)),
            Cql2Syntax.LessOp when Match(Cql2Syntax.EqualOp) => new Cql2TextToken(Cql2TextTokenKind.LessOrEqual, _text.Slice(_index - 2, 2)),
            Cql2Syntax.LessOp when Match(Cql2Syntax.GreaterOp) => new Cql2TextToken(Cql2TextTokenKind.NotEqual, _text.Slice(_index - 2, 2)),
            Cql2Syntax.LessOp => new Cql2TextToken(Cql2TextTokenKind.Less, _text.Slice(_index - 1, 1)),
            Cql2Syntax.GreaterOp when Match(Cql2Syntax.EqualOp) => new Cql2TextToken(Cql2TextTokenKind.GreaterOrEqual, _text.Slice(_index - 2, 2)),
            Cql2Syntax.GreaterOp => new Cql2TextToken(Cql2TextTokenKind.Greater, _text.Slice(_index - 1, 1)),
            _ => throw new FormatException($"Unexpected character '{ch}'")
        };
    }

    /// <summary>
    /// Reads a single-quoted string literal, returning a slice of its raw inner content
    /// (escaped <c>''</c> pairs preserved for the parser to unescape).
    /// </summary>
    /// <returns>The string token.</returns>
    Cql2TextToken ReadString()
    {
        // step past the opening quote
        var contentStart = ++_index;
        while (_index < _text.Length)
        {
            if (_text[_index] == Cql2Syntax.SingleQuote)
            {
                // a doubled '' is an escaped quote, keep scanning
                if (_index + 1 < _text.Length && _text[_index + 1] == Cql2Syntax.SingleQuote)
                {
                    _index += 2;
                    continue;
                }

                // real closing quote, slice the raw inner content
                var content = _text[contentStart.._index];
                _index++;
                return new Cql2TextToken(Cql2TextTokenKind.String, content);
            }

            _index++;
        }

        throw new FormatException("Unterminated string literal");
    }

    /// <summary>
    /// Classifies a word span as a keyword or a plain identifier.
    /// </summary>
    /// <param name="word">The scanned word span.</param>
    /// <returns>The token.</returns>
    static Cql2TextToken KeywordOrIdentifier(ReadOnlySpan<char> word)
    {
        if (word.Equals(Cql2Syntax.And, StringComparison.OrdinalIgnoreCase)) return new Cql2TextToken(Cql2TextTokenKind.And, word);
        if (word.Equals(Cql2Syntax.Or, StringComparison.OrdinalIgnoreCase)) return new Cql2TextToken(Cql2TextTokenKind.Or, word);
        if (word.Equals(Cql2Syntax.Not, StringComparison.OrdinalIgnoreCase)) return new Cql2TextToken(Cql2TextTokenKind.Not, word);
        if (word.Equals(Cql2Syntax.Is, StringComparison.OrdinalIgnoreCase)) return new Cql2TextToken(Cql2TextTokenKind.Is, word);
        if (word.Equals(Cql2Syntax.True, StringComparison.OrdinalIgnoreCase)) return new Cql2TextToken(Cql2TextTokenKind.True, word);
        if (word.Equals(Cql2Syntax.False, StringComparison.OrdinalIgnoreCase)) return new Cql2TextToken(Cql2TextTokenKind.False, word);
        if (word.Equals(Cql2Syntax.Null, StringComparison.OrdinalIgnoreCase)) return new Cql2TextToken(Cql2TextTokenKind.Null, word);
        return new Cql2TextToken(Cql2TextTokenKind.Identifier, word);
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

    /// <summary>
    /// Determines whether a word is a WKT geometry type keyword.
    /// </summary>
    /// <param name="word">The candidate word span.</param>
    /// <returns><see langword="true"/> when the word names a supported geometry type.</returns>
    static bool IsGeometryKeyword(ReadOnlySpan<char> word)
    {
        return word.Equals(Cql2Syntax.Point, StringComparison.OrdinalIgnoreCase)
            || word.Equals(Cql2Syntax.LineString, StringComparison.OrdinalIgnoreCase)
            || word.Equals(Cql2Syntax.Polygon, StringComparison.OrdinalIgnoreCase)
            || word.Equals(Cql2Syntax.MultiPoint, StringComparison.OrdinalIgnoreCase)
            || word.Equals(Cql2Syntax.MultiLineString, StringComparison.OrdinalIgnoreCase)
            || word.Equals(Cql2Syntax.MultiPolygon, StringComparison.OrdinalIgnoreCase)
            || word.Equals(Cql2Syntax.GeometryCollection, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Consumes an optional WKT dimensionality tag (<c>Z</c>, <c>M</c>, or <c>ZM</c>) that may
    /// appear between a geometry keyword and its opening parenthesis. The tag is advanced over so
    /// that it is retained within the enclosing geometry token's span (and handed to the WKT
    /// reader); if the word is not a tag, the cursor is restored.
    /// </summary>
    void ConsumeDimensionTag()
    {
        var save = _index;
        while (_index < _text.Length && char.IsLetter(_text[_index]))
            _index++;

        var tag = _text[save.._index];
        if (tag.Equals(Cql2Syntax.DimensionZ, StringComparison.OrdinalIgnoreCase)
            || tag.Equals(Cql2Syntax.DimensionM, StringComparison.OrdinalIgnoreCase)
            || tag.Equals(Cql2Syntax.DimensionZM, StringComparison.OrdinalIgnoreCase))
            return;

        // not a tag, give the word back
        _index = save;
    }

    /// <summary>
    /// Consumes a balanced parenthesis group starting at the current opening parenthesis.
    /// </summary>
    void ConsumeBalancedParens()
    {
        var depth = 0;
        while (_index < _text.Length)
        {
            var current = _text[_index++];
            if (current == Cql2Syntax.LeftParen)
            {
                depth++;
            }
            else if (current == Cql2Syntax.RightParen)
            {
                depth--;
                if (depth == 0)
                    return;
            }
        }

        throw new FormatException("Unterminated geometry literal");
    }
}
