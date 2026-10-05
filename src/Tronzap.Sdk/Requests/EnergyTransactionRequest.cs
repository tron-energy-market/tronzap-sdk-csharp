using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>An energy purchase.</summary>
public sealed record EnergyTransactionRequest
{
    /// <summary>The TRON address that receives the energy.</summary>
    public required string Address { get; init; }

    /// <summary>The amount of energy to buy.</summary>
    public required long Energy { get; init; }

    /// <summary>Rental duration in hours, one of the durations <see cref="ITronzapClient.GetServicesAsync"/> lists. Defaults to 1.</summary>
    public int Duration { get; init; } = 1;

    /// <summary>Your own ID for the transaction, to look it up with <see cref="CheckTransactionRequest.ByExternalId"/>.</summary>
    public string? ExternalId { get; init; }

    /// <summary>Whether to activate the address in the same transaction.</summary>
    public bool ActivateAddress { get; init; }

    internal void Validate()
    {
        Guard.Required(Address, nameof(Address));
        Guard.Positive(Energy, nameof(Energy));
        Guard.Positive(Duration, nameof(Duration));
        Guard.Optional(ExternalId, nameof(ExternalId));
    }
}
