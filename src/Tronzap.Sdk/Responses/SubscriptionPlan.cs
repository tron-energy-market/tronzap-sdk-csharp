using Tronzap.Sdk.Requests;

namespace Tronzap.Sdk.Responses;

/// <summary>A subscription plan on sale and its prices.</summary>
public sealed record SubscriptionPlan
{
    /// <summary>
    /// The plan key, such as <c>unlimited_energy</c>. Pass it as <see cref="StartSubscriptionRequest.SubscriptionId"/>.
    /// </summary>
    public string SubscriptionId { get; init; } = "";

    /// <summary>The plan's numeric ID.</summary>
    public long Id { get; init; }

    /// <summary>The plan name.</summary>
    public string Name { get; init; } = "";

    /// <summary>The one-time fee charged when a subscription starts, in TRX.</summary>
    public decimal ActivationFee { get; init; }

    /// <summary>The amount charged when a subscription starts, in TRX.</summary>
    public decimal InitialPrice { get; init; }

    /// <summary>The price of each transaction the subscription serves, in TRX.</summary>
    public decimal Price { get; init; }

    /// <summary>How many transactions the plan covers, 0 for no limit.</summary>
    public long TransactionsLimit { get; init; }

    /// <summary>How many days the plan runs, 0 for no time limit.</summary>
    public int DurationDays { get; init; }
}
