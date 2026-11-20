using System;
using Tronzap.Sdk.Internal;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Requests;

/// <summary>The bulk order whose report to download, by its TronZap ID, your external ID, or both.</summary>
public sealed record BulkReportRequest
{
    /// <summary>The bulk order ID assigned by TronZap.</summary>
    public string? Id { get; init; }

    /// <summary>The external ID you passed when starting the order.</summary>
    public string? ExternalId { get; init; }

    /// <summary>Returns only the rows with this result, or every row when <see langword="null"/>.</summary>
    public BulkRowStatus? Status { get; init; }

    /// <summary>Downloads the report of a bulk order by its TronZap ID.</summary>
    /// <param name="id">The bulk order ID.</param>
    /// <returns>The request.</returns>
    public static BulkReportRequest ById(string id) => new() { Id = id };

    /// <summary>Downloads the report of a bulk order by the external ID you passed when starting it.</summary>
    /// <param name="externalId">The external ID.</param>
    /// <returns>The request.</returns>
    public static BulkReportRequest ByExternalId(string externalId) => new() { ExternalId = externalId };

    internal void Validate()
    {
        Guard.Optional(Id, nameof(Id));
        Guard.Optional(ExternalId, nameof(ExternalId));
        if (Id is null && ExternalId is null)
        {
            throw new ArgumentException("Either Id or ExternalId is required.");
        }

        if (Status is { } status)
        {
            Guard.Defined(status, nameof(Status));
        }
    }
}
