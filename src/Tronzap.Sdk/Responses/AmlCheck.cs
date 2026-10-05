using System.Collections.Generic;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>An AML screening of an address or a transaction hash.</summary>
public sealed record AmlCheck
{
    /// <summary>The AML check ID.</summary>
    public string Id { get; init; } = "";

    /// <summary>What is screened.</summary>
    public AmlCheckType Type { get; init; }

    /// <summary>The screened wallet address.</summary>
    public string Address { get; init; } = "";

    /// <summary>The screened transaction hash, or <see langword="null"/> for an address check.</summary>
    public string? Hash { get; init; }

    /// <summary>The direction of the screened transaction, or <see langword="null"/> when not given.</summary>
    public AmlDirection? Direction { get; init; }

    /// <summary>The network code, for example <c>TRX</c>, <c>BTC</c> or <c>ETH</c>.</summary>
    public string Network { get; init; } = "";

    /// <summary>The current state of the check.</summary>
    public AmlStatus Status { get; init; }

    /// <summary>
    /// The risk score, or <see langword="null"/> until the check completes. A completed check can have a score of 0,
    /// which is not the same as having no score yet.
    /// </summary>
    public decimal? RiskScore { get; init; }

    /// <summary>The risk level, or <see langword="null"/> until the check completes.</summary>
    public AmlRiskLevel? RiskLevel { get; init; }

    /// <summary>Whether the subject is blacklisted.</summary>
    public bool Blacklist { get; init; }

    /// <summary>The factors that contributed to the risk score.</summary>
    public IReadOnlyList<AmlRiskFactor> RiskFactors { get; init; } = [];

    /// <summary>When the check completed, or <see langword="null"/> when the API did not report it.</summary>
    public Timestamp? CheckedAt { get; init; }
}
