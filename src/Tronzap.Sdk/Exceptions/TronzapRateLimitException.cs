using System;
using System.Net;

namespace Tronzap.Sdk.Exceptions;

/// <summary>
/// HTTP 429 without an API payload, such as one from a proxy: too many requests. Back off and retry.
/// </summary>
/// <remarks>
/// A rate limit reported by the API itself arrives as a <see cref="TronzapApiException"/> with
/// <see cref="TronzapErrorCode.RateLimited"/>. Both carry the wait in <c>RetryAfter</c>.
/// </remarks>
public class TronzapRateLimitException : TronzapHttpException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    /// <param name="retryAfter">How long the API asked to wait, or <see langword="null"/>.</param>
    public TronzapRateLimitException(string message, HttpStatusCode statusCode, string responseBody, TimeSpan? retryAfter)
        : base(message, statusCode, responseBody, retryAfter)
    {
    }

    /// <summary>Creates an exception.</summary>
    public TronzapRateLimitException()
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapRateLimitException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapRateLimitException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
