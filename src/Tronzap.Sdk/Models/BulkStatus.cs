namespace Tronzap.Sdk.Models;

/// <summary>
/// The state of a bulk order. A calculation is a draft; a started order moves from processing to one of the final
/// states: completed, partial or failed.
/// </summary>
public enum BulkStatus
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>A calculation that was not started.</summary>
    Draft,

    /// <summary>The addresses are being processed.</summary>
    Processing,

    /// <summary>Energy was delivered to every address.</summary>
    Completed,

    /// <summary>Energy was delivered to some addresses.</summary>
    Partial,

    /// <summary>Energy was delivered to no address.</summary>
    Failed,
}
