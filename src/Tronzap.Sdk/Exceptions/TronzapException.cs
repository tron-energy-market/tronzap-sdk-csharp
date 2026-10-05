using System;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The base class of every failure of a TronZap API call.</summary>
public class TronzapException : Exception
{
    /// <summary>Creates an exception.</summary>
    public TronzapException()
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and the failure that caused it.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
