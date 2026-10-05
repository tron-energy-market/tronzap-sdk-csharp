using System;
using System.Net;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The API answered with HTTP 401 or 403: check the API token and secret.</summary>
public class TronzapUnauthorizedException : TronzapHttpException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    public TronzapUnauthorizedException(string message, HttpStatusCode statusCode, string responseBody)
        : base(message, statusCode, responseBody)
    {
    }

    /// <summary>Creates an exception.</summary>
    public TronzapUnauthorizedException()
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapUnauthorizedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapUnauthorizedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
