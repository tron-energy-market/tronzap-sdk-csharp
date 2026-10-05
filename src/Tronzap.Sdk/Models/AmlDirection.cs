namespace Tronzap.Sdk.Models;

/// <summary>The direction of a screened transaction.</summary>
public enum AmlDirection
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>Incoming funds (<c>deposit</c>).</summary>
    Deposit,

    /// <summary>Outgoing funds (<c>withdrawal</c>).</summary>
    Withdrawal,
}
