using System;
using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>The bulk order to look up, by its TronZap ID, your external ID, or both.</summary>
public sealed record BulkOrderRequest
{
    /// <summary>The bulk order ID assigned by TronZap.</summary>
    public string? Id { get; init; }

    /// <summary>The external ID you passed when starting the order.</summary>
    public string? ExternalId { get; init; }

    /// <summary>Looks a bulk order up by its TronZap ID.</summary>
    /// <param name="id">The bulk order ID.</param>
    /// <returns>The request.</returns>
    public static BulkOrderRequest ById(string id) => new() { Id = id };

    /// <summary>Looks a bulk order up by the external ID you passed when starting it.</summary>
    /// <param name="externalId">The external ID.</param>
    /// <returns>The request.</returns>
    public static BulkOrderRequest ByExternalId(string externalId) => new() { ExternalId = externalId };

    internal void Validate()
    {
        Guard.Optional(Id, nameof(Id));
        Guard.Optional(ExternalId, nameof(ExternalId));
        if (Id is null && ExternalId is null)
        {
            throw new ArgumentException("Either Id or ExternalId is required.");
        }
    }
}
