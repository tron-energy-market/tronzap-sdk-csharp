namespace Tronzap.Sdk.Models;

/// <summary>The result of one address of a bulk order: the <c>status</c> column of the bulk report.</summary>
public enum BulkRowStatus
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>Energy was delivered.</summary>
    Delivered,

    /// <summary>Energy was not delivered and the charge was refunded.</summary>
    Failed,

    /// <summary>Support is checking the address.</summary>
    InReview,

    /// <summary>The address was not bought and not charged.</summary>
    Skipped,

    /// <summary>The address was not processed yet.</summary>
    Pending,
}
