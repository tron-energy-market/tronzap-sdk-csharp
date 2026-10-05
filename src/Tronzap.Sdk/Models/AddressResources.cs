namespace Tronzap.Sdk.Models;

/// <summary>The resources a TRON address has available.</summary>
public sealed record AddressResources
{
    /// <summary>Available energy.</summary>
    public long Energy { get; init; }

    /// <summary>Available bandwidth.</summary>
    public long Bandwidth { get; init; }
}
