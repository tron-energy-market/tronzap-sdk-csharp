namespace Tronzap.Sdk.Models;

/// <summary>The state of a transaction: <see cref="New"/> → <see cref="Pending"/> → <see cref="Success"/> or <see cref="Failed"/>.</summary>
public enum TransactionStatus
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>The transaction was created and is waiting to be processed.</summary>
    New,

    /// <summary>The transaction is being processed.</summary>
    Pending,

    /// <summary>The resources were delivered.</summary>
    Success,

    /// <summary>The transaction failed.</summary>
    Failed,
}
