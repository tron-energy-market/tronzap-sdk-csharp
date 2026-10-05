using System.Collections.Generic;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>The address to pay for a direct energy recharge and the rates energy is delivered at.</summary>
public sealed record DirectRechargeInfo
{
    /// <summary>The TRON address to send TRX to.</summary>
    public string Address { get; init; } = "";

    /// <summary>The rates energy is delivered at.</summary>
    public IReadOnlyList<DirectRechargeRate> Rates { get; init; } = [];
}
