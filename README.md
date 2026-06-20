# OgcCql2

[![Build](https://github.com/sweepinc/ogc-cql2-net/actions/workflows/OgcCql2.yml/badge.svg)](https://github.com/sweepinc/ogc-cql2-net/actions/workflows/OgcCql2.yml)

`OgcCql2` is a .NET library for working with [OGC CQL2](https://docs.ogc.org/is/21-065r2/21-065r2.html) expressions.
It provides:

- A typed expression tree (AST)
- CQL2-text parsing and formatting
- CQL2-JSON parsing and formatting
- Visitor-based traversal for custom processing
- Stable round-tripping across text, JSON, and AST

## Installation

```bash
dotnet add package OgcCql2
```

## Quick start

```csharp
using OgcCql2;

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

The AST is rooted at `Cql2Expression` with concrete node types:

- `Cql2LiteralExpression`
- `Cql2PropertyExpression`
- `Cql2UnaryExpression`
- `Cql2BinaryExpression`
- `Cql2FunctionCallExpression`

Operators are represented by:

- `Cql2UnaryOperator`
- `Cql2BinaryOperator`

Traversal uses:

- `ICqlExpressionVisitor<T>`

## Visitor example

```csharp
using OgcCql2;

sealed class NodeCountingVisitor : ICqlExpressionVisitor<int>
{
    public int VisitLiteral(Cql2LiteralExpression expression) => 1;
    public int VisitProperty(Cql2PropertyExpression expression) => 1;
    public int VisitUnary(Cql2UnaryExpression expression) => 1 + expression.Operand.Accept(this);
    public int VisitBinary(Cql2BinaryExpression expression) => 1 + expression.Left.Accept(this) + expression.Right.Accept(this);

    public int VisitFunctionCall(Cql2FunctionCallExpression expression)
    {
        var count = 1;
        foreach (var argument in expression.Arguments)
            count += argument.Accept(this);

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
