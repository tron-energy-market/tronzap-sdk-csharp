using System;
using System.Net;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The API answered with an HTTP 5xx status. Usually transient.</summary>
public class TronzapServerException : TronzapHttpException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    public TronzapServerException(string message, HttpStatusCode statusCode, string responseBody)
        : base(message, statusCode, responseBody)
    {
    }

    /// <summary>Creates an exception.</summary>
    public TronzapServerException()
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapServerException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapServerException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
