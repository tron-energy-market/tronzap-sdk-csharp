using System;
using Tronzap.Sdk.Internal;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Requests;

/// <summary>An address or a transaction hash to screen.</summary>
public sealed record AmlCheckRequest
{
    /// <summary>What to screen.</summary>
    public required AmlCheckType Type { get; init; }

    /// <summary>The network code, for example <c>TRX</c>, <c>BTC</c> or <c>ETH</c>.</summary>
    public required string Network { get; init; }

    /// <summary>The wallet address.</summary>
    public required string Address { get; init; }

    /// <summary>The transaction hash. Required when <see cref="Type"/> is <see cref="AmlCheckType.Hash"/>.</summary>
    public string? Hash { get; init; }

    /// <summary>The transaction direction, for a hash check.</summary>
    public AmlDirection? Direction { get; init; }

    /// <summary>Screens a wallet address.</summary>
    /// <param name="network">The network code, for example <c>TRX</c>.</param>
    /// <param name="address">The address to screen.</param>
    /// <returns>The request.</returns>
    public static AmlCheckRequest ForAddress(string network, string address) =>
        new() { Type = AmlCheckType.Address, Network = network, Address = address };

    /// <summary>Screens a transaction.</summary>
    /// <param name="network">The network code, for example <c>BTC</c>.</param>
    /// <param name="address">The wallet address.</param>
    /// <param name="hash">The transaction hash.</param>
    /// <param name="direction">The transaction direction, or <see langword="null"/>.</param>
    /// <returns>The request.</returns>
    public static AmlCheckRequest ForHash(string network, string address, string hash, AmlDirection? direction = null) =>
        new() { Type = AmlCheckType.Hash, Network = network, Address = address, Hash = hash, Direction = direction };

    internal void Validate()
    {
        Guard.Defined(Type, nameof(Type));
        Guard.Required(Network, nameof(Network));
        Guard.Required(Address, nameof(Address));
        Guard.Optional(Hash, nameof(Hash));
        if (Direction is { } direction)
        {
            Guard.Defined(direction, nameof(Direction));
        }

        if (Type == AmlCheckType.Hash && Hash is null)
        {
            throw new ArgumentException("Hash is required for a hash check.", nameof(Hash));
        }
    }
}
