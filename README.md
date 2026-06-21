# OgcCql2

[![Build](https://github.com/sweepinc/ogc-cql2-net/actions/workflows/OgcCql2.yml/badge.svg)](https://github.com/sweepinc/ogc-cql2-net/actions/workflows/OgcCql2.yml)

`OgcCql2` is a .NET library for working with [OGC CQL2](https://docs.ogc.org/is/21-065r2/21-065r2.html) expressions.
It provides:

- A typed expression tree (AST)
- CQL2-text parsing and formatting
- CQL2-JSON parsing and formatting
- Boolean logic (`AND`/`OR`/`NOT`), comparisons, and the `IS [NOT] NULL` predicate
- Scalar literals (string, number, boolean), temporal literals (`DATE`, `TIMESTAMP`, `INTERVAL`), arrays, and spatial literals (WKT / GeoJSON)
- Visitor-based traversal for custom processing
- Stable round-tripping across text, JSON, and AST

Spatial literals are represented as [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite)
geometries: WKT is parsed/written for CQL2-Text and GeoJSON for CQL2-JSON.

## Installation

```bash
dotnet add package OgcCql2
```

## Quick start

```csharp
using OgcCql2.Formatting;
using OgcCql2.Parsing;

var textExpression = Cql2TextParser.Parse("foo = 1 AND NOT bar >= 10");
var canonicalText = Cql2TextFormatter.Format(textExpression);

var json = Cql2JsonFormatter.Format(textExpression);
var fromJson = Cql2JsonParser.Parse(json);

var roundTripText = Cql2TextFormatter.Format(fromJson);
```

## Core API

### Parse CQL2 text

```csharp
var expression = Cql2TextParser.Parse("contains(name, 'abc') AND count >= 10");
```

### Parse CQL2 JSON

```csharp
var json = """
{
  "op": "and",
  "args": [
    { "op": "contains", "args": [ { "property": "name" }, "abc" ] },
    { "op": ">=", "args": [ { "property": "count" }, 10 ] }
  ]
}
""";

var expression = Cql2JsonParser.Parse(json);
```

### Format canonical text

```csharp
var canonicalText = Cql2TextFormatter.Format(expression);
```

### Format canonical JSON

```csharp
var canonicalJson = Cql2JsonFormatter.Format(expression);
```

## Expression model

The AST is rooted at `Cql2Expression`. Constant literals derive from the abstract
`Cql2LiteralExpression` base (`Cql2StringExpression`, `Cql2NumberExpression`, `Cql2BooleanExpression`,
`Cql2DateExpression`, `Cql2TimestampExpression`, `Cql2GeometryExpression`); composite forms that may
contain non-constant sub-expressions (intervals, arrays) derive from `Cql2Expression` directly.

Concrete node types:

- `Cql2StringExpression`, `Cql2NumberExpression` (`decimal`), `Cql2BooleanExpression` (CQL2 has no null literal)
- `Cql2PropertyExpression`
- `Cql2UnaryExpression`
- `Cql2BinaryExpression`
- `Cql2FunctionCallExpression` (`ImmutableArray` arguments; function names are open-ended)
- `Cql2IsNullExpression`
- `Cql2ArrayExpression` (`ImmutableArray` of element expressions)
- `Cql2DateExpression` (`DateOnly`), `Cql2TimestampExpression` (`DateTimeOffset`, UTC), `Cql2IntervalExpression` (nullable expression bounds; `null` = open `..`)
- `Cql2GeometryExpression` (wraps a NetTopologySuite `Geometry`)

Operators are represented by:

- `Cql2UnaryOperator`
- `Cql2BinaryOperator`

Traversal uses:

- `ICqlExpressionVisitor<T>`

## Visitor example

```csharp
using OgcCql2.Expressions;

sealed class NodeCountingVisitor : ICqlExpressionVisitor<int>
{
    public int VisitString(Cql2StringExpression expression) => 1;
    public int VisitNumber(Cql2NumberExpression expression) => 1;
    public int VisitBoolean(Cql2BooleanExpression expression) => 1;
    public int VisitProperty(Cql2PropertyExpression expression) => 1;
    public int VisitUnary(Cql2UnaryExpression expression) => 1 + expression.Operand.Accept(this);
    public int VisitBinary(Cql2BinaryExpression expression) => 1 + expression.Left.Accept(this) + expression.Right.Accept(this);
    public int VisitIsNull(Cql2IsNullExpression expression) => 1 + expression.Operand.Accept(this);
    public int VisitDate(Cql2DateExpression expression) => 1;
    public int VisitTimestamp(Cql2TimestampExpression expression) => 1;
    public int VisitInterval(Cql2IntervalExpression expression) => 1;
    public int VisitGeometry(Cql2GeometryExpression expression) => 1;

    public int VisitFunctionCall(Cql2FunctionCallExpression expression)
    {
        var count = 1;
        foreach (var argument in expression.Arguments)
            count += argument.Accept(this);

        return count;
    }

    public int VisitArray(Cql2ArrayExpression expression)
    {
        var count = 1;
        foreach (var element in expression.Elements)
            count += element.Accept(this);

        return count;
    }
}
```

## Round-tripping behavior

This library is designed for deterministic round-tripping:

- text → AST → canonical text
- JSON → AST → canonical JSON
- text ↔ JSON through the same AST

Logical `and`/`or` are normalized for JSON output and emitted with canonical operator text in CQL2-text output.

## Development

Build:

```bash
dotnet build OgcCql2.slnx -c Release
```

Test:

```bash
dotnet test OgcCql2.slnx -c Release
```

## License

Apache 2.0
