namespace Tronzap.Sdk.Models;

/// <summary>One factor that contributed to an AML risk score.</summary>
public sealed record AmlRiskFactor
{
    /// <summary>Machine-readable factor name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Human-readable factor label.</summary>
    public string Label { get; init; } = "";

    /// <summary>The group the factor belongs to.</summary>
    public string Group { get; init; } = "";

    /// <summary>The factor's share of the risk score.</summary>
    public decimal Score { get; init; }
}
