using Tronzap.Sdk.Internal;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Requests;

/// <summary>A page of subscription history to return.</summary>
public sealed record SubscriptionHistoryRequest
{
    /// <summary>The page size used when <see cref="PerPage"/> is not set.</summary>
    public const int DefaultPerPage = 10;

    /// <summary>The page number, starting at 1. Defaults to 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>The page size, at most 50. Defaults to <see cref="DefaultPerPage"/>.</summary>
    public int PerPage { get; init; } = DefaultPerPage;

    /// <summary>Returns only subscriptions in this state, or all subscriptions when <see langword="null"/>.</summary>
    public SubscriptionStatus? Status { get; init; }

    internal void Validate()
    {
        Guard.Positive(Page, nameof(Page));
        Guard.Positive(PerPage, nameof(PerPage));
        if (Status is { } status)
        {
            Guard.Defined(status, nameof(Status));
        }
    }
}
