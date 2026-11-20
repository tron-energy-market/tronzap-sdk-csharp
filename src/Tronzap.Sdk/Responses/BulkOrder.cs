using System.Collections.Generic;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>A started bulk order and its progress.</summary>
/// <remarks>
/// Every accepted address ends up delivered, failed, in review or skipped; until then it is pending, and
/// <see cref="Processed"/> counts the addresses with a final result.
/// </remarks>
public sealed record BulkOrder
{
    /// <summary>The bulk order ID assigned by TronZap.</summary>
    public string Id { get; init; } = "";

    /// <summary>The ID you passed when starting the order, or <see langword="null"/>.</summary>
    public string? ExternalId { get; init; }

    /// <summary>The current state of the order.</summary>
    public BulkStatus Status { get; init; }

    /// <summary>The resource ordered, always <c>energy</c>.</summary>
    public string Resource { get; init; } = "";

    /// <summary>The rental duration in hours.</summary>
    public int Duration { get; init; }

    /// <summary>Whether price increases are allowed.</summary>
    public bool Force { get; init; }

    /// <summary>The allowed price increase in percent, 0 without <see cref="Force"/>.</summary>
    public int MaxPriceIncrease { get; init; }

    /// <summary>The URL called when the order becomes final, or <see langword="null"/>.</summary>
    public string? WebhookUrl { get; init; }

    /// <summary>The number of accepted addresses.</summary>
    public int Total { get; init; }

    /// <summary>The number of addresses with a final result.</summary>
    public int Processed { get; init; }

    /// <summary>The number of addresses energy was delivered to.</summary>
    public int Delivered { get; init; }

    /// <summary>The number of addresses not delivered and refunded.</summary>
    public int Failed { get; init; }

    /// <summary>The number of addresses support is checking.</summary>
    public int InReview { get; init; }

    /// <summary>The number of addresses not bought and not charged.</summary>
    public int Skipped { get; init; }

    /// <summary>
    /// The skipped addresses counted by reason, such as <c>address_not_activated</c> or <c>price_increased</c>.
    /// </summary>
    public IReadOnlyDictionary<string, int> SkippedByReason { get; init; } = new Dictionary<string, int>();

    /// <summary>The number of rows the calculation rejected.</summary>
    public int RejectedCount { get; init; }

    /// <summary>The estimated total from the calculation, in TRX.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// The amount charged so far, in TRX. It may decrease while addresses in review are refunded.
    /// </summary>
    public decimal ChargedAmount { get; init; }

    /// <summary>When the calculation was created, or <see langword="null"/> when the API did not report it.</summary>
    public Timestamp? CreatedAt { get; init; }

    /// <summary>When the order started, or <see langword="null"/> when the API did not report it.</summary>
    public Timestamp? StartedAt { get; init; }

    /// <summary>When the order became final, or <see langword="null"/> while it is processing.</summary>
    public Timestamp? FinalizedAt { get; init; }
}
