using System;

namespace Tronzap.Sdk.Exceptions;

/// <summary>No response arrived: the request failed below the HTTP level.</summary>
public class TronzapNetworkException : TronzapException
{
    /// <summary>Creates an exception.</summary>
    public TronzapNetworkException()
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapNetworkException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and the failure that caused it.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapNetworkException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
