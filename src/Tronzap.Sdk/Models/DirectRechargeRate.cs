namespace Tronzap.Sdk.Models;

/// <summary>A rate energy is delivered at for a direct recharge.</summary>
public sealed record DirectRechargeRate
{
    /// <summary>Rental duration in hours.</summary>
    public int Duration { get; init; }

    /// <summary>Smallest energy amount this rate applies to.</summary>
    public long MinEnergy { get; init; }

    /// <summary>Largest energy amount this rate applies to.</summary>
    public long MaxEnergy { get; init; }

    /// <summary>Price per 1000 units of energy, in TRX.</summary>
    public decimal Price { get; init; }

    /// <summary>Price of 32 000 energy, in TRX.</summary>
    public decimal Price32K { get; init; }

    /// <summary>Price of 65 000 energy, in TRX.</summary>
    public decimal Price65K { get; init; }

    /// <summary>Price of 131 000 energy, in TRX.</summary>
    public decimal Price131K { get; init; }
}
