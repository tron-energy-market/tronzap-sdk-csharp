using System;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The connection could not be established: the host name did not resolve or the connection was refused.</summary>
public class TronzapConnectionException : TronzapNetworkException
{
    /// <summary>Creates an exception.</summary>
    public TronzapConnectionException()
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and the failure that caused it.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapConnectionException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
