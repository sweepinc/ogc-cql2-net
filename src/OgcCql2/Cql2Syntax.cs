namespace OgcCql2;

/// <summary>
/// Literal keyword and marker strings shared by the CQL2 text lexer, parsers, and formatters.
/// </summary>
static class Cql2Syntax
{

    /// <summary>The open-bound marker for an unbounded interval endpoint.</summary>
    public const string OpenBound = "..";

    /// <summary>Logical conjunction keyword.</summary>
    public const string And = "AND";

    /// <summary>Logical disjunction keyword.</summary>
    public const string Or = "OR";

    /// <summary>Logical negation keyword.</summary>
    public const string Not = "NOT";

    /// <summary>The <c>IS</c> keyword of the null predicate.</summary>
    public const string Is = "IS";

    /// <summary>The <c>NULL</c> keyword of the null predicate.</summary>
    public const string Null = "NULL";

    /// <summary>Equality comparison operator symbol.</summary>
    public const string EqualOperator = "=";

    /// <summary>Inequality comparison operator symbol.</summary>
    public const string NotEqualOperator = "<>";

    /// <summary>Less-than comparison operator symbol.</summary>
    public const string LessThanOperator = "<";

    /// <summary>Less-than-or-equal comparison operator symbol.</summary>
    public const string LessThanOrEqualOperator = "<=";

    /// <summary>Greater-than comparison operator symbol.</summary>
    public const string GreaterThanOperator = ">";

    /// <summary>Greater-than-or-equal comparison operator symbol.</summary>
    public const string GreaterThanOrEqualOperator = ">=";

    /// <summary>Boolean true keyword.</summary>
    public const string True = "TRUE";

    /// <summary>Boolean false keyword.</summary>
    public const string False = "FALSE";

    /// <summary>Date literal constructor keyword.</summary>
    public const string Date = "DATE";

    /// <summary>Timestamp literal constructor keyword.</summary>
    public const string Timestamp = "TIMESTAMP";

    /// <summary>Interval literal constructor keyword.</summary>
    public const string Interval = "INTERVAL";

    /// <summary>WKT point geometry keyword.</summary>
    public const string Point = "POINT";

    /// <summary>WKT line-string geometry keyword.</summary>
    public const string LineString = "LINESTRING";

    /// <summary>WKT polygon geometry keyword.</summary>
    public const string Polygon = "POLYGON";

    /// <summary>WKT multi-point geometry keyword.</summary>
    public const string MultiPoint = "MULTIPOINT";

    /// <summary>WKT multi-line-string geometry keyword.</summary>
    public const string MultiLineString = "MULTILINESTRING";

    /// <summary>WKT multi-polygon geometry keyword.</summary>
    public const string MultiPolygon = "MULTIPOLYGON";

    /// <summary>WKT geometry-collection keyword.</summary>
    public const string GeometryCollection = "GEOMETRYCOLLECTION";

    /// <summary>WKT Z (3D) dimensionality tag.</summary>
    public const string DimensionZ = "Z";

    /// <summary>WKT M (measured) dimensionality tag.</summary>
    public const string DimensionM = "M";

    /// <summary>WKT ZM (3D measured) dimensionality tag.</summary>
    public const string DimensionZM = "ZM";

    /// <summary>Identifier word separator / leading character.</summary>
    public const char Underscore = '_';

    /// <summary>Decimal point and nested-property separator.</summary>
    public const char Dot = '.';

    /// <summary>String literal delimiter.</summary>
    public const char SingleQuote = '\'';

    /// <summary>Opening parenthesis.</summary>
    public const char LeftParen = '(';

    /// <summary>Closing parenthesis.</summary>
    public const char RightParen = ')';

    /// <summary>Argument and element separator.</summary>
    public const char Comma = ',';

    /// <summary>Equality operator character.</summary>
    public const char EqualOp = '=';

    /// <summary>Less-than operator character.</summary>
    public const char LessOp = '<';

    /// <summary>Greater-than operator character.</summary>
    public const char GreaterOp = '>';

    /// <summary>Numeric sign / exponent positive sign.</summary>
    public const char Plus = '+';

    /// <summary>Numeric sign / exponent negative sign.</summary>
    public const char Minus = '-';

    /// <summary>Lower-case scientific-notation exponent marker.</summary>
    public const char ExponentLower = 'e';

    /// <summary>Upper-case scientific-notation exponent marker.</summary>
    public const char ExponentUpper = 'E';

}
