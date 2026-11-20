using System;
using System.Net;
using System.Text.Json;
using Tronzap.Sdk.Responses;

namespace Tronzap.Sdk.Exceptions;

/// <summary>
/// The price changed since the bulk calculation, so the order was not started:
/// <see cref="TronzapErrorCode.BulkPriceChanged"/>. Confirm the new total of <see cref="Calculation"/>, then start the
/// order with its ID.
/// </summary>
public class TronzapBulkPriceChangedException : TronzapApiException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message from the API.</param>
    /// <param name="errorKey">The error key, or <see langword="null"/>.</param>
    /// <param name="requestId">The request ID, or <see langword="null"/>.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    /// <param name="result">The <c>result</c> field sent with the error.</param>
    /// <param name="retryAfter">How long the API asked to wait, or <see langword="null"/>.</param>
    /// <param name="calculation">The new calculation.</param>
    public TronzapBulkPriceChangedException(
        string message,
        string? errorKey,
        string? requestId,
        HttpStatusCode statusCode,
        string responseBody,
        JsonElement? result,
        TimeSpan? retryAfter,
        BulkCalculation calculation)
        : base(message, (int)TronzapErrorCode.BulkPriceChanged, errorKey, requestId, statusCode, responseBody, result, retryAfter)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        Calculation = calculation;
    }

    /// <summary>Creates an exception.</summary>
    public TronzapBulkPriceChangedException()
        : this("The price changed since the bulk calculation.")
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapBulkPriceChangedException(string message)
        : this(message, null, null, default, "", null, null, new BulkCalculation())
    {
    }

    /// <summary>Creates an exception with a message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapBulkPriceChangedException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Calculation = new BulkCalculation();
    }

    /// <summary>The new calculation at the current prices.</summary>
    public BulkCalculation Calculation { get; }
}
