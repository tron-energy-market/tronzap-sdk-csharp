using System.Collections.Generic;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>The resources on sale and their current prices.</summary>
public sealed record ServiceRates
{
    /// <summary>Energy price tiers. Energy is priced per 1000 units.</summary>
    public IReadOnlyList<EnergyRate> Energy { get; init; } = [];

    /// <summary>Bandwidth price tiers. Bandwidth is priced per 1000 units.</summary>
    public IReadOnlyList<BandwidthRate> Bandwidth { get; init; } = [];

    /// <summary>The address activation price, or <see langword="null"/> when the API did not report one.</summary>
    public ActivateAddressRate? ActivateAddress { get; init; }
}
