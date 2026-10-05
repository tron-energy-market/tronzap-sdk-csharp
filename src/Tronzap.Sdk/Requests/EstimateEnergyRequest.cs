using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>A token transfer to estimate the energy for.</summary>
public sealed record EstimateEnergyRequest
{
    /// <summary>The USDT (TRC20) contract address, used when <see cref="ContractAddress"/> is not set.</summary>
    public const string UsdtContractAddress = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";

    /// <summary>The sender's TRON address.</summary>
    public required string FromAddress { get; init; }

    /// <summary>The recipient's TRON address.</summary>
    public required string ToAddress { get; init; }

    /// <summary>The token contract address. When not set the API estimates a USDT (TRC20) transfer.</summary>
    public string? ContractAddress { get; init; }

    internal void Validate()
    {
        Guard.Required(FromAddress, nameof(FromAddress));
        Guard.Required(ToAddress, nameof(ToAddress));
        Guard.Optional(ContractAddress, nameof(ContractAddress));
    }
}
