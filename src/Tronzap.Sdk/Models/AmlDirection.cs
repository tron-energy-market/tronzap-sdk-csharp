namespace Tronzap.Sdk.Models;

/// <summary>Which side of a screened transaction you are on. The risk is scored for the counterparty.</summary>
public enum AmlDirection
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>
    /// The funds were sent to your address: the screened address is yours and the sender is scored (<c>deposit</c>).
    /// </summary>
    Deposit,

    /// <summary>
    /// You sent the funds: the screened address is the external recipient's and the recipient is scored
    /// (<c>withdrawal</c>).
    /// </summary>
    Withdrawal,
}
