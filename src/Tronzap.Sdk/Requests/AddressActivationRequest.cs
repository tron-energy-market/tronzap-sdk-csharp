using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>A TRON address to activate.</summary>
public sealed record AddressActivationRequest
{
    /// <summary>The TRON address to activate.</summary>
    public required string Address { get; init; }

    /// <summary>Your own ID for the transaction, to look it up with <see cref="CheckTransactionRequest.ByExternalId"/>.</summary>
    public string? ExternalId { get; init; }

    internal void Validate()
    {
        Guard.Required(Address, nameof(Address));
        Guard.Optional(ExternalId, nameof(ExternalId));
    }
}
