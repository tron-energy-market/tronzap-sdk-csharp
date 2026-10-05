using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using Xunit;

namespace Tronzap.Sdk.Tests;

internal static class TestSupport
{
    public const string Token = "test-token";
    public const string Secret = "test-secret";
    public const string Address = "TRecipientAddressXXXXXXXXXXXXXXXXX";

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static TronzapClient Client(TestServer server, Action<TronzapClientOptions>? configure = null)
    {
        var options = new TronzapClientOptions { ApiToken = Token, ApiSecret = Secret, BaseUrl = server.BaseUrl };
        configure?.Invoke(options);
        return new TronzapClient(options);
    }

    public static TronzapClient Client(HttpClient httpClient, string baseUrl = "http://127.0.0.1:1", Action<TronzapClientOptions>? configure = null)
    {
        var options = new TronzapClientOptions { ApiToken = Token, ApiSecret = Secret, BaseUrl = baseUrl };
        configure?.Invoke(options);
        return new TronzapClient(httpClient, options);
    }

    public static string ExpectedSignature(byte[] body)
    {
        byte[] secret = Encoding.UTF8.GetBytes(Secret);
        byte[] data = new byte[body.Length + secret.Length];
        body.CopyTo(data, 0);
        secret.CopyTo(data, body.Length);
        return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    }

    public static void AssertJsonEqual(string expected, RecordedRequest request)
    {
        JsonNode? want = JsonNode.Parse(expected);
        JsonNode? got = JsonNode.Parse(request.Body);
        Assert.True(JsonNode.DeepEquals(want, got), $"expected {want?.ToJsonString()}, got {request.BodyText}");
    }
}
