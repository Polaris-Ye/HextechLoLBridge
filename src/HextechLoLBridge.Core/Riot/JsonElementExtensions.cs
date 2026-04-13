using System.Text.Json;

namespace HextechLoLBridge.Core.Riot;

internal static class JsonElementExtensions
{
    public static string? GetStringOrNull(this JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    public static double GetDoubleOrDefault(this JsonElement element, string propertyName, double fallback = 0)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble();
        }

        return fallback;
    }

    public static double? GetNullableDouble(this JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyIgnoreCase(element, propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
            {
                return value.GetDouble();
            }
        }

        return null;
    }

    public static int GetInt32OrDefault(this JsonElement element, string propertyName, int fallback = 0)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetInt32(out var result) ? result : fallback;
        }

        return fallback;
    }

    public static int? GetNullableInt32(this JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyIgnoreCase(element, propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
            {
                if (value.TryGetInt32(out var result))
                {
                    return result;
                }
            }
        }

        return null;
    }

    public static bool GetBooleanOrDefault(this JsonElement element, string propertyName, bool fallback = false)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return fallback;
    }

    public static bool? GetNullableBoolean(this JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return null;
    }

    public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName)
    {
        return TryGetPropertyIgnoreCase(element, propertyName, out var value) ? value : null;
    }

    public static bool TryGetPropertyIgnoreCase(this JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
