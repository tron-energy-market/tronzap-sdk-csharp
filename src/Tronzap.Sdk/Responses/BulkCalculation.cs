using System.Collections.Generic;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>A priced bulk order that was not started yet. Start it with its <see cref="Id"/> before it expires.</summary>
public sealed record BulkCalculation
{
    /// <summary>The ID to start the order with.</summary>
    public string Id { get; init; } = "";

    /// <summary>Always <see cref="BulkStatus.Draft"/>.</summary>
    public BulkStatus Status { get; init; }

    /// <summary>The resource ordered, always <c>energy</c>.</summary>
    public string Resource { get; init; } = "";

    /// <summary>The rental duration in hours.</summary>
    public int Duration { get; init; }

    /// <summary>The estimated total of the accepted rows, in TRX.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>The number of accepted rows.</summary>
    public int Accepted { get; init; }

    /// <summary>The number of rejected rows.</summary>
    public int RejectedCount { get; init; }

    /// <summary>The rejected rows with their row numbers and reasons.</summary>
    public IReadOnlyList<BulkRejectedRow> Rejected { get; init; } = [];

    /// <summary>When the calculation expires, or <see langword="null"/> when the API did not report it.</summary>
    public Timestamp? ExpiresAt { get; init; }
}
