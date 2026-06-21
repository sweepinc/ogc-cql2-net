using System;
using System.Text.Json;
using System.Text.Json.Nodes;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.IO.Converters;

namespace OgcCql2.Geometries;

/// <summary>
/// Reads and writes NetTopologySuite <see cref="Geometry"/> values as CQL2 spatial literals:
/// OGC Well-Known Text (WKT) for CQL2-Text and GeoJSON for CQL2-JSON.
/// </summary>
public static class GeometryIo
{

    static readonly JsonSerializerOptions s_geoJsonOptions = CreateGeoJsonOptions();

    static JsonSerializerOptions CreateGeoJsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new GeoJsonConverterFactory());
        return options;
    }

    /// <summary>
    /// Parses a WKT geometry literal.
    /// </summary>
    /// <param name="wkt">The WKT text.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="FormatException">Thrown when the WKT text is malformed.</exception>
    public static Geometry ReadWkt(string wkt)
    {
        ArgumentNullException.ThrowIfNull(wkt);
        try
        {
            return new WKTReader().Read(wkt);
        }
        catch (Exception ex) when (ex is not FormatException)
        {
            throw new FormatException($"Invalid WKT geometry: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Formats a geometry as WKT.
    /// </summary>
    /// <param name="geometry">The geometry to format.</param>
    /// <returns>The WKT representation.</returns>
    public static string WriteWkt(Geometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        return new WKTWriter().Write(geometry);
    }

    /// <summary>
    /// Reads a GeoJSON geometry object from a forward-only reader positioned on its opening object.
    /// </summary>
    /// <param name="reader">The forward-only JSON reader.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="FormatException">Thrown when the GeoJSON geometry is malformed.</exception>
    public static Geometry ReadGeoJson(ref Utf8JsonReader reader)
    {
        try
        {
            return JsonSerializer.Deserialize<Geometry>(ref reader, s_geoJsonOptions)
                ?? throw new FormatException("GeoJSON geometry must not be null.");
        }
        catch (JsonException ex)
        {
            throw new FormatException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Converts a geometry to its GeoJSON JSON node representation.
    /// </summary>
    /// <param name="geometry">The geometry to convert.</param>
    /// <returns>The GeoJSON node.</returns>
    public static JsonNode WriteGeoJson(Geometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        return JsonSerializer.SerializeToNode(geometry, s_geoJsonOptions)
            ?? throw new NotSupportedException("Failed to serialize geometry to GeoJSON.");
    }

    /// <summary>
    /// Determines whether the object the reader is positioned on is a GeoJSON geometry, by
    /// peeking for a <c>type</c>, <c>coordinates</c>, or <c>geometries</c> member. The reader is
    /// passed by value so the caller's cursor is unaffected.
    /// </summary>
    /// <param name="reader">A copy of the reader positioned at the opening object.</param>
    /// <returns><see langword="true"/> when the object is a GeoJSON geometry.</returns>
    public static bool LooksLikeGeometry(Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject || !reader.Read())
            return false;

        while (reader.TokenType == JsonTokenType.PropertyName)
        {
            var name = reader.GetString();
            if (name is "type" or "coordinates" or "geometries")
                return true;

            reader.Read();
            reader.Skip();
            reader.Read();
        }

        return false;
    }

}
