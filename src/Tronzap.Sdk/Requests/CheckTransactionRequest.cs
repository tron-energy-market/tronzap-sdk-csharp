using System;
using Tronzap.Sdk.Internal;

namespace Tronzap.Sdk.Requests;

/// <summary>The transaction to look up, by its TronZap ID, your external ID, or both.</summary>
public sealed record CheckTransactionRequest
{
    /// <summary>The transaction ID assigned by TronZap.</summary>
    public string? Id { get; init; }

    /// <summary>The external ID you passed when creating the transaction.</summary>
    public string? ExternalId { get; init; }

    /// <summary>Looks a transaction up by its TronZap ID.</summary>
    /// <param name="id">The transaction ID.</param>
    /// <returns>The request.</returns>
    public static CheckTransactionRequest ById(string id) => new() { Id = id };

    /// <summary>Looks a transaction up by the external ID you passed when creating it.</summary>
    /// <param name="externalId">The external ID.</param>
    /// <returns>The request.</returns>
    public static CheckTransactionRequest ByExternalId(string externalId) => new() { ExternalId = externalId };

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
