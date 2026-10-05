namespace Tronzap.Sdk.Models;

/// <summary>What an AML check screens.</summary>
public enum AmlCheckType
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>A wallet address (<c>address</c>).</summary>
    Address,

    /// <summary>A transaction hash (<c>hash</c>).</summary>
    Hash,
}
