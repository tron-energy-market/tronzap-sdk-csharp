using System;

namespace Tronzap.Sdk.Models;

/// <summary>An energy price tier.</summary>
public sealed record EnergyRate
{
    /// <summary>Rental duration in hours.</summary>
    public int Duration { get; init; }

    /// <summary>Smallest amount this tier applies to.</summary>
    public long MinAmount { get; init; }

    /// <summary>Largest amount this tier applies to.</summary>
    public long MaxAmount { get; init; }

    /// <summary>Deprecated: use <see cref="MinAmount"/>, which holds the same value.</summary>
    [Obsolete("Use MinAmount.")]
    public long MinEnergy { get; init; }

    /// <summary>Deprecated: use <see cref="MaxAmount"/>, which holds the same value.</summary>
    [Obsolete("Use MaxAmount.")]
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
