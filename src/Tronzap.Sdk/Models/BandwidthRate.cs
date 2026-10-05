namespace Tronzap.Sdk.Models;

/// <summary>A bandwidth price tier.</summary>
public sealed record BandwidthRate
{
    /// <summary>Rental duration in hours.</summary>
    public int Duration { get; init; }

    /// <summary>Smallest bandwidth amount this tier applies to.</summary>
    public long MinAmount { get; init; }

    /// <summary>Largest bandwidth amount this tier applies to.</summary>
    public long MaxAmount { get; init; }

    /// <summary>Price per 1000 units of bandwidth, in TRX.</summary>
    public decimal Price { get; init; }
}
