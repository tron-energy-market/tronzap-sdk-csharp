namespace Tronzap.Sdk.Models;

/// <summary>The parameters a subscription was started with.</summary>
public sealed record SubscriptionParams
{
    /// <summary>The TRON address the subscription serves.</summary>
    public string Address { get; init; } = "";

    /// <summary>How many days the subscription runs, 0 for no time limit.</summary>
    public int DurationDays { get; init; }

    /// <summary>How many transactions the subscription covers, 0 for no limit.</summary>
    public long TransactionsLimit { get; init; }

    /// <summary>Whether activation of the address was requested.</summary>
    public bool ActivateAddress { get; init; }
}
