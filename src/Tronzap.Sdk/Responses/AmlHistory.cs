using System.Collections.Generic;

namespace Tronzap.Sdk.Responses;

/// <summary>One page of past AML checks, newest first.</summary>
public sealed record AmlHistory
{
    /// <summary>The page number, starting at 1.</summary>
    public int Page { get; init; }

    /// <summary>The page size.</summary>
    public int PerPage { get; init; }

    /// <summary>The number of checks across all pages.</summary>
    public int Total { get; init; }

    /// <summary>The checks on this page.</summary>
    public IReadOnlyList<AmlCheck> Items { get; init; } = [];
}
