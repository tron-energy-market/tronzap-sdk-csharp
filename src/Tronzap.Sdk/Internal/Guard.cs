using System;
using System.Collections.Generic;

namespace Tronzap.Sdk.Internal;

internal static class Guard
{
    public static string Required(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value;
    }

    public static void Optional(string? value, string name)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} must not be blank when set.", name);
        }
    }

    public static void Positive(long value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be positive.");
        }
    }

    public static void Defined<TEnum>(TEnum value, string name)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value) || EqualityComparer<TEnum>.Default.Equals(value, default))
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be a known value.");
        }
    }
}
