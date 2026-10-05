using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Internal;

internal sealed class MappingException(string message, Exception? innerException = null) : Exception(message, innerException);

internal static class JsonValues
{
    private static readonly JsonElement EmptyObject = CreateEmptyObject();

    public static bool IsAbsent(JsonElement element) =>
        element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null;

    public static JsonElement Field(JsonElement obj, string name) =>
        obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out JsonElement value) ? value : default;

    // The API is backed by PHP, which encodes an empty associative array as [].
    public static JsonElement Object(JsonElement element, string name)
    {
        if (IsAbsent(element) || (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 0))
        {
            return EmptyObject;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new MappingException($"{name} is not an object");
        }

        return element;
    }

    public static IReadOnlyList<T> List<T>(JsonElement obj, string field, Func<JsonElement, T> map) =>
        Elements(Field(obj, field), field, map);

    public static IReadOnlyList<T> Elements<T>(JsonElement element, string name, Func<JsonElement, T> map)
    {
        if (IsAbsent(element) || (element.ValueKind == JsonValueKind.Object && !element.EnumerateObject().MoveNext()))
        {
            return [];
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new MappingException($"{name} is not an array");
        }

        var items = new List<T>(element.GetArrayLength());
        foreach (JsonElement item in element.EnumerateArray())
        {
            items.Add(map(item));
        }

        return items.AsReadOnly();
    }

    public static IReadOnlyDictionary<string, decimal> DecimalMap(JsonElement obj, string field)
    {
        JsonElement element = Object(Field(obj, field), field);
        var values = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            values[property.Name] = ToDecimal(property.Value, $"{field}.{property.Name}");
        }

        return values.AsReadOnly();
    }

    public static string Text(JsonElement obj, string field)
    {
        JsonElement element = Field(obj, field);
        return element.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => "",
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => throw new MappingException($"{field} is not a string"),
        };
    }

    public static string? OptionalText(JsonElement obj, string field)
    {
        string value = Text(obj, field);
        return value.Length == 0 ? null : value;
    }

    public static long Int64(JsonElement obj, string field)
    {
        JsonElement element = Field(obj, field);
        if (IsAbsent(element))
        {
            return 0;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out long integer))
        {
            return integer;
        }

        if (element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString()))
        {
            return 0;
        }

        if (element.ValueKind is JsonValueKind.Number or JsonValueKind.String)
        {
            decimal value = ToDecimal(element, field);
            if (value == decimal.Truncate(value) && value >= long.MinValue && value <= long.MaxValue)
            {
                return (long)value;
            }
        }

        throw new MappingException($"{field} is not an integer: {element.GetRawText()}");
    }

    public static int Int32(JsonElement obj, string field)
    {
        long value = Int64(obj, field);
        if (value is < int.MinValue or > int.MaxValue)
        {
            throw new MappingException($"{field} is out of range: {value}");
        }

        return (int)value;
    }

    public static decimal Decimal(JsonElement obj, string field) => OptionalDecimal(obj, field) ?? 0m;

    public static decimal? OptionalDecimal(JsonElement obj, string field)
    {
        JsonElement element = Field(obj, field);
        if (IsAbsent(element) || (element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString())))
        {
            return null;
        }

        return ToDecimal(element, field);
    }

    // Amounts arrive as JSON numbers from some endpoints and as strings from others.
    public static decimal ToDecimal(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out decimal number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String
            && decimal.TryParse(element.GetString()?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal text))
        {
            return text;
        }

        throw new MappingException($"{name} is not a number: {element.GetRawText()}");
    }

    public static bool Bool(JsonElement obj, string field)
    {
        JsonElement element = Field(obj, field);
        switch (element.ValueKind)
        {
            case JsonValueKind.Undefined or JsonValueKind.Null or JsonValueKind.False:
                return false;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.Number:
                return ToDecimal(element, field) != 0m;
            case JsonValueKind.String:
                switch (element.GetString()?.Trim())
                {
                    case "true" or "1":
                        return true;
                    case "" or "false" or "0":
                        return false;
                }

                break;
        }

        throw new MappingException($"{field} is not a boolean: {element.GetRawText()}");
    }

    public static Timestamp? Timestamp(JsonElement obj, string field)
    {
        JsonElement element = Field(obj, field);
        string? raw = element.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out _) => element.GetRawText(),
            _ => throw new MappingException($"{field} is not a timestamp: {element.GetRawText()}"),
        };
        return string.IsNullOrWhiteSpace(raw) ? null : Models.Timestamp.Parse(raw);
    }

    private static JsonElement CreateEmptyObject()
    {
        using JsonDocument document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }
}
