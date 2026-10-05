namespace Tronzap.Sdk.Models;

/// <summary>The risk level an AML check assigned.</summary>
public enum AmlRiskLevel
{
    /// <summary>A value this SDK version does not recognise.</summary>
    Unknown = 0,

    /// <summary>Low risk.</summary>
    Low,

    /// <summary>Medium risk.</summary>
    Medium,

    /// <summary>High risk.</summary>
    High,
}
