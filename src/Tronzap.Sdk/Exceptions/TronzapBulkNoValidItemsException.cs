using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Tronzap.Sdk.Models;

namespace Tronzap.Sdk.Exceptions;

/// <summary>
/// No row of a bulk order is valid: <see cref="TronzapErrorCode.BulkNoValidItems"/>. <see cref="Rejected"/> says why
/// each row was rejected.
/// </summary>
public class TronzapBulkNoValidItemsException : TronzapApiException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message from the API.</param>
    /// <param name="errorKey">The error key, or <see langword="null"/>.</param>
    /// <param name="requestId">The request ID, or <see langword="null"/>.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    /// <param name="result">The <c>result</c> field sent with the error.</param>
    /// <param name="retryAfter">How long the API asked to wait, or <see langword="null"/>.</param>
    /// <param name="rejected">The rejected rows.</param>
    public TronzapBulkNoValidItemsException(
        string message,
        string? errorKey,
        string? requestId,
        HttpStatusCode statusCode,
        string responseBody,
        JsonElement? result,
        TimeSpan? retryAfter,
        IReadOnlyList<BulkRejectedRow> rejected)
        : base(message, (int)TronzapErrorCode.BulkNoValidItems, errorKey, requestId, statusCode, responseBody, result, retryAfter)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        Rejected = rejected;
    }

    /// <summary>Creates an exception.</summary>
    public TronzapBulkNoValidItemsException()
        : this("No row of the bulk order is valid.")
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapBulkNoValidItemsException(string message)
        : this(message, null, null, default, "", null, null, [])
    {
    }

    /// <summary>Creates an exception with a message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapBulkNoValidItemsException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Rejected = [];
    }

    /// <summary>The rejected rows with their row numbers and reasons.</summary>
    public IReadOnlyList<BulkRejectedRow> Rejected { get; }
}
