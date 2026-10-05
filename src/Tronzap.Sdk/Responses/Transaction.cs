using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>A purchase of energy, bandwidth or address activation.</summary>
public sealed record Transaction
{
    /// <summary>The transaction ID assigned by TronZap.</summary>
    public string Id { get; init; } = "";

    /// <summary>The ID you passed when creating the transaction, or <see langword="null"/>.</summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// The service of the transaction. The API currently reports a resource bundle as
    /// <see cref="ServiceType.Energy"/>; read <see cref="TransactionParams.Amounts"/> to see which resources it contains.
    /// </summary>
    public ServiceType Service { get; init; }

    /// <summary>The parameters the transaction was created with.</summary>
    public TransactionParams Params { get; init; } = new();

    /// <summary>The current state of the transaction.</summary>
    public TransactionStatus Status { get; init; }

    /// <summary>The amount charged, in TRX.</summary>
    public decimal Amount { get; init; }

    /// <summary>When the transaction was created, or <see langword="null"/> when the API did not report it.</summary>
    public Timestamp? CreatedAt { get; init; }

    /// <summary>The on-chain transaction hash, or <see langword="null"/> when the API did not report one.</summary>
    public string? Hash { get; init; }
}
