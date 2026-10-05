using System;
using Tronzap.Sdk.Models;
using Xunit;

namespace Tronzap.Sdk.Tests;

public sealed class TimestampTests
{
    [Theory]
    [InlineData("2026-08-10T12:30:00+00:00", 0)]
    [InlineData("2026-08-10T12:30:00Z", 0)]
    [InlineData("2026-08-10T15:30:00+03:00", 3)]
    [InlineData("2026-08-10T12:30:00.123456Z", 0)]
    [InlineData("2026-08-10T12:30:00", 0)]
    [InlineData("2026-08-10 12:30:00", 0)]
    [InlineData("2026-08-10T12:30Z", 0)]
    [InlineData("  2026-08-10T12:30:00Z  ", 0)]
    public void ParsesApiFormats(string raw, int offsetHours)
    {
        Timestamp timestamp = Timestamp.Parse(raw);

        Assert.Equal(raw, timestamp.Raw);
        Assert.NotNull(timestamp.Value);
        Assert.Equal(TimeSpan.FromHours(offsetHours), timestamp.Value.Value.Offset);
        Assert.Equal(new DateTime(2026, 8, 10, 12, 30, 0, DateTimeKind.Utc), timestamp.Value.Value.UtcDateTime.AddTicks(-(timestamp.Value.Value.UtcDateTime.Ticks % TimeSpan.TicksPerSecond)));
    }

    [Fact]
    public void ParsesBareDateAsUtcMidnight() =>
        Assert.Equal(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero), Timestamp.Parse("2026-08-10").Value);

    [Fact]
    public void ParsesUnixSeconds() =>
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1786364400), Timestamp.Parse("1786364400").Value);

    [Theory]
    [InlineData("")]
    [InlineData("yesterday")]
    [InlineData("10/08/2026")]
    [InlineData("2026-13-40T99:00:00Z")]
    [InlineData("99999999999999")]
    public void KeepsUnparsableTextWithoutFailing(string raw)
    {
        Timestamp timestamp = Timestamp.Parse(raw);

        Assert.Equal(raw, timestamp.Raw);
        Assert.Null(timestamp.Value);
    }

    [Fact]
    public void RejectsNull() => Assert.Throws<ArgumentNullException>(() => Timestamp.Parse(null!));
}
