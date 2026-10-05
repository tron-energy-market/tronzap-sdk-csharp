using System;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The TLS handshake failed, for example because the server certificate is not trusted.</summary>
public class TronzapSslException : TronzapNetworkException
{
    /// <summary>Creates an exception.</summary>
    public TronzapSslException()
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapSslException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and the failure that caused it.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapSslException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
