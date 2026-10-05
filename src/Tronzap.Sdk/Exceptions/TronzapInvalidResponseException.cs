using System;
using System.Net;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The API answered with a 2xx status, but the SDK could not read the response.</summary>
public class TronzapInvalidResponseException : TronzapException
{
    /// <summary>Creates an exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status of the response.</param>
    /// <param name="responseBody">The raw response body.</param>
    /// <param name="innerException">The cause, or <see langword="null"/>.</param>
    public TronzapInvalidResponseException(string message, HttpStatusCode statusCode, string responseBody, Exception? innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody ?? "";
    }

    /// <summary>Creates an exception.</summary>
    public TronzapInvalidResponseException()
    {
        ResponseBody = "";
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapInvalidResponseException(string message)
        : base(message)
    {
        ResponseBody = "";
    }

    /// <summary>Creates an exception with a message and a cause.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapInvalidResponseException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ResponseBody = "";
    }

    /// <summary>The HTTP status of the response.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The raw response body, empty when it was too large to keep.</summary>
    public string ResponseBody { get; }
}
