using System.Collections.Generic;

namespace Tronzap.Sdk.Responses;

/// <summary>One page of your bulk orders, newest first.</summary>
public sealed record BulkHistory
{
    /// <summary>The page number, starting at 1.</summary>
    public int Page { get; init; }

    /// <summary>The page size.</summary>
    public int PerPage { get; init; }

    /// <summary>The number of orders across all pages.</summary>
    public int Total { get; init; }

    /// <summary>The orders on this page.</summary>
    public IReadOnlyList<BulkOrder> Items { get; init; } = [];
}
