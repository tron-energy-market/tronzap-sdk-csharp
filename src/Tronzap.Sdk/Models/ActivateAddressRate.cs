namespace Tronzap.Sdk.Models;

/// <summary>The price of activating a TRON address.</summary>
public sealed record ActivateAddressRate
{
    /// <summary>Activation price, in TRX.</summary>
    public decimal Price { get; init; }
}
