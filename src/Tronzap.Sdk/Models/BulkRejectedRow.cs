namespace Tronzap.Sdk.Models;

/// <summary>A row of a bulk order that the calculation did not accept.</summary>
public sealed record BulkRejectedRow
{
    /// <summary>The row's position in the request items, starting at 1.</summary>
    public int Row { get; init; }

    /// <summary>The address as sent.</summary>
    public string Address { get; init; } = "";

    /// <summary>Why the row was rejected, such as <c>invalid_address</c> or <c>duplicate</c>.</summary>
    public string Reason { get; init; } = "";
}
