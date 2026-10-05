using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using Tronzap.Sdk.Exceptions;

namespace Tronzap.Sdk.Internal;

internal static class NetworkErrors
{
    private const int MaxCauseDepth = 16;

    public static TronzapNetworkException Classify(Exception failure)
    {
        string detail = Describe(failure);
        HttpRequestError error = failure is HttpRequestException http ? http.HttpRequestError : HttpRequestError.Unknown;

        if (error == HttpRequestError.SecureConnectionError || HasCause<AuthenticationException>(failure))
        {
            return new TronzapSslException($"TLS error: {detail}", failure);
        }

        if (error is HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError
            || HasCause<SocketException>(failure, IsConnectFailure))
        {
            return new TronzapConnectionException($"Connection failed: {detail}", failure);
        }

        return new TronzapNetworkException($"Network error: {detail}", failure);
    }

    private static bool IsConnectFailure(SocketException e) => e.SocketErrorCode
        is SocketError.ConnectionRefused
        or SocketError.HostNotFound
        or SocketError.HostUnreachable
        or SocketError.NetworkUnreachable
        or SocketError.TryAgain
        or SocketError.NoData;

    private static bool HasCause<T>(Exception failure, Func<T, bool>? predicate = null)
        where T : Exception
    {
        Exception? current = failure;
        for (int depth = 0; current is not null && depth < MaxCauseDepth; depth++)
        {
            if (current is T match && (predicate is null || predicate(match)))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private static string Describe(Exception failure)
    {
        Exception? current = failure.InnerException;
        string message = failure.Message;
        for (int depth = 0; current is not null && depth < MaxCauseDepth; depth++)
        {
            if (!string.IsNullOrWhiteSpace(current.Message) && !message.Contains(current.Message, StringComparison.Ordinal))
            {
                message = $"{message} ({current.Message})";
            }

            current = current.InnerException;
        }

        return message;
    }
}
