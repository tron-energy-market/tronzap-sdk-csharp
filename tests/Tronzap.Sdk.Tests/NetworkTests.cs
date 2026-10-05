using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Tronzap.Sdk.Exceptions;
using Xunit;
using static Tronzap.Sdk.Tests.TestSupport;

namespace Tronzap.Sdk.Tests;

public sealed class NetworkTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(300);

    [Fact]
    public async Task SlowResponseTimesOut()
    {
        await using var server = TestServer.Start(new Reply { Delay = TimeSpan.FromSeconds(5), Body = "{}"u8.ToArray() });
        TronzapClient client = Client(server, o => o.Timeout = ShortTimeout);

        var stopwatch = Stopwatch.StartNew();
        var e = await Assert.ThrowsAsync<TronzapTimeoutException>(() => client.GetBalanceAsync(Ct));

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(4), $"took {stopwatch.Elapsed}");
        Assert.IsAssignableFrom<TronzapNetworkException>(e);
        Assert.IsAssignableFrom<OperationCanceledException>(e.InnerException);
    }

    [Fact]
    public async Task TricklingBodyTimesOut()
    {
        await using var server = TestServer.Start(new Reply
        {
            Body = """{"code":0,"result":{"balance":1,"address":"T"}}"""u8.ToArray(),
            TrickleDelay = TimeSpan.FromMilliseconds(100),
        });
        TronzapClient client = Client(server, o => o.Timeout = ShortTimeout);

        await Assert.ThrowsAsync<TronzapTimeoutException>(() => client.GetBalanceAsync(Ct));
    }

    [Fact]
    public async Task CallerCancellationIsNotTimeout()
    {
        await using var server = TestServer.Start(new Reply { Delay = TimeSpan.FromSeconds(5), Body = "{}"u8.ToArray() });
        TronzapClient client = Client(server, o => o.Timeout = TimeSpan.FromSeconds(30));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cts.CancelAfter(ShortTimeout);

        var e = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetBalanceAsync(cts.Token));

        Assert.Equal(cts.Token, e.CancellationToken);
    }

    [Fact]
    public async Task AlreadyCanceledTokenSendsNothing()
    {
        await using var server = TestServer.Start(Reply.Ok("{}"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(server).GetBalanceAsync(cts.Token));

        await Task.Delay(100, Ct);
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task HttpClientTimeoutIsReportedAsTimeout()
    {
        await using var server = TestServer.Start(new Reply { Delay = TimeSpan.FromSeconds(5), Body = "{}"u8.ToArray() });
        using var httpClient = new HttpClient { Timeout = ShortTimeout };

        var e = await Assert.ThrowsAsync<TronzapTimeoutException>(() => Client(httpClient, server.BaseUrl).GetBalanceAsync(Ct));

        Assert.Contains("HttpClient timeout", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefusedConnectionIsConnectionError()
    {
        int port;
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        var options = new TronzapClientOptions { ApiToken = Token, ApiSecret = Secret, BaseUrl = $"http://127.0.0.1:{port}" };

        var e = await Assert.ThrowsAsync<TronzapConnectionException>(() => new TronzapClient(options).GetBalanceAsync(Ct));

        Assert.IsType<HttpRequestException>(e.InnerException);
    }

    [Fact]
    public async Task UntrustedCertificateIsSslError()
    {
        await using var server = TestServer.StartTls(Reply.Ok("""{"balance":1,"address":"T"}"""));

        var e = await Assert.ThrowsAsync<TronzapSslException>(() => Client(server).GetBalanceAsync(Ct));

        Assert.StartsWith("TLS error", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TlsWorksWithACustomHttpClient()
    {
        await using var server = TestServer.StartTls(Reply.Ok("""{"balance":1,"address":"T"}"""));
#pragma warning disable CA5359 // The test trusts its own throwaway certificate.
        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
#pragma warning restore CA5359
        using var httpClient = new HttpClient(handler);

        var balance = await Client(httpClient, server.BaseUrl).GetBalanceAsync(Ct);

        Assert.Equal(1m, balance.Balance);
        Assert.Equal(ExpectedSignature(server.SingleRequest.Body), server.SingleRequest.Header("X-Signature"));
    }

    [Fact]
    public async Task DroppedConnectionIsNetworkError()
    {
        await using var server = TestServer.Start(new Reply { Drop = true });

        var e = await Assert.ThrowsAnyAsync<TronzapNetworkException>(() => Client(server).GetBalanceAsync(Ct));

        Assert.IsNotType<TronzapTimeoutException>(e);
    }

    [Fact]
    public async Task TruncatedBodyIsNetworkError()
    {
        await using var server = TestServer.Start(new Reply
        {
            Body = """{"code":0,"result":{"balance":1,"address":"T"}}"""u8.ToArray(),
            Truncate = true,
        });

        var e = await Assert.ThrowsAnyAsync<TronzapNetworkException>(() => Client(server).GetBalanceAsync(Ct));

        Assert.IsNotType<TronzapTimeoutException>(e);
    }

    [Theory]
    [InlineData(HttpRequestError.NameResolutionError, typeof(TronzapConnectionException))]
    [InlineData(HttpRequestError.ConnectionError, typeof(TronzapConnectionException))]
    [InlineData(HttpRequestError.SecureConnectionError, typeof(TronzapSslException))]
    [InlineData(HttpRequestError.ResponseEnded, typeof(TronzapNetworkException))]
    [InlineData(HttpRequestError.ProxyTunnelError, typeof(TronzapNetworkException))]
    [InlineData(HttpRequestError.Unknown, typeof(TronzapNetworkException))]
    public async Task ClassifiesHttpRequestErrors(HttpRequestError error, Type expected)
    {
        using var httpClient = new HttpClient(new ThrowingHandler(new HttpRequestException(error, "boom")));

        var e = await Assert.ThrowsAnyAsync<TronzapNetworkException>(() => Client(httpClient).GetBalanceAsync(Ct));

        Assert.IsType(expected, e);
        Assert.Contains("boom", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClassifiesSocketErrorWithoutRequestErrorKind()
    {
        var failure = new HttpRequestException("failed", new SocketException((int)SocketError.HostNotFound));
        using var httpClient = new HttpClient(new ThrowingHandler(failure));

        await Assert.ThrowsAsync<TronzapConnectionException>(() => Client(httpClient).GetBalanceAsync(Ct));
    }

    [Fact]
    public async Task ClassifiesAuthenticationFailureAsSsl()
    {
        var failure = new HttpRequestException("failed", new System.Security.Authentication.AuthenticationException("bad cert"));
        using var httpClient = new HttpClient(new ThrowingHandler(failure));

        var e = await Assert.ThrowsAsync<TronzapSslException>(() => Client(httpClient).GetBalanceAsync(Ct));

        Assert.Contains("bad cert", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolvesARealUnknownHost()
    {
        var options = new TronzapClientOptions
        {
            ApiToken = Token,
            ApiSecret = Secret,
            BaseUrl = "https://tronzap-sdk-test.invalid",
            Timeout = TimeSpan.FromSeconds(20),
        };

        await Assert.ThrowsAsync<TronzapConnectionException>(() => new TronzapClient(options).GetBalanceAsync(Ct));
    }

    private sealed class ThrowingHandler(Exception failure) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(failure);
    }
}
