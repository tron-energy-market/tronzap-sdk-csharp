using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>A bandwidth purchase. Bandwidth is rented for one hour.</summary>
public sealed record BandwidthTransactionRequest
{
    /// <summary>The TRON address that receives the bandwidth.</summary>
    public required string Address { get; init; }

    /// <summary>The amount of bandwidth to buy.</summary>
    public required long Bandwidth { get; init; }

    /// <summary>Your own ID for the transaction, to look it up with <see cref="CheckTransactionRequest.ByExternalId"/>.</summary>
    public string? ExternalId { get; init; }

    internal void Validate()
    {
        Guard.Required(Address, nameof(Address));
        Guard.Positive(Bandwidth, nameof(Bandwidth));
        Guard.Optional(ExternalId, nameof(ExternalId));
    }
}
