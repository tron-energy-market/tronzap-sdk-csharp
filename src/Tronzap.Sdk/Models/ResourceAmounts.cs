namespace Tronzap.Sdk.Models;

/// <summary>The resources a transaction buys. A resource the transaction does not include is 0.</summary>
public sealed record ResourceAmounts
{
    /// <summary>Energy bought.</summary>
    public long Energy { get; init; }

    /// <summary>Bandwidth bought.</summary>
    public long Bandwidth { get; init; }
}
