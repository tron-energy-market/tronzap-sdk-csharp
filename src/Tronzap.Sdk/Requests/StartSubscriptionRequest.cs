using Tronzap.Sdk.Internal;
using Tronzap.Sdk.Responses;

namespace Tronzap.Sdk.Requests;

/// <summary>A subscription to start for an address. Starting one charges the plan's initial price.</summary>
public sealed record StartSubscriptionRequest
{
    /// <summary>
    /// The plan to subscribe to: the <see cref="SubscriptionPlan.SubscriptionId"/> that
    /// <see cref="ITronzapClient.GetSubscriptionsAsync"/> returns, such as <c>unlimited_energy</c>, not the plan's
    /// numeric <see cref="SubscriptionPlan.Id"/>.
    /// </summary>
    public required string SubscriptionId { get; init; }

    /// <summary>The TRON address the subscription serves.</summary>
    public required string Address { get; init; }

    /// <summary>How many days the subscription runs, 0 for no time limit. Defaults to 0.</summary>
    public int DurationDays { get; init; }

    /// <summary>How many transactions the subscription covers, 0 for no limit. Defaults to 0.</summary>
    public long TransactionsLimit { get; init; }

    /// <summary>Your own ID for the subscription, to look it up with <see cref="SubscriptionRequest.ByExternalId"/>.</summary>
    public string? ExternalId { get; init; }

    /// <summary>Whether to activate the address if it is not active yet.</summary>
    public bool ActivateAddress { get; init; }

    internal void Validate()
    {
        Guard.Required(SubscriptionId, nameof(SubscriptionId));
        Guard.Required(Address, nameof(Address));
        Guard.NonNegative(DurationDays, nameof(DurationDays));
        Guard.NonNegative(TransactionsLimit, nameof(TransactionsLimit));
        Guard.Optional(ExternalId, nameof(ExternalId));
    }
}
