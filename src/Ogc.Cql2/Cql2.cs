namespace Ogc.Cql2;

public static class Cql2
{
    public static Cql2Expression ParseText(string text) => Cql2TextParser.Parse(text);

    public static Cql2Expression ParseJson(string json) => Cql2JsonParser.Parse(json);

    public static string ToText(Cql2Expression expression) => Cql2TextFormatter.Format(expression);

    public static string ToJson(Cql2Expression expression) => Cql2JsonFormatter.Format(expression);
}
