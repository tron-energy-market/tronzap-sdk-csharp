using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>A subscription that keeps an address supplied with energy.</summary>
/// <remarks>
/// <see cref="ITronzapClient.StartSubscriptionAsync"/>, <see cref="ITronzapClient.CheckSubscriptionAsync"/> and
/// <see cref="ITronzapClient.StopSubscriptionAsync"/> return the subscription with its <see cref="Params"/>; the items of
/// <see cref="SubscriptionHistory"/> carry the usage counters <see cref="TransactionsUsed"/>, <see cref="EnergyUsed"/>
/// and <see cref="TotalPrice"/> instead. A value the response does not carry is 0, empty or <see langword="null"/>.
/// </remarks>
public sealed record Subscription
{
    /// <summary>The subscription ID assigned by TronZap.</summary>
    public string Id { get; init; } = "";

    /// <summary>The plan the subscription belongs to, such as <c>unlimited_energy</c>.</summary>
    public string SubscriptionId { get; init; } = "";

    /// <summary>The ID you passed when starting the subscription, or <see langword="null"/>.</summary>
    public string? ExternalId { get; init; }

    /// <summary>The TRON address the subscription serves, or empty when the response does not report it.</summary>
    public string Address { get; init; } = "";

    /// <summary>The current state of the subscription.</summary>
    public SubscriptionStatus Status { get; init; }

    /// <summary>The parameters the subscription was started with, or <see langword="null"/> in the history.</summary>
    public SubscriptionParams? Params { get; init; }

    /// <summary>How many transactions the subscription covers, 0 for no limit.</summary>
    public long TransactionsLimit { get; init; }

    /// <summary>How many transactions the subscription has served.</summary>
    public long TransactionsUsed { get; init; }

    /// <summary>How much energy the subscription has delegated.</summary>
    public long EnergyUsed { get; init; }

    /// <summary>The amount charged for the subscription so far, in TRX.</summary>
    public decimal TotalPrice { get; init; }

    /// <summary>When the subscription was created, or <see langword="null"/> when the API did not report it.</summary>
    public Timestamp? CreatedAt { get; init; }

    /// <summary>When the subscription started, or <see langword="null"/> when the API did not report it.</summary>
    public Timestamp? StartedAt { get; init; }

    /// <summary>When the subscription was last renewed, or <see langword="null"/>.</summary>
    public Timestamp? RenewedAt { get; init; }

    /// <summary>When the subscription was stopped, or <see langword="null"/> if it was not.</summary>
    public Timestamp? StoppedAt { get; init; }

    /// <summary>When the subscription ends, or <see langword="null"/> when it has no time limit or the API did not report it.</summary>
    public Timestamp? ExpireAt { get; init; }
}
