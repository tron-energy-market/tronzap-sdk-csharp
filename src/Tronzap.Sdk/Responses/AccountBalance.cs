namespace Tronzap.Sdk.Responses;

/// <summary>The account balance and the address that tops it up.</summary>
public sealed record AccountBalance
{
    /// <summary>Available balance, in TRX.</summary>
    public decimal Balance { get; init; }

    /// <summary>The TRON address to send TRX to in order to top up the balance.</summary>
    public string Address { get; init; } = "";
}
