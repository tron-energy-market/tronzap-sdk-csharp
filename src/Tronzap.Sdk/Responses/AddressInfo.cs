using System.Collections.Generic;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>The on-chain resources and token balances of a TRON address.</summary>
public sealed record AddressInfo
{
    /// <summary>Available energy and bandwidth.</summary>
    public AddressResources Resources { get; init; } = new();

    /// <summary>Token balances by token symbol, for example <c>TRX</c> and <c>USDT</c>.</summary>
    public IReadOnlyDictionary<string, decimal> Balances { get; init; } = new Dictionary<string, decimal>();
}
