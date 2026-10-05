using System;

namespace Tronzap.Sdk.Exceptions;

/// <summary>The request did not complete within <see cref="TronzapClientOptions.Timeout"/> or the <see cref="System.Net.Http.HttpClient.Timeout"/> of the HTTP client. Cancellation through a <see cref="System.Threading.CancellationToken"/> is reported as <see cref="OperationCanceledException"/> instead.</summary>
public class TronzapTimeoutException : TronzapNetworkException
{
    /// <summary>Creates an exception.</summary>
    public TronzapTimeoutException()
    {
    }

    /// <summary>Creates an exception with a message.</summary>
    /// <param name="message">The error message.</param>
    public TronzapTimeoutException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and the failure that caused it.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The cause.</param>
    public TronzapTimeoutException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
