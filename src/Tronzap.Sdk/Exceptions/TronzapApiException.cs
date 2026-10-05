using System;
using System.Net;

namespace Tronzap.Sdk.Exceptions;

/// <summary>
/// The API rejected the request with a non-zero error code. The API reports some errors with a 2xx status and others
/// with 4xx or 5xx, so this exception takes precedence over <see cref="TronzapHttpException"/> whenever the response
/// carries an error code.
/// </summary>
public class TronzapApiException : TronzapException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message from the API.</param>
    /// <param name="code">The numeric error code.</param>
    /// <param name="errorKey">The error key, or <see langword="null"/>.</param>
    /// <param name="requestId">The request ID, or <see langword="null"/>.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    public TronzapApiException(
        string message,
        int code,
        string? errorKey,
        string? requestId,
        HttpStatusCode statusCode,
        string responseBody)
        : base(message)
    {
        Code = code;
        ErrorKey = errorKey;
        RequestId = requestId;
        StatusCode = statusCode;
        ResponseBody = responseBody ?? "";
    }

    /// <summary>Creates an exception.</summary>
    public TronzapApiException()
        : this("Unknown API error", 1, null, null, default, "")
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapApiException(string message)
        : this(message, 1, null, null, default, "")
    {
    }

    /// <summary>Creates an exception with a message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapApiException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Code = 1;
        ResponseBody = "";
    }

    /// <summary>The numeric error code the API returned.</summary>
    public int Code { get; }

    /// <summary>The error code as a <see cref="TronzapErrorCode"/>, or <see cref="TronzapErrorCode.Unknown"/>.</summary>
    public TronzapErrorCode ErrorCode =>
        Code != 0 && Enum.IsDefined((TronzapErrorCode)Code) ? (TronzapErrorCode)Code : TronzapErrorCode.Unknown;

    /// <summary>
    /// The error key, for example <c>invalid_tron_address</c> or <c>invalid_tron_address.from_address</c>, or
    /// <see langword="null"/> when the API did not send one.
    /// </summary>
    public string? ErrorKey { get; }

    /// <summary>The ID the API assigned to the request, or <see langword="null"/>. Quote it when contacting support.</summary>
    public string? RequestId { get; }

    /// <summary>The HTTP status of the response.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The raw response body.</summary>
    public string ResponseBody { get; }
}
