using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>A purchase of energy and bandwidth in one transaction.</summary>
public sealed record ResourceBundleTransactionRequest
{
    /// <summary>The TRON address that receives the resources.</summary>
    public required string Address { get; init; }

    /// <summary>The amount of energy to buy.</summary>
    public required long Energy { get; init; }

    /// <summary>The amount of bandwidth to buy.</summary>
    public required long Bandwidth { get; init; }

    /// <summary>Rental duration in hours. Defaults to 1.</summary>
    public int Duration { get; init; } = 1;

    /// <summary>Your own ID for the transaction, to look it up with <see cref="CheckTransactionRequest.ByExternalId"/>.</summary>
    public string? ExternalId { get; init; }

    /// <summary>Whether to activate the address in the same transaction.</summary>
    public bool ActivateAddress { get; init; }

    internal void Validate()
    {
        Guard.Required(Address, nameof(Address));
        Guard.Positive(Energy, nameof(Energy));
        Guard.Positive(Bandwidth, nameof(Bandwidth));
        Guard.Positive(Duration, nameof(Duration));
        Guard.Optional(ExternalId, nameof(ExternalId));
    }
}
