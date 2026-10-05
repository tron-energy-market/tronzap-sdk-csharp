using System;
using System.Globalization;

namespace Tronzap.Sdk.Models;

/// <summary>A timestamp as the API sent it, together with its parsed value.</summary>
/// <param name="Raw">The text exactly as the API sent it.</param>
/// <param name="Value">The parsed timestamp, or <see langword="null"/> when <paramref name="Raw"/> could not be parsed.</param>
public sealed record Timestamp(string Raw, DateTimeOffset? Value)
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "yyyy-MM-dd HH:mm:ss.FFFFFFFK",
        "yyyy-MM-dd'T'HH:mmK",
        "yyyy-MM-dd",
    ];

    /// <summary>
    /// Parses a timestamp in any of the formats the API emits: RFC 3339 with or without an offset, a
    /// space-separated date and time, a bare date, or Unix seconds. Times without an offset are read as UTC.
    /// Text in an unrecognised format gives a <see cref="Value"/> of <see langword="null"/>.
    /// </summary>
    /// <param name="raw">The timestamp text.</param>
    /// <returns>The timestamp.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="raw"/> is <see langword="null"/>.</exception>
    public static Timestamp Parse(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return new Timestamp(raw, TryParse(raw.Trim()));
    }

    private static DateTimeOffset? TryParse(string text)
    {
        if (text.Length == 0)
        {
            return null;
        }

        if (DateTimeOffset.TryParseExact(
                text,
                Formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out DateTimeOffset parsed))
        {
            return parsed;
        }

        if (text.Length <= 12
            && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out long seconds)
            && seconds <= DateTimeOffset.MaxValue.ToUnixTimeSeconds())
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        return null;
    }
}
