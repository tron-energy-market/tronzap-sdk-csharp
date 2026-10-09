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

    /// <summary>
    /// For an address check, the address to screen; for a hash check, the recipient address of the transaction,
    /// where the funds were received.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>The transaction hash. Required when <see cref="Type"/> is <see cref="AmlCheckType.Hash"/>.</summary>
    public string? Hash { get; init; }

    /// <summary>
    /// For a hash check, which side of the transaction you are on: <see cref="AmlDirection.Deposit"/> if the funds
    /// were sent to your address (<see cref="Address"/> is yours), <see cref="AmlDirection.Withdrawal"/> if you sent
    /// them (<see cref="Address"/> is the external recipient's). The risk is scored for the counterparty. When
    /// <see langword="null"/> on a hash check, the SDK sends <c>deposit</c>.
    /// </summary>
    public AmlDirection? Direction { get; init; }

    /// <summary>Screens a wallet address.</summary>
    /// <param name="network">The network code, for example <c>TRX</c>.</param>
    /// <param name="address">The address to screen.</param>
    /// <returns>The request.</returns>
    public static AmlCheckRequest ForAddress(string network, string address) =>
        new() { Type = AmlCheckType.Address, Network = network, Address = address };

    /// <summary>
    /// Screens a transaction. The risk is scored for the counterparty: the sender of a deposit, the recipient of a
    /// withdrawal.
    /// </summary>
    /// <param name="network">The network code, for example <c>BTC</c>.</param>
    /// <param name="address">The recipient address of the transaction, where the funds were received.</param>
    /// <param name="hash">The transaction hash.</param>
    /// <param name="direction">
    /// <see cref="AmlDirection.Deposit"/> if the funds were sent to your address (<paramref name="address"/> is
    /// yours), <see cref="AmlDirection.Withdrawal"/> if you sent them (<paramref name="address"/> is the external
    /// recipient's). When <see langword="null"/>, the SDK sends <c>deposit</c>.
    /// </param>
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
