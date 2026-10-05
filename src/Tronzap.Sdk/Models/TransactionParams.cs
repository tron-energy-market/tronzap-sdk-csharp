namespace Tronzap.Sdk.Models;

/// <summary>The parameters a transaction was created with.</summary>
public sealed record TransactionParams
{
    /// <summary>The TRON address that receives the resources.</summary>
    public string Address { get; init; } = "";

    /// <summary>Rental duration in hours.</summary>
    public int Duration { get; init; }

    /// <summary>The resources the transaction buys.</summary>
    public ResourceAmounts Amounts { get; init; } = new();

    /// <summary>Whether the transaction also activates the address.</summary>
    public bool ActivateAddress { get; init; }
}
