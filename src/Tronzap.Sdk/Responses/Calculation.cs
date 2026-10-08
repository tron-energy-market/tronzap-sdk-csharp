using System;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>The price of an energy purchase.</summary>
public sealed record Calculation
{
    /// <summary>The TRON address that would receive the energy.</summary>
    public string Address { get; init; } = "";

    /// <summary>The service the price is for.</summary>
    public ServiceType Service { get; init; }

    /// <summary>The amount priced.</summary>
    public long Amount { get; init; }

    /// <summary>Deprecated: use <see cref="Amount"/>, which holds the same value.</summary>
    [Obsolete("Use Amount.")]
    public long Energy { get; init; }

    /// <summary>Rental duration in hours.</summary>
    public int Duration { get; init; }

    /// <summary>Price of the energy, in TRX.</summary>
    public decimal Price { get; init; }

    /// <summary>Address activation fee, in TRX.</summary>
    public decimal ActivationFee { get; init; }

    /// <summary>Total cost, in TRX.</summary>
    public decimal Total { get; init; }
}
