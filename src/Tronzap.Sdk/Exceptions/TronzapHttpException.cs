using System;
using System.Net;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The API answered with a non-2xx status and no error code in the body.</summary>
public class TronzapHttpException : TronzapException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    public TronzapHttpException(string message, HttpStatusCode statusCode, string responseBody)
        : this(message, statusCode, responseBody, null)
    {
    }

    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    /// <param name="retryAfter">How long the server asked to wait, or <see langword="null"/>.</param>
    public TronzapHttpException(string message, HttpStatusCode statusCode, string responseBody, TimeSpan? retryAfter)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody ?? "";
        RetryAfter = retryAfter;
    }

    /// <summary>Creates an exception.</summary>
    public TronzapHttpException()
    {
        ResponseBody = "";
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapHttpException(string message)
        : base(message)
    {
        ResponseBody = "";
    }

    /// <summary>Creates an exception with a message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapHttpException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ResponseBody = "";
    }

    /// <summary>The HTTP status of the response.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The raw response body.</summary>
    public string ResponseBody { get; }

    /// <summary>
    /// How long to wait before retrying, from the <c>Retry-After</c> header (seconds or an HTTP date) or the
    /// <c>retry_after</c> field, or <see langword="null"/> when the server sent neither.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
