using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Tronzap.Sdk.Tests;

internal sealed record RecordedRequest(string Method, string Path, IReadOnlyDictionary<string, string> Headers, byte[] Body)
{
    public string BodyText => Encoding.UTF8.GetString(Body);

    public JsonNode? Json => JsonNode.Parse(Body);

    public string? Header(string name) => Headers.TryGetValue(name, out string? value) ? value : null;
}

internal sealed class Reply
{
    public int Status { get; init; } = 200;

    public byte[] Body { get; init; } = [];

    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public TimeSpan Delay { get; init; }

    public bool Drop { get; init; }

    public bool Truncate { get; init; }

    public bool OmitContentLength { get; init; }

    public TimeSpan TrickleDelay { get; init; }

    public static Reply Raw(int status, string body, string contentType = "application/json") => new()
    {
        Status = status,
        Body = Encoding.UTF8.GetBytes(body),
        Headers = new(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = contentType },
    };

    public static Reply Ok(string resultJson) => Raw(200, $$"""{"code":0,"result":{{resultJson}}}""");

    public static Reply Error(int status, int code, string error, string? key = null, string? requestId = null)
    {
        var body = new JsonObject { ["code"] = code, ["error"] = error };
        if (key is not null)
        {
            body["key"] = key;
        }

        if (requestId is not null)
        {
            body["request_id"] = requestId;
        }

        return Raw(status, body.ToJsonString());
    }
}

internal sealed class TestServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<RecordedRequest, Reply> _handler;
    private readonly X509Certificate2? _certificate;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _acceptLoop;

    private TestServer(Func<RecordedRequest, Reply> handler, X509Certificate2? certificate)
    {
        _handler = handler;
        _certificate = certificate;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public string BaseUrl => $"{(_certificate is null ? "http" : "https")}://127.0.0.1:{Port}";

    public RecordedRequest SingleRequest
    {
        get
        {
            Xunit.Assert.Single(Requests);
            Requests.TryPeek(out RecordedRequest? request);
            return request!;
        }
    }

    public static TestServer Start(Func<RecordedRequest, Reply> handler) => new(handler, null);

    public static TestServer Start(Reply reply) => new(_ => reply, null);

    public static TestServer StartTls(Reply reply) => new(_ => reply, CreateSelfSignedCertificate());

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        try
        {
            await _acceptLoop;
        }
        catch (Exception e) when (e is OperationCanceledException or SocketException or ObjectDisposedException)
        {
        }

        _stop.Dispose();
        _certificate?.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient connection;
            try
            {
                connection = await _listener.AcceptTcpClientAsync(_stop.Token);
            }
            catch (Exception e) when (e is OperationCanceledException or SocketException or ObjectDisposedException)
            {
                return;
            }

            _ = Task.Run(() => ServeAsync(connection));
        }
    }

    private async Task ServeAsync(TcpClient connection)
    {
        using (connection)
        {
            try
            {
                Stream stream = connection.GetStream();
                if (_certificate is not null)
                {
                    var tls = new SslStream(stream);
                    await tls.AuthenticateAsServerAsync(_certificate);
                    stream = tls;
                }

                await using (stream)
                {
                    RecordedRequest request = await ReadRequestAsync(stream);
                    Requests.Enqueue(request);
                    Reply reply = _handler(request);
                    if (reply.Delay > TimeSpan.Zero)
                    {
                        await Task.Delay(reply.Delay, _stop.Token);
                    }

                    if (reply.Drop)
                    {
                        connection.Client.LingerState = new LingerOption(true, 0);
                        return;
                    }

                    await WriteReplyAsync(stream, reply);
                }
            }
            catch (Exception e) when (e is IOException or SocketException or OperationCanceledException or ObjectDisposedException
                                          or System.Security.Authentication.AuthenticationException)
            {
            }
        }
    }

    private async Task WriteReplyAsync(Stream stream, Reply reply)
    {
        var head = new StringBuilder();
        head.Append("HTTP/1.1 ").Append(reply.Status).Append(' ').Append(Reason(reply.Status)).Append("\r\n");
        foreach ((string name, string value) in reply.Headers)
        {
            head.Append(name).Append(": ").Append(value).Append("\r\n");
        }

        if (!reply.OmitContentLength)
        {
            head.Append("Content-Length: ").Append(reply.Body.Length).Append("\r\n");
        }

        head.Append("Connection: close\r\n\r\n");
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head.ToString()), _stop.Token);

        if (reply.Truncate)
        {
            await stream.WriteAsync(reply.Body.AsMemory(0, reply.Body.Length / 2), _stop.Token);
            await stream.FlushAsync(_stop.Token);
            return;
        }

        if (reply.TrickleDelay > TimeSpan.Zero)
        {
            foreach (byte b in reply.Body)
            {
                await stream.WriteAsync(new[] { b }, _stop.Token);
                await stream.FlushAsync(_stop.Token);
                await Task.Delay(reply.TrickleDelay, _stop.Token);
            }

            return;
        }

        await stream.WriteAsync(reply.Body, _stop.Token);
        await stream.FlushAsync(_stop.Token);
    }

    private async Task<RecordedRequest> ReadRequestAsync(Stream stream)
    {
        var head = new MemoryStream();
        var one = new byte[1];
        while (!EndsWithBlankLine(head))
        {
            if (await stream.ReadAsync(one, _stop.Token) == 0)
            {
                throw new IOException("connection closed before the request head ended");
            }

            head.WriteByte(one[0]);
        }

        string[] lines = Encoding.ASCII.GetString(head.ToArray()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        string[] requestLine = lines[0].Split(' ');
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in lines[1..])
        {
            int colon = line.IndexOf(':', StringComparison.Ordinal);
            headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
        }

        int length = headers.TryGetValue("Content-Length", out string? value) ? int.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        var body = new byte[length];
        await stream.ReadExactlyAsync(body, _stop.Token);
        return new RecordedRequest(requestLine[0], requestLine[1], headers, body);
    }

    private static bool EndsWithBlankLine(MemoryStream head)
    {
        if (head.Length < 4)
        {
            return false;
        }

        byte[] buffer = head.GetBuffer();
        long n = head.Length;
        return buffer[n - 4] == '\r' && buffer[n - 3] == '\n' && buffer[n - 2] == '\r' && buffer[n - 1] == '\n';
    }

    private static string Reason(int status) => status switch
    {
        200 => "OK",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        _ => "Status",
    };

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=127.0.0.1", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        using X509Certificate2 ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        // SslStream on macOS and Windows needs a certificate whose key is backed by a PKCS#12 import.
        byte[] pfx = ephemeral.Export(X509ContentType.Pfx);
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pfx, null);
#else
        return new X509Certificate2(pfx);
#endif
    }
}
