using System.Collections.Generic;

namespace Tronzap.Sdk.Responses;

/// <summary>One page of your subscriptions, newest first.</summary>
public sealed record SubscriptionHistory
{
    /// <summary>The page number, starting at 1.</summary>
    public int Page { get; init; }

    /// <summary>The page size.</summary>
    public int PerPage { get; init; }

    /// <summary>The number of subscriptions across all pages.</summary>
    public int Total { get; init; }

    /// <summary>The subscriptions on this page.</summary>
    public IReadOnlyList<Subscription> Items { get; init; } = [];
}
