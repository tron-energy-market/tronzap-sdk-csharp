namespace Tronzap.Sdk.Requests;

/// <summary>
/// One row of a bulk energy order. The SDK does not check the address or the amount: the calculation rejects invalid
/// rows and reports them with their row number.
/// </summary>
public sealed record BulkItem
{
    /// <summary>The TRON address that receives the energy.</summary>
    public required string Address { get; init; }

    /// <summary>The amount of energy to buy.</summary>
    public required long Amount { get; init; }
}
