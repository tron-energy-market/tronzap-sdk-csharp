using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Responses;

/// <summary>An AML screening product and its price.</summary>
public sealed record AmlService
{
    /// <summary>The service ID.</summary>
    public string Id { get; init; } = "";

    /// <summary>What the service screens.</summary>
    public AmlCheckType Type { get; init; }

    /// <summary>Price of one check, in TRX.</summary>
    public decimal Price { get; init; }
}
