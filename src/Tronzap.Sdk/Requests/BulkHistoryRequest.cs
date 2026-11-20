using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>A page of bulk orders to return.</summary>
public sealed record BulkHistoryRequest
{
    /// <summary>The page size used when <see cref="PerPage"/> is not set.</summary>
    public const int DefaultPerPage = 10;

    /// <summary>The page number, starting at 1. Defaults to 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>The page size; the API accepts at most 50. Defaults to <see cref="DefaultPerPage"/>.</summary>
    public int PerPage { get; init; } = DefaultPerPage;

    internal void Validate()
    {
        Guard.Positive(Page, nameof(Page));
        Guard.Positive(PerPage, nameof(PerPage));
    }
}
