using System;
using System.Collections.Generic;
using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>The rows of a bulk energy order to validate and price.</summary>
public sealed record BulkCalculateRequest
{
    /// <summary>The rows, at most 10 000.</summary>
    public required IReadOnlyList<BulkItem> Items { get; init; }

    /// <summary>The rental duration in hours, one of the durations the services list offers. Defaults to 1.</summary>
    public int Duration { get; init; } = 1;

    internal void Validate()
    {
        if (Items is null || Items.Count == 0)
        {
            throw new ArgumentException("Items is required.", nameof(Items));
        }

        foreach (BulkItem item in Items)
        {
            if (item is null)
            {
                throw new ArgumentException("Items must not contain null.", nameof(Items));
            }

            if (item.Address is null)
            {
                throw new ArgumentException("Every item needs an Address.", nameof(Items));
            }
        }

        Guard.Positive(Duration, nameof(Duration));
    }
}
