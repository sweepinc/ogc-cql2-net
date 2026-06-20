using System;
using System.Collections.Generic;
using System.Globalization;

namespace OgcCql2;

public static class Cql2TextParser
{
    public static Cql2Expression Parse(string text)
    {
        var parser = new Parser(text ?? throw new ArgumentNullException(nameof(text)));
        return parser.Parse();
    }

    private sealed class Parser
    {
        private readonly Lexer _lexer;
        private Token _current;

        public Parser(string text)
        {
            _lexer = new Lexer(text);
            _current = _lexer.Next();
        }

        public Cql2Expression Parse()
        {
            var expr = ParseOr();
            Expect(TokenKind.End);
            return expr;
        }

        private Cql2Expression ParseOr()
        {
            var expr = ParseAnd();
            while (Match(TokenKind.Or))
            {
                expr = new Cql2BinaryExpression(Cql2BinaryOperator.Or, expr, ParseAnd());
            }

            return expr;
        }

        private Cql2Expression ParseAnd()
        {
            var expr = ParseComparison();
            while (Match(TokenKind.And))
            {
                expr = new Cql2BinaryExpression(Cql2BinaryOperator.And, expr, ParseComparison());
            }

            return expr;
        }

        private Cql2Expression ParseComparison()
        {
            var left = ParseUnary();

            if (Match(TokenKind.Equal))
            {
                return new Cql2BinaryExpression(Cql2BinaryOperator.Equal, left, ParseUnary());
            }

            if (Match(TokenKind.NotEqual))
            {
                return new Cql2BinaryExpression(Cql2BinaryOperator.NotEqual, left, ParseUnary());
            }

            if (Match(TokenKind.Less))
            {
                return new Cql2BinaryExpression(Cql2BinaryOperator.LessThan, left, ParseUnary());
            }

            if (Match(TokenKind.LessOrEqual))
            {
                return new Cql2BinaryExpression(Cql2BinaryOperator.LessThanOrEqual, left, ParseUnary());
            }

            if (Match(TokenKind.Greater))
            {
                return new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThan, left, ParseUnary());
            }

            if (Match(TokenKind.GreaterOrEqual))
            {
                return new Cql2BinaryExpression(Cql2BinaryOperator.GreaterThanOrEqual, left, ParseUnary());
            }

            return left;
        }

        private Cql2Expression ParseUnary()
        {
            if (Match(TokenKind.Not))
            {
                return new Cql2UnaryExpression(Cql2UnaryOperator.Not, ParseUnary());
            }

            return ParsePrimary();
        }

        private Cql2Expression ParsePrimary()
        {
            if (Match(TokenKind.LeftParen))
            {
                var expr = ParseOr();
                Expect(TokenKind.RightParen);
                return expr;
            }

            if (_current.Kind == TokenKind.String ||
                _current.Kind == TokenKind.Number ||
                _current.Kind == TokenKind.True ||
                _current.Kind == TokenKind.False ||
                _current.Kind == TokenKind.Null)
            {
                var literal = _current.Value;
                Advance();
                return new Cql2LiteralExpression(literal);
            }

            if (_current.Kind == TokenKind.Identifier)
            {
                var name = _current.Text;
                Advance();

                if (Match(TokenKind.LeftParen))
                {
                    var args = new List<Cql2Expression>();
                    if (!Match(TokenKind.RightParen))
                    {
                        do
                        {
                            args.Add(ParseOr());
                        }
                        while (Match(TokenKind.Comma));

                        Expect(TokenKind.RightParen);
                    }

                    return new Cql2FunctionCallExpression(name, args);
                }

                return new Cql2PropertyExpression(name);
            }

            throw new FormatException($"Unexpected token '{_current.Text}'");
        }

        private bool Match(TokenKind kind)
        {
            if (_current.Kind != kind)
            {
                return false;
            }

            Advance();
            return true;
        }

        private void Expect(TokenKind kind)
        {
            if (!Match(kind))
            {
                throw new FormatException($"Expected {kind} but found '{_current.Text}'");
            }
        }

        private void Advance() => _current = _lexer.Next();
    }

    private enum TokenKind
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

    private readonly record struct Token(TokenKind Kind, string Text, object? Value = null);

    private sealed class Lexer
    {
        private readonly string _text;
        private int _index;

        public Lexer(string text)
        {
            _text = text;
        }

        public Token Next()
        {
            SkipWhitespace();
            if (_index >= _text.Length)
            {
                return new Token(TokenKind.End, string.Empty);
            }

            var ch = _text[_index];
            if (char.IsLetter(ch) || ch == '_')
            {
                var start = _index++;
                while (_index < _text.Length && (char.IsLetterOrDigit(_text[_index]) || _text[_index] == '_' || _text[_index] == '.'))
                {
                    _index++;
                }

                var text = _text[start.._index];
                return text.ToUpperInvariant() switch
                {
                    "AND" => new Token(TokenKind.And, text),
                    "OR" => new Token(TokenKind.Or, text),
                    "NOT" => new Token(TokenKind.Not, text),
                    "TRUE" => new Token(TokenKind.True, text, true),
                    "FALSE" => new Token(TokenKind.False, text, false),
                    "NULL" => new Token(TokenKind.Null, text, null),
                    _ => new Token(TokenKind.Identifier, text)
                };
            }

            if (char.IsDigit(ch) || ch == '-')
            {
                var start = _index++;
                while (_index < _text.Length && (char.IsDigit(_text[_index]) || _text[_index] == '.'))
                {
                    _index++;
                }

                var text = _text[start.._index];
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                {
                    return new Token(TokenKind.Number, text, integer);
                }

                return new Token(TokenKind.Number, text, double.Parse(text, CultureInfo.InvariantCulture));
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

                        return new Token(TokenKind.String, value, value);
                    }

                    value += current;
                }

                throw new FormatException("Unterminated string literal");
            }

            _index++;
            return ch switch
            {
                '(' => new Token(TokenKind.LeftParen, "("),
                ')' => new Token(TokenKind.RightParen, ")"),
                ',' => new Token(TokenKind.Comma, ","),
                '=' => new Token(TokenKind.Equal, "="),
                '<' when Match('=') => new Token(TokenKind.LessOrEqual, "<="),
                '<' when Match('>') => new Token(TokenKind.NotEqual, "<>"),
                '<' => new Token(TokenKind.Less, "<"),
                '>' when Match('=') => new Token(TokenKind.GreaterOrEqual, ">="),
                '>' => new Token(TokenKind.Greater, ">"),
                _ => throw new FormatException($"Unexpected character '{ch}'")
            };
        }

        private bool Match(char expected)
        {
            if (_index < _text.Length && _text[_index] == expected)
            {
                _index++;
                return true;
            }

            return false;
        }

        private void SkipWhitespace()
        {
            while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            {
                _index++;
            }
        }
    }
}
