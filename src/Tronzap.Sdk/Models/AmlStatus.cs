namespace Tronzap.Sdk.Models;

/// <summary>The state of an AML check.</summary>
public enum AmlStatus
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>The check is queued.</summary>
    Pending,

    /// <summary>The check is running.</summary>
    Processing,

    /// <summary>The check finished and its result is available.</summary>
    Completed,

    /// <summary>The check failed.</summary>
    Failed,
}
