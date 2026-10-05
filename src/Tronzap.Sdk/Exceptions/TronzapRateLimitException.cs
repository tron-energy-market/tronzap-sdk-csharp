using System;
using System.Net;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The API answered with HTTP 429: too many requests. Back off and retry.</summary>
public class TronzapRateLimitException : TronzapHttpException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    /// <param name="retryAfter">How long the API asked to wait, or <see langword="null"/>.</param>
    public TronzapRateLimitException(string message, HttpStatusCode statusCode, string responseBody, TimeSpan? retryAfter)
        : base(message, statusCode, responseBody)
    {
        RetryAfter = retryAfter;
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

    /// <summary>How long the API asked to wait before retrying, from the <c>Retry-After</c> header, or <see langword="null"/>.</summary>
    public TimeSpan? RetryAfter { get; }
}
