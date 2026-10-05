namespace Tronzap.Sdk.Responses;

/// <summary>The energy a token transfer needs and what that energy costs.</summary>
public sealed record EnergyEstimate
{
    /// <summary>The amount of energy the price applies to.</summary>
    public long Amount { get; init; }

    /// <summary>The energy the transfer needs.</summary>
    public long Energy { get; init; }

    /// <summary>Rental duration in hours.</summary>
    public int Duration { get; init; }

    /// <summary>Price of the energy, in TRX.</summary>
    public decimal Price { get; init; }

    /// <summary>Address activation fee, in TRX.</summary>
    public decimal ActivationFee { get; init; }

    /// <summary>Total cost, in TRX.</summary>
    public decimal Total { get; init; }

    /// <summary>The sender address.</summary>
    public string FromAddress { get; init; } = "";

    /// <summary>The recipient address.</summary>
    public string ToAddress { get; init; } = "";

    /// <summary>The token contract the estimate is for.</summary>
    public string ContractAddress { get; init; } = "";
}
