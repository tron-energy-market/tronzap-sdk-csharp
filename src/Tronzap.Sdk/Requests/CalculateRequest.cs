using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>An energy purchase to price.</summary>
public sealed record CalculateRequest
{
    /// <summary>The TRON address that would receive the energy.</summary>
    public required string Address { get; init; }

    /// <summary>The amount of energy.</summary>
    public required long Energy { get; init; }

    /// <summary>Rental duration in hours, one of the durations <see cref="ITronzapClient.GetServicesAsync"/> lists. Defaults to 1.</summary>
    public int Duration { get; init; } = 1;

    internal void Validate()
    {
        Guard.Required(Address, nameof(Address));
        Guard.Positive(Energy, nameof(Energy));
        Guard.Positive(Duration, nameof(Duration));
    }
}
